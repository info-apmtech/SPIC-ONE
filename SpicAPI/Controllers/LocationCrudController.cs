using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;
using System.Data;         // ADD THIS LINE
using System.Data.Common;
using System.Linq.Expressions;

namespace SpicAPI.Controllers
{
    public abstract class LocationCrudController<T> : GenericCrudController<T> where T : class
    {
        protected readonly AppDbContext _db;
        protected readonly string[] _fkColumnNames;

        protected LocationCrudController(
            IGenericRepository<T> repo,
            AppDbContext db,
            params string[] fkColumnNames) : base(repo)
        {
            _db = db;
            _fkColumnNames = fkColumnNames;
        }

        // The response remains List<string> so LocationMaster.razor and existing callers
        // do not need a DTO or endpoint change.
        [HttpGet("{id}/dependencies")]
        public async Task<IActionResult> GetDependencies(int id)
        {
            var dependencies = await CheckDependenciesAsync(id);
            return Ok(dependencies);
        }

        public override async Task<IActionResult> Delete(int id)
        {
            // Always recheck on the server even when the UI showed no dependencies.
            // A failed dependency scan must fail closed, never proceed to deletion.
            var dependencies = await CheckDependenciesAsync(id);
            if (dependencies.Count > 0)
            {
                return BadRequest(
                    "This record cannot be deleted because it is being used in other records. " +
                    "Please remove or reassign the dependent records first.");
            }

            try
            {
                return await base.Delete(id);
            }
            catch (DbUpdateException)
            {
                return BadRequest(
                    "This record cannot be deleted because it is being used in other records. " +
                    "Please remove or reassign the dependent records first.");
            }
        }

        private async Task<List<string>> CheckDependenciesAsync(int id)
        {
            // The application's PostgreSQL provider permits detection of tables that
            // are NOT present in AppDbContext, including legacy integer ID columns.
            // Other database providers keep the original EF-metadata-based behavior.
            if (_db.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
            {
                return await CheckPostgreSqlDependenciesAsync(id);
            }

            return await CheckEfMappedDependenciesAsync(id);
        }

        private async Task<List<string>> CheckPostgreSqlDependenciesAsync(int id)
        {
            var masterEntity = _db.Model.FindEntityType(typeof(T))
                ?? throw new InvalidOperationException(
                    $"The location master {typeof(T).Name} is not registered in AppDbContext.");

            var masterTable = masterEntity.GetTableName()
                ?? throw new InvalidOperationException(
                    $"The location master {typeof(T).Name} is not mapped to a database table.");

            var masterSchema = masterEntity.GetSchema() ?? "public";
            var qualifiedMasterTable =
                $"{QuoteIdentifier(masterSchema)}.{QuoteIdentifier(masterTable)}";

            var names = _fkColumnNames
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            // These parameter names are generated internally, not from any user input.
            var matchingColumnNames = names.Length == 0
                ? "FALSE"
                : string.Join(" OR ", names.Select((_, index) =>
                    $"lower(col.attname) = lower(@column{index})"));

            // Inspect real PostgreSQL tables, rather than only EF entities. Includes:
            //  (1) integer columns matching the configured master ID field names;
            //  (2) actual foreign keys to the master, even with different field names.
            // String/name-only mappings are intentionally not guessed: a location name
            // can be duplicated, and comparing it to an ID could misidentify records.
            var candidateSql = $@"
SELECT DISTINCT
       ns.nspname AS schema_name,
       tbl.relname AS table_name,
       col.attname AS column_name
FROM pg_catalog.pg_class AS tbl
JOIN pg_catalog.pg_namespace AS ns ON ns.oid = tbl.relnamespace
JOIN pg_catalog.pg_attribute AS col ON col.attrelid = tbl.oid
WHERE tbl.relkind IN ('r', 'p')
  AND ns.nspname <> 'information_schema'
  AND ns.nspname !~ '^pg_'
  AND col.attnum > 0
  AND NOT col.attisdropped
  AND (
      (
          col.atttypid IN ('int2'::regtype, 'int4'::regtype, 'int8'::regtype)
          AND ({matchingColumnNames})
      )
      OR EXISTS (
          SELECT 1
          FROM pg_catalog.pg_constraint AS fk
          WHERE fk.contype = 'f'
            AND fk.conrelid = tbl.oid
            AND fk.confrelid = to_regclass(@masterTable)
            AND col.attnum = ANY(fk.conkey)
      )
  )
ORDER BY schema_name, table_name, column_name;";

            var dependencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var references = new List<(string Schema, string Table, string Column)>();
            var connection = _db.Database.GetDbConnection();
            var openedHere = connection.State != ConnectionState.Open;
            var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;

            try
            {
                if (openedHere)
                {
                    await _db.Database.OpenConnectionAsync(cancellationToken);
                }

                await using (var discover = connection.CreateCommand())
                {
                    discover.CommandText = candidateSql;
                    UseCurrentTransaction(discover);
                    AddParameter(discover, "@masterTable", qualifiedMasterTable);
                    for (var i = 0; i < names.Length; i++)
                    {
                        AddParameter(discover, $"@column{i}", names[i]);
                    }

                    await using var reader =
                        await discover.ExecuteReaderAsync(cancellationToken);
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        references.Add((
                            reader.GetString(0),
                            reader.GetString(1),
                            reader.GetString(2)));
                    }
                }

                foreach (var reference in references)
                {
                    // Only catalog-provided schema/table/column identifiers are quoted;
                    // the location ID is a SQL parameter, not string interpolation.
                    var tableIdentifier =
                        $"{QuoteIdentifier(reference.Schema)}.{QuoteIdentifier(reference.Table)}";
                    var columnIdentifier = QuoteIdentifier(reference.Column);

                    await using var exists = connection.CreateCommand();
                    exists.CommandText = $@"
SELECT EXISTS (
    SELECT 1
    FROM {tableIdentifier}
    WHERE {columnIdentifier} = @locationId
);";
                    UseCurrentTransaction(exists);
                    AddParameter(exists, "@locationId", id);

                    if (await exists.ExecuteScalarAsync(cancellationToken) is true)
                    {
                        var displayTable = reference.Schema == "public"
                            ? reference.Table
                            : $"{reference.Schema}.{reference.Table}";
                        dependencies.Add(displayTable);
                    }
                }
            }
            finally
            {
                if (openedHere)
                {
                    await _db.Database.CloseConnectionAsync();
                }
            }

            return dependencies.OrderBy(name => name).ToList();
        }

        private void UseCurrentTransaction(DbCommand command)
        {
            var transaction = _db.Database.CurrentTransaction;
            if (transaction != null)
            {
                command.Transaction = transaction.GetDbTransaction();
            }
        }

        private static void AddParameter(DbCommand command, string name, object value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            command.Parameters.Add(parameter);
        }

        private static string QuoteIdentifier(string identifier) =>
            "\"" + identifier.Replace("\"", "\"\"") + "\"";

        // Compatibility fallback for non-PostgreSQL EF Core providers. This keeps
        // the original named-property behavior without changing other project flows.
        private async Task<List<string>> CheckEfMappedDependenciesAsync(int id)
        {
            var dependencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var anyAsync = typeof(EntityFrameworkQueryableExtensions).GetMethods()
                .First(m => m.Name == nameof(EntityFrameworkQueryableExtensions.AnyAsync)
                    && m.IsGenericMethodDefinition
                    && m.GetParameters().Length == 3);

            foreach (var entityType in _db.Model.GetEntityTypes())
            {
                var clrType = entityType.ClrType;
                if (clrType == typeof(T) ||
                    clrType == typeof(Dictionary<string, object>) ||
                    entityType.IsOwned() ||
                    entityType.HasSharedClrType)
                {
                    continue;
                }

                foreach (var columnName in _fkColumnNames)
                {
                    var property = entityType.FindProperty(columnName);
                    if (property == null ||
                        (property.ClrType != typeof(int) && property.ClrType != typeof(int?)))
                    {
                        continue;
                    }

                    var setMethod = typeof(DbContext).GetMethod(
                        nameof(DbContext.Set), Type.EmptyTypes)?.MakeGenericMethod(clrType);
                    if (setMethod == null) continue;

                    var queryable = (IQueryable)setMethod.Invoke(_db, null)!;
                    var parameter = Expression.Parameter(clrType, "e");
                    var efProperty = typeof(EF).GetMethod(nameof(EF.Property))!
                        .MakeGenericMethod(property.ClrType);
                    var left = Expression.Call(
                        null, efProperty, parameter, Expression.Constant(columnName));
                    Expression right = Expression.Constant(id);
                    if (property.ClrType == typeof(int?))
                    {
                        right = Expression.Convert(right, typeof(int?));
                    }

                    var predicate = Expression.Lambda(
                        Expression.Equal(left, right), parameter);
                    var genericAny = anyAsync.MakeGenericMethod(clrType);
                    var task = (Task<bool>)genericAny.Invoke(null,
                        new object[] { queryable, predicate, CancellationToken.None })!;

                    if (await task)
                    {
                        dependencies.Add(entityType.GetTableName() ?? clrType.Name);
                    }
                }
            }

            return dependencies.OrderBy(name => name).ToList();
        }
    }
}
