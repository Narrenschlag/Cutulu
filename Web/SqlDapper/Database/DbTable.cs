#if SQL_DAPPER
namespace Cutulu.Web;

using Cutulu.Core;
using Dapper;

public record DbTable(string Name);

public partial class DatabaseClient
{
    #region Get

    public async Task<List<string>> GetTables()
    {
        const string sql = @"
            SELECT TABLE_NAME
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = DATABASE();
        ";

        await using var conn = await OpenAsync();
        var result = await conn.QueryAsync<string>(sql);
        return result.ToList();
    }

    public async Task<bool> TableExists(string table)
    {
        const string sql = @"
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = DATABASE()
            AND TABLE_NAME = @table;
        ";

        await using var conn = await OpenAsync();
        return await conn.ExecuteScalarAsync<int>(sql, new { table }) > 0;
    }

    public async Task<DbTable?> GetTable(string table)
    {
        const string sql = @"
            SELECT TABLE_NAME AS Name
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = DATABASE()
            AND TABLE_NAME = @table;
        ";

        await using var conn = await OpenAsync();
        return await conn.QueryFirstOrDefaultAsync<DbTable>(sql, new { table });
    }

    #endregion

    #region Create

    public async Task CreateTable(
    string table,
    IEnumerable<ColumnDef> columns,
    string? primaryKey = null,
    params string[] indexes          // ← add
)
    {
        var cols = columns.Select(c =>
        {
            var sql = $"`{c.Name}` {c.Type}";
            if (!c.Nullable) sql += " NOT NULL";
            if (c.AutoIncrement) sql += " AUTO_INCREMENT";
            if (c.Default != null) sql += $" DEFAULT {c.Default}";
            return sql;
        });

        var sqlBuilder = new List<string>();
        sqlBuilder.Add($"CREATE TABLE IF NOT EXISTS `{table}` (");
        sqlBuilder.Add(string.Join(",\n", cols));

        if (primaryKey != null)
            sqlBuilder.Add($", PRIMARY KEY (`{primaryKey}`)");

        sqlBuilder.Add(") ENGINE=InnoDB;");

        await Query(string.Join("\n", sqlBuilder));

        // Apply indexes after table creation
        await EnsureIndexes(table, indexes);
    }

    public async Task EnsureTable(
        string table,
        IEnumerable<ColumnDef> columns,
        string? primaryKey = null,
        params string[] indexes
    )
    {
        if (!await TableExists(table))
        {
            await CreateTable(table, columns, primaryKey, indexes); // ← pass indexes
            return;
        }

        const string colSql = @"
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE()
            AND TABLE_NAME = @table;
        ";

        await using var conn = await OpenAsync();
        var existingCols = (await conn.QueryAsync<string>(colSql, new { table }))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var col in columns)
        {
            if (existingCols.Contains(col.Name)) continue;

            var sql = $"ALTER TABLE `{table}` ADD COLUMN `{col.Name}` {col.Type}";
            if (!col.Nullable) sql += " NOT NULL";
            if (col.AutoIncrement) sql += " AUTO_INCREMENT";
            if (col.Default != null) sql += $" DEFAULT {col.Default}";

            await conn.ExecuteAsync(sql);
        }

        if (!string.IsNullOrEmpty(primaryKey))
        {
            const string pkSql = @"
                SELECT COUNT(*)
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
                WHERE TABLE_SCHEMA = DATABASE()
                AND TABLE_NAME = @table
                AND CONSTRAINT_TYPE = 'PRIMARY KEY';
            ";

            var hasPk = await conn.ExecuteScalarAsync<int>(pkSql, new { table });
            if (hasPk == 0)
                await conn.ExecuteAsync(
                    $"ALTER TABLE `{table}` ADD PRIMARY KEY (`{primaryKey}`)"
                );
        }

        await EnsureIndexes(table, indexes);   // ← extracted, shared with CreateTable
    }

    // ─────────────────────────────────────────────
    // Shared index logic
    // ─────────────────────────────────────────────
    private async Task EnsureIndexes(string table, string[] indexes)
    {
        if (indexes.Length == 0) return;

        await using var conn = await OpenAsync();

        foreach (var index in indexes)
        {
            // Case-insensitive split on INDEX keyword
            var parts = index.Split("INDEX", CONST.StringSplit);
            if (parts.Length < 2) continue;

            var indexName = parts[1].Trim().Split(' ')[0];
            if (string.IsNullOrWhiteSpace(indexName)) continue;

            const string idxSql = @"
                SELECT COUNT(*)
                FROM INFORMATION_SCHEMA.STATISTICS
                WHERE TABLE_SCHEMA = DATABASE()
                AND TABLE_NAME = @table
                AND INDEX_NAME = @indexName;
            ";

            var exists = await conn.ExecuteScalarAsync<int>(idxSql, new { table, indexName });
            if (exists == 0)
                await conn.ExecuteAsync($"ALTER TABLE `{table}` ADD {index};");
        }
    }

    #endregion

    #region Drop

    public async Task DropTable(string table)
    {
        var sql = $"DROP TABLE IF EXISTS `{table}`;";
        await Query(sql);
    }

    #endregion
}
#endif