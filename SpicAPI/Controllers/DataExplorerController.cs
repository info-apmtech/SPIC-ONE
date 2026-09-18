using System.Data;
using System.Diagnostics;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;

namespace SpicAPI.Controllers
{
	/// <summary>
	/// Admin Data Explorer: browse, filter, export and correct rows of any table, and run
	/// ad-hoc SQL, from the web portal instead of handing out database passwords.
	///
	/// Protection, in layers:
	///   1. JWT login with the Admin or SpecialAdmin role.
	///   2. The page password (DataExplorer:Password) sent as X-Explorer-Password on every call.
	///   3. Query box: one statement only; DDL and bulk-destructive keywords are refused;
	///      UPDATE and DELETE must carry a WHERE clause; a change is first dry-run in a
	///      rolled-back transaction so the caller sees the affected row count before confirming.
	///   4. Row editor: table and column names must exist in the catalogue, values are bound
	///      parameters cast to the column type, and updates/deletes must match exactly one row.
	///   5. Every committed change and every export is recorded in "DataExplorerAuditLogs".
	///   6. 30-second statement timeout; reads run in a READ ONLY transaction.
	/// </summary>
	[ApiController]
	[Route("api/[controller]")]
	[Authorize]
	public sealed class DataExplorerController : ControllerBase
	{
		private const string PasswordHeader = "X-Explorer-Password";
		private const string AuditTable = "DataExplorerAuditLogs";
		private const int StatementTimeoutMs = 30_000;
		private const int MaxPageSize = 500;
		private const int ExportRowCap = 50_000;

		private static readonly HashSet<string> ProtectedTables = new(StringComparer.OrdinalIgnoreCase)
		{
			AuditTable, "__EFMigrationsHistory", "DataProtectionKeys"
		};
		private static readonly Regex ForbiddenKeywords = new(
			@"\b(drop|truncate|alter|create|grant|revoke|copy|vacuum|reindex|cluster|comment|security|reassign|do|call|execute|prepare|deallocate|listen|notify|set|reset|show|explain)\b",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static readonly Regex LeadingKeyword = new(@"^\s*(select|with|insert|update|delete)\b",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);

		private readonly IConfiguration _config;
		private readonly ILogger<DataExplorerController> _log;

		public DataExplorerController(IConfiguration config, ILogger<DataExplorerController> log)
		{
			_config = config;
			_log = log;
		}

		// ------------------------------------------------------------------ guards

		private IActionResult? Guard()
		{
			var role = User.FindFirstValue(ClaimTypes.Role);
			if (role != nameof(AppRole.Admin) && role != nameof(AppRole.SpecialAdmin))
				return StatusCode(403, new { message = "Data Explorer is available to Admin and SpecialAdmin only." });

			var expected = _config["DataExplorer:Password"];
			if (string.IsNullOrEmpty(expected))
				return StatusCode(503, new { message = "Data Explorer is not configured on this server (DataExplorer:Password missing)." });

			var given = Request.Headers[PasswordHeader].ToString();
			var a = Encoding.UTF8.GetBytes(expected);
			var b = Encoding.UTF8.GetBytes(given);
			if (a.Length != b.Length || !CryptographicOperations.FixedTimeEquals(a, b))
				return StatusCode(401, new { message = "Wrong Data Explorer password." });
			return null;
		}

		private string UserName =>
			User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";

		private NpgsqlConnection OpenConnection()
		{
			var cs = _config.GetConnectionString("DefaultConnection")
				?? throw new InvalidOperationException("DefaultConnection is not configured.");
			var conn = new NpgsqlConnection(cs);
			conn.Open();
			return conn;
		}

		private static string Q(string identifier) => "\"" + identifier.Replace("\"", "\"\"") + "\"";

		/// <summary>Strips comments, refuses more than one statement and forbidden keywords, returns the leading keyword.</summary>
		private static (string sql, string keyword) Sanitize(string raw)
		{
			if (string.IsNullOrWhiteSpace(raw)) throw new ArgumentException("SQL is empty.");
			var sql = Regex.Replace(raw, @"--[^\n]*", string.Empty);
			sql = Regex.Replace(sql, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline).Trim();
			if (sql.EndsWith(';')) sql = sql[..^1].TrimEnd();
			if (sql.Contains(';')) throw new ArgumentException("One statement at a time; remove the extra ';'.");
			if (ForbiddenKeywords.IsMatch(sql)) throw new ArgumentException("Only data statements are allowed here: no DROP, TRUNCATE, ALTER, CREATE, GRANT, COPY, SET or similar.");
			foreach (var t in ProtectedTables)
				if (sql.Contains(t, StringComparison.OrdinalIgnoreCase) && !Regex.IsMatch(sql, @"^\s*(select|with)\b", RegexOptions.IgnoreCase))
					throw new ArgumentException($"Table {t} is protected.");
			var m = LeadingKeyword.Match(sql);
			if (!m.Success) throw new ArgumentException("Only SELECT, WITH, INSERT, UPDATE and DELETE statements are accepted.");
			var keyword = m.Groups[1].Value.ToLowerInvariant();
			if ((keyword == "update" || keyword == "delete") && !Regex.IsMatch(sql, @"\bwhere\b", RegexOptions.IgnoreCase))
				throw new ArgumentException($"{keyword.ToUpperInvariant()} without WHERE is refused. Add WHERE, even WHERE true if you really mean every row.");
			return (sql, keyword);
		}

		private static bool IsRead(string keyword) => keyword is "select" or "with";

		// ------------------------------------------------------------------ catalogue

		private static List<DataExplorerColumnInfo> LoadColumns(NpgsqlConnection conn, string schema, string table)
		{
			const string sql = @"
				select c.column_name, c.udt_name, c.is_nullable = 'YES', c.column_default is not null,
				       exists (select 1 from information_schema.table_constraints tc
				               join information_schema.key_column_usage k on k.constraint_name = tc.constraint_name and k.table_schema = tc.table_schema
				               where tc.constraint_type = 'PRIMARY KEY' and tc.table_schema = c.table_schema and tc.table_name = c.table_name and k.column_name = c.column_name)
				from information_schema.columns c
				join information_schema.tables t on t.table_schema = c.table_schema and t.table_name = c.table_name and t.table_type = 'BASE TABLE'
				where c.table_schema = @s and c.table_name = @t
				order by c.ordinal_position";
			using var cmd = new NpgsqlCommand(sql, conn);
			cmd.Parameters.AddWithValue("s", schema);
			cmd.Parameters.AddWithValue("t", table);
			using var r = cmd.ExecuteReader();
			var list = new List<DataExplorerColumnInfo>();
			while (r.Read())
				list.Add(new DataExplorerColumnInfo { Name = r.GetString(0), DataType = r.GetString(1), Nullable = r.GetBoolean(2), HasDefault = r.GetBoolean(3), IsPrimaryKey = r.GetBoolean(4) });
			if (list.Count == 0) throw new ArgumentException($"Table {schema}.{table} does not exist.");
			if (ProtectedTables.Contains(table)) throw new ArgumentException($"Table {table} is protected.");
			return list;
		}

		[HttpGet("tables")]
		public IActionResult Tables()
		{
			if (Guard() is { } denied) return denied;
			using var conn = OpenConnection();
			const string sql = @"
				select t.table_schema, t.table_name, coalesce(s.n_live_tup, 0)
				from information_schema.tables t
				left join pg_stat_user_tables s on s.schemaname = t.table_schema and s.relname = t.table_name
				where t.table_schema = 'public' and t.table_type = 'BASE TABLE'
				order by t.table_name";
			var tables = new List<DataExplorerTableInfo>();
			using (var cmd = new NpgsqlCommand(sql, conn))
			using (var r = cmd.ExecuteReader())
				while (r.Read())
					if (!ProtectedTables.Contains(r.GetString(1)))
						tables.Add(new DataExplorerTableInfo { Schema = r.GetString(0), Name = r.GetString(1), EstimatedRows = r.GetInt64(2) });
			foreach (var t in tables) t.Columns = LoadColumns(conn, t.Schema, t.Name);
			return Ok(tables);
		}

		// ------------------------------------------------------------------ query box

		[HttpPost("query")]
		public IActionResult Query([FromBody] DataExplorerQueryRequest req)
		{
			if (Guard() is { } denied) return denied;
			try
			{
				var (sql, keyword) = Sanitize(req.Sql);
				using var conn = OpenConnection();
				if (IsRead(keyword))
				{
					var pageSize = Math.Clamp(req.PageSize, 1, MaxPageSize);
					var page = Math.Max(1, req.Page);
					var paged = $"select * from ({sql}) q limit {pageSize + 1} offset {(page - 1) * pageSize}";
					return Ok(RunRead(conn, paged, new List<NpgsqlParameter>(), page, pageSize));
				}
				var write = RunWrite(conn, sql, new List<NpgsqlParameter>(), req.Confirm, keyword, sql);
				return Ok(new DataExplorerQueryResult
				{
					Kind = "affected", AffectedRows = write.AffectedRows, Committed = write.Committed, ElapsedMs = write.ElapsedMs, Message = write.Message
				});
			}
			catch (Exception ex) when (ex is ArgumentException or PostgresException or NpgsqlException)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

		[HttpPost("query/export")]
		public IActionResult QueryExport([FromBody] DataExplorerQueryRequest req)
		{
			if (Guard() is { } denied) return denied;
			try
			{
				var (sql, keyword) = Sanitize(req.Sql);
				if (!IsRead(keyword)) return BadRequest(new { message = "Only SELECT can be exported." });
				using var conn = OpenConnection();
				var result = RunRead(conn, $"select * from ({sql}) q limit {ExportRowCap}", new List<NpgsqlParameter>(), 1, ExportRowCap);
				WriteAudit(conn, null, "export", sql, result.Rows.Count);
				return CsvFile(result, $"query-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
			}
			catch (Exception ex) when (ex is ArgumentException or PostgresException or NpgsqlException)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

		// ------------------------------------------------------------------ table browsing

		[HttpPost("browse")]
		public IActionResult Browse([FromBody] DataExplorerBrowseRequest req)
		{
			if (Guard() is { } denied) return denied;
			try
			{
				using var conn = OpenConnection();
				var columns = LoadColumns(conn, req.Schema, req.Table);
				var pageSize = Math.Clamp(req.PageSize, 1, MaxPageSize);
				var page = Math.Max(1, req.Page);
				var (sql, parameters) = BuildSelect(req, columns, pageSize + 1, (page - 1) * pageSize);
				return Ok(RunRead(conn, sql, parameters, page, pageSize));
			}
			catch (Exception ex) when (ex is ArgumentException or PostgresException or NpgsqlException)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

		[HttpPost("export")]
		public IActionResult Export([FromBody] DataExplorerBrowseRequest req)
		{
			if (Guard() is { } denied) return denied;
			try
			{
				using var conn = OpenConnection();
				var columns = LoadColumns(conn, req.Schema, req.Table);
				var (sql, parameters) = BuildSelect(req, columns, ExportRowCap, 0);
				var result = RunRead(conn, sql, parameters, 1, ExportRowCap);
				WriteAudit(conn, null, "export", $"{req.Table} filter='{req.Filter}'", result.Rows.Count);
				return CsvFile(result, $"{req.Table}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
			}
			catch (Exception ex) when (ex is ArgumentException or PostgresException or NpgsqlException)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

		private static (string sql, List<NpgsqlParameter> parameters) BuildSelect(DataExplorerBrowseRequest req, List<DataExplorerColumnInfo> columns, int limit, int offset)
		{
			var parameters = new List<NpgsqlParameter>();
			var sb = new StringBuilder();
			sb.Append("select ").Append(string.Join(", ", columns.Select(c => Q(c.Name))));
			sb.Append(" from ").Append(Q(req.Schema)).Append('.').Append(Q(req.Table)).Append(" t");
			if (!string.IsNullOrWhiteSpace(req.Filter))
			{
				sb.Append(" where cast(row_to_json(t) as text) ilike @f");
				parameters.Add(new NpgsqlParameter("f", "%" + req.Filter.Trim() + "%"));
			}
			var orderBy = columns.FirstOrDefault(c => c.Name == req.OrderBy) ?? columns.FirstOrDefault(c => c.IsPrimaryKey) ?? columns[0];
			sb.Append(" order by ").Append(Q(orderBy.Name)).Append(req.Descending ? " desc" : " asc");
			sb.Append(" limit ").Append(limit).Append(" offset ").Append(offset);
			return (sb.ToString(), parameters);
		}

		private static DataExplorerQueryResult RunRead(NpgsqlConnection conn, string sql, List<NpgsqlParameter> parameters, int page, int pageSize)
		{
			var sw = Stopwatch.StartNew();
			using var tx = conn.BeginTransaction(IsolationLevel.ReadCommitted);
			using (var set = new NpgsqlCommand($"set local statement_timeout = {StatementTimeoutMs}; set transaction read only", conn, tx)) set.ExecuteNonQuery();
			using var cmd = new NpgsqlCommand(sql, conn, tx);
			foreach (var p in parameters) cmd.Parameters.Add(p);
			var result = new DataExplorerQueryResult { Kind = "rows", Page = page, PageSize = pageSize };
			using (var r = cmd.ExecuteReader())
			{
				for (var i = 0; i < r.FieldCount; i++) result.Columns.Add(r.GetName(i));
				while (r.Read())
				{
					if (result.Rows.Count == pageSize) { result.HasMore = true; break; }
					var row = new List<string?>(r.FieldCount);
					for (var i = 0; i < r.FieldCount; i++) row.Add(r.IsDBNull(i) ? null : FormatValue(r.GetValue(i)));
					result.Rows.Add(row);
				}
			}
			tx.Rollback();
			result.ElapsedMs = sw.ElapsedMilliseconds;
			return result;
		}

		private static string FormatValue(object v) => v switch
		{
			DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss"),
			DateTimeOffset dto => dto.ToString("yyyy-MM-dd HH:mm:ss zzz"),
			byte[] bytes => $"<{bytes.Length} bytes>",
			Array arr => "{" + string.Join(",", arr.Cast<object?>().Select(x => x?.ToString())) + "}",
			_ => Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty
		};

		private FileContentResult CsvFile(DataExplorerQueryResult result, string fileName)
		{
			var sb = new StringBuilder();
			sb.AppendLine(string.Join(",", result.Columns.Select(Csv)));
			foreach (var row in result.Rows) sb.AppendLine(string.Join(",", row.Select(c => Csv(c ?? string.Empty))));
			var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
			return File(bytes, "text/csv", fileName);
		}

		private static string Csv(string s) => s.Contains(',') || s.Contains('"') || s.Contains('\n') ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;

		// ------------------------------------------------------------------ row editor

		[HttpPost("row/insert")]
		public IActionResult RowInsert([FromBody] DataExplorerRowRequest req)
		{
			if (Guard() is { } denied) return denied;
			try
			{
				using var conn = OpenConnection();
				var columns = LoadColumns(conn, req.Schema, req.Table);
				var values = req.Values.Where(kv => !string.IsNullOrEmpty(kv.Value)).ToList();
				if (values.Count == 0) return BadRequest(new { message = "Enter at least one value." });
				var parameters = new List<NpgsqlParameter>();
				var cols = new List<string>();
				var vals = new List<string>();
				var i = 0;
				foreach (var kv in values)
				{
					var col = Column(columns, kv.Key);
					cols.Add(Q(col.Name));
					vals.Add(Typed($"v{i++}", kv.Value, col, parameters));
				}
				var sql = $"insert into {Q(req.Schema)}.{Q(req.Table)} ({string.Join(", ", cols)}) values ({string.Join(", ", vals)})";
				return Ok(RunWrite(conn, sql, parameters, true, "insert", $"{req.Table} [new]"));
			}
			catch (Exception ex) when (ex is ArgumentException or PostgresException or NpgsqlException or FormatException)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

		[HttpPost("row/update")]
		public IActionResult RowUpdate([FromBody] DataExplorerRowRequest req)
		{
			if (Guard() is { } denied) return denied;
			try
			{
				using var conn = OpenConnection();
				var columns = LoadColumns(conn, req.Schema, req.Table);
				if (req.Values.Count == 0) return BadRequest(new { message = "Nothing to update." });
				string Build(List<NpgsqlParameter> ps)
				{
					var sets = new List<string>();
					var i = 0;
					foreach (var kv in req.Values)
					{
						var col = Column(columns, kv.Key);
						sets.Add($"{Q(col.Name)} = {Typed($"v{i++}", kv.Value, col, ps)}");
					}
					return $"update {Q(req.Schema)}.{Q(req.Table)} set {string.Join(", ", sets)} where {KeyWhere(columns, req.Key, ps)}";
				}
				var dryParams = new List<NpgsqlParameter>();
				var dry = RunWrite(conn, Build(dryParams), dryParams, false, "update", $"{req.Table} [{KeyText(req.Key)}]");
				if (dry.AffectedRows != 1) return BadRequest(new { message = $"Refused: the key matches {dry.AffectedRows} rows, not exactly one." });
				var ps2 = new List<NpgsqlParameter>();
				return Ok(RunWrite(conn, Build(ps2), ps2, true, "update", $"{req.Table} [{KeyText(req.Key)}]"));
			}
			catch (Exception ex) when (ex is ArgumentException or PostgresException or NpgsqlException or FormatException)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

		[HttpPost("row/delete")]
		public IActionResult RowDelete([FromBody] DataExplorerRowRequest req)
		{
			if (Guard() is { } denied) return denied;
			try
			{
				using var conn = OpenConnection();
				var columns = LoadColumns(conn, req.Schema, req.Table);
				string Build(List<NpgsqlParameter> ps) => $"delete from {Q(req.Schema)}.{Q(req.Table)} where {KeyWhere(columns, req.Key, ps)}";
				var dryParams = new List<NpgsqlParameter>();
				var dry = RunWrite(conn, Build(dryParams), dryParams, false, "delete", $"{req.Table} [{KeyText(req.Key)}]");
				if (dry.AffectedRows != 1) return BadRequest(new { message = $"Refused: the key matches {dry.AffectedRows} rows, not exactly one." });
				var ps2 = new List<NpgsqlParameter>();
				return Ok(RunWrite(conn, Build(ps2), ps2, true, "delete", $"{req.Table} [{KeyText(req.Key)}]"));
			}
			catch (Exception ex) when (ex is ArgumentException or PostgresException or NpgsqlException or FormatException)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

		private static DataExplorerColumnInfo Column(List<DataExplorerColumnInfo> columns, string name) =>
			columns.FirstOrDefault(c => c.Name == name) ?? throw new ArgumentException($"Unknown column '{name}'.");

		/// <summary>WHERE over the primary key columns only; a table without a primary key cannot be edited row by row.</summary>
		private static string KeyWhere(List<DataExplorerColumnInfo> columns, Dictionary<string, string?> key, List<NpgsqlParameter> parameters)
		{
			var pk = columns.Where(c => c.IsPrimaryKey).ToList();
			if (pk.Count == 0) throw new ArgumentException("This table has no primary key, so rows cannot be edited here; use the query box.");
			var parts = new List<string>();
			var i = 0;
			foreach (var col in pk)
			{
				if (!key.TryGetValue(col.Name, out var v) || v == null) throw new ArgumentException($"Key column '{col.Name}' is missing.");
				parts.Add($"{Q(col.Name)} = {Typed($"k{i++}", v, col, parameters)}");
			}
			return string.Join(" and ", parts);
		}

		private static string KeyText(Dictionary<string, string?> key) => string.Join(", ", key.Select(kv => $"{kv.Key}={kv.Value}"));

		/// <summary>Binds a text parameter and casts it to the column's type. The literal NULL means SQL NULL.</summary>
		private static string Typed(string name, string? value, DataExplorerColumnInfo col, List<NpgsqlParameter> parameters)
		{
			var isArray = col.DataType.StartsWith('_');
			var type = Q(col.DataType.TrimStart('_')) + (isArray ? "[]" : string.Empty);
			if (value == null || value == "NULL")
			{
				if (!col.Nullable) throw new ArgumentException($"Column '{col.Name}' cannot be NULL.");
				parameters.Add(new NpgsqlParameter(name, DBNull.Value) { NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Text });
			}
			else
			{
				parameters.Add(new NpgsqlParameter(name, value) { NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Text });
			}
			return $"cast(@{name} as {type})";
		}

		private DataExplorerExecuteResult RunWrite(NpgsqlConnection conn, string sql, List<NpgsqlParameter> parameters, bool commit, string kind, string auditDetail)
		{
			var sw = Stopwatch.StartNew();
			using var tx = conn.BeginTransaction(IsolationLevel.ReadCommitted);
			using (var set = new NpgsqlCommand($"set local statement_timeout = {StatementTimeoutMs}", conn, tx)) set.ExecuteNonQuery();
			int affected;
			using (var cmd = new NpgsqlCommand(sql, conn, tx))
			{
				foreach (var p in parameters) cmd.Parameters.Add(p);
				affected = cmd.ExecuteNonQuery();
			}
			if (commit)
			{
				WriteAudit(conn, tx, kind, auditDetail, affected);
				tx.Commit();
			}
			else
			{
				tx.Rollback();
			}
			return new DataExplorerExecuteResult
			{
				AffectedRows = affected,
				Committed = commit,
				ElapsedMs = sw.ElapsedMilliseconds,
				Message = commit ? $"{affected} row(s) changed and committed." : $"Dry run: this would change {affected} row(s). Nothing was written yet."
			};
		}

		// ------------------------------------------------------------------ audit

		[HttpGet("audit")]
		public IActionResult AuditLog([FromQuery] int take = 100)
		{
			if (Guard() is { } denied) return denied;
			using var conn = OpenConnection();
			EnsureAuditTable(conn, null);
			using var cmd = new NpgsqlCommand($"select \"Id\", \"At\", \"UserName\", \"Kind\", \"Sql\", \"AffectedRows\" from {Q(AuditTable)} order by \"Id\" desc limit {Math.Clamp(take, 1, 1000)}", conn);
			using var r = cmd.ExecuteReader();
			var list = new List<DataExplorerAuditEntry>();
			while (r.Read())
				list.Add(new DataExplorerAuditEntry
				{
					Id = r.GetInt64(0), At = r.GetDateTime(1).ToString("yyyy-MM-dd HH:mm:ss"), UserName = r.GetString(2),
					Kind = r.GetString(3), Sql = r.GetString(4), AffectedRows = r.GetInt32(5)
				});
			return Ok(list);
		}

		private void WriteAudit(NpgsqlConnection conn, NpgsqlTransaction? tx, string kind, string detail, int affected)
		{
			EnsureAuditTable(conn, tx);
			using var cmd = new NpgsqlCommand($"insert into {Q(AuditTable)} (\"At\", \"UserId\", \"UserName\", \"Kind\", \"Sql\", \"AffectedRows\") values (now(), @uid, @un, @k, @s, @n)", conn, tx);
			cmd.Parameters.AddWithValue("uid", User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty);
			cmd.Parameters.AddWithValue("un", UserName);
			cmd.Parameters.AddWithValue("k", kind);
			cmd.Parameters.AddWithValue("s", detail.Length > 20000 ? detail[..20000] : detail);
			cmd.Parameters.AddWithValue("n", affected);
			cmd.ExecuteNonQuery();
			_log.LogInformation("DataExplorer {Kind} by {User}: {Rows} row(s)", kind, UserName, affected);
		}

		private static void EnsureAuditTable(NpgsqlConnection conn, NpgsqlTransaction? tx)
		{
			using var cmd = new NpgsqlCommand($@"create table if not exists {Q(AuditTable)} (
				""Id"" bigserial primary key, ""At"" timestamptz not null, ""UserId"" text not null, ""UserName"" text not null,
				""Kind"" text not null, ""Sql"" text not null, ""AffectedRows"" int not null)", conn, tx);
			cmd.ExecuteNonQuery();
		}
	}
}
