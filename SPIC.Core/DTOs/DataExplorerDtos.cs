using System.Collections.Generic;

namespace SPIC.Core.DTOs
{
	/// <summary>
	/// Contracts of the admin Data Explorer: table browsing, ad-hoc SQL, and
	/// row-level create/update/delete, all behind the page password.
	/// </summary>
	public sealed class DataExplorerColumnInfo
	{
		public string Name { get; set; } = string.Empty;
		public string DataType { get; set; } = string.Empty;   // PostgreSQL udt name, e.g. int4, text, timestamp
		public bool Nullable { get; set; }
		public bool HasDefault { get; set; }
		public bool IsPrimaryKey { get; set; }
	}

	public sealed class DataExplorerTableInfo
	{
		public string Schema { get; set; } = "public";
		public string Name { get; set; } = string.Empty;
		public long EstimatedRows { get; set; }
		public List<DataExplorerColumnInfo> Columns { get; set; } = new();
	}

	public sealed class DataExplorerQueryRequest
	{
		public string Sql { get; set; } = string.Empty;
		public int Page { get; set; } = 1;
		public int PageSize { get; set; } = 100;
		/// <summary>For statements that change rows: false = dry run inside a rolled-back transaction, true = commit.</summary>
		public bool Confirm { get; set; }
	}

	public sealed class DataExplorerBrowseRequest
	{
		public string Schema { get; set; } = "public";
		public string Table { get; set; } = string.Empty;
		public int Page { get; set; } = 1;
		public int PageSize { get; set; } = 100;
		/// <summary>Free text matched case-insensitively against every column of the row.</summary>
		public string? Filter { get; set; }
		public string? OrderBy { get; set; }
		public bool Descending { get; set; }
	}

	public sealed class DataExplorerQueryResult
	{
		/// <summary>rows (a result set), affected (a change: see AffectedRows/Committed), or message.</summary>
		public string Kind { get; set; } = "rows";
		public List<string> Columns { get; set; } = new();
		public List<List<string?>> Rows { get; set; } = new();
		public bool HasMore { get; set; }
		public int Page { get; set; }
		public int PageSize { get; set; }
		public long ElapsedMs { get; set; }
		public int AffectedRows { get; set; }
		public bool Committed { get; set; }
		public string? Message { get; set; }
	}

	/// <summary>
	/// An INSERT/UPDATE/DELETE statement. Confirm=false runs it inside a transaction that is
	/// rolled back and only reports how many rows it would touch; Confirm=true commits.
	/// </summary>
	public sealed class DataExplorerExecuteRequest
	{
		public string Sql { get; set; } = string.Empty;
		public bool Confirm { get; set; }
	}

	public sealed class DataExplorerExecuteResult
	{
		public int AffectedRows { get; set; }
		public bool Committed { get; set; }
		public long ElapsedMs { get; set; }
		public string Message { get; set; } = string.Empty;
	}

	/// <summary>
	/// Row-level change by primary key. Values are strings; the API casts them to the column
	/// type. The literal text NULL (upper case) means SQL NULL.
	/// </summary>
	public sealed class DataExplorerRowRequest
	{
		public string Schema { get; set; } = "public";
		public string Table { get; set; } = string.Empty;
		public Dictionary<string, string?> Key { get; set; } = new();
		public Dictionary<string, string?> Values { get; set; } = new();
	}

	public sealed class DataExplorerAuditEntry
	{
		public long Id { get; set; }
		public string At { get; set; } = string.Empty;
		public string UserName { get; set; } = string.Empty;
		public string Kind { get; set; } = string.Empty;
		public string Sql { get; set; } = string.Empty;
		public int AffectedRows { get; set; }
	}
}
