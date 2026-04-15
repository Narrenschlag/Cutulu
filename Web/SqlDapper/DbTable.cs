#if SQL_DAPPER
namespace Cutulu.Web;

using Dapper;

public record DbTable(string Name);

public partial class DatabaseClient
{
    #region Get

    public async Task<List<string>> GetTablesAsync()
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

    public async Task<bool> TableExistsAsync(string table)
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

    public async Task<DbTable?> GetTableAsync(string table)
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

    public async Task CreateTableAsync(
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

        await ExecuteAsync(sql);
    }

    public async Task EnsureTableAsync(
        string table,
        IEnumerable<ColumnDef> columns,
        string? primaryKey = null
    )
    {
        if (!await TableExistsAsync(table))
        {
            await CreateTableAsync(table, columns, primaryKey);
        }
    }

    #endregion

    #region Drop

    public async Task DropTableAsync(string table)
    {
        var sql = $"DROP TABLE IF EXISTS `{table}`;";
        await ExecuteAsync(sql);
    }

    #endregion
}
#endif