#if SQL_DAPPER
namespace Cutulu.Web;

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
    string? primaryKey = null)
    {
        var cols = columns.Select(c =>
        {
            var sql = $"`{c.Name}` {c.Type}";

            if (!c.Nullable)
                sql += " NOT NULL";

            if (c.AutoIncrement)
                sql += " AUTO_INCREMENT";

            if (c.Default != null)
                sql += $" DEFAULT {c.Default}";

            return sql;
        });

        var sqlBuilder = new List<string>();
        sqlBuilder.Add($"CREATE TABLE IF NOT EXISTS `{table}` (");
        sqlBuilder.Add(string.Join(",\n", cols));

        if (primaryKey != null)
            sqlBuilder.Add($", PRIMARY KEY (`{primaryKey}`)");

        sqlBuilder.Add(") ENGINE=InnoDB;");

        var sql = string.Join("\n", sqlBuilder);

        await Query(sql);
    }

    public async Task EnsureTable(
    string table,
    IEnumerable<ColumnDef> columns,
    string? primaryKey = null
)
    {
        // ─────────────────────────────────────────────
        // 1. Create table if it doesn't exist
        // ─────────────────────────────────────────────
        if (!await TableExists(table))
        {
            await CreateTable(table, columns, primaryKey);
            return;
        }

        // ─────────────────────────────────────────────
        // 2. Get existing columns
        // ─────────────────────────────────────────────
        const string colSql = @"
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE()
            AND TABLE_NAME = @table;
        ";

        await using var conn = await OpenAsync();
        var existingCols = (await conn.QueryAsync<string>(colSql, new { table }))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // ─────────────────────────────────────────────
        // 3. Add missing columns
        // ─────────────────────────────────────────────
        foreach (var col in columns)
        {
            if (existingCols.Contains(col.Name))
                continue;

            var sql = $"ALTER TABLE `{table}` ADD COLUMN `{col.Name}` {col.Type}";

            if (!col.Nullable)
                sql += " NOT NULL";

            if (col.AutoIncrement)
                sql += " AUTO_INCREMENT";

            if (col.Default != null)
                sql += $" DEFAULT {col.Default}";

            await conn.ExecuteAsync(sql);
        }

        // ─────────────────────────────────────────────
        // 4. Ensure primary key exists (safe check)
        // ─────────────────────────────────────────────
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
            {
                var sql = $"ALTER TABLE `{table}` ADD PRIMARY KEY (`{primaryKey}`)";
                await conn.ExecuteAsync(sql);
            }
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