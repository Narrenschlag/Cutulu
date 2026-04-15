#if SQL_DAPPER
namespace Cutulu.Web;

using Dapper;

public record DbColumn(
    string Name,
    string DataType,
    string IsNullable,
    string? DefaultValue,
    string? KeyType,
    int? MaxLength
);

public record ColumnDef(
    string Name,
    string Type,
    bool Nullable = true,
    bool AutoIncrement = false,
    string? Default = null
);

public partial class DatabaseClient
{
    #region Get

    public async Task<List<DbColumn>> GetColumnsAsync(string table)
    {
        const string sql = @"
            SELECT 
                COLUMN_NAME      AS Name,
                DATA_TYPE        AS DataType,
                IS_NULLABLE      AS IsNullable,
                COLUMN_DEFAULT   AS DefaultValue,
                COLUMN_KEY       AS KeyType,
                CHARACTER_MAXIMUM_LENGTH AS MaxLength
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE()
            AND TABLE_NAME = @table
            ORDER BY ORDINAL_POSITION;
        ";

        await using var conn = await OpenAsync();
        var result = await conn.QueryAsync<DbColumn>(sql, new { table });
        return result.ToList();
    }

    public async Task<bool> ColumnExistsAsync(string table, string column)
    {
        const string sql = @"
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE()
            AND TABLE_NAME = @table
            AND COLUMN_NAME = @column;
        ";

        await using var conn = await OpenAsync();
        return await conn.ExecuteScalarAsync<int>(sql, new { table, column }) > 0;
    }

    public async Task<DbColumn?> GetColumnAsync(string table, string column)
    {
        const string sql = @"
            SELECT 
                COLUMN_NAME      AS Name,
                DATA_TYPE        AS DataType,
                IS_NULLABLE      AS IsNullable,
                COLUMN_DEFAULT   AS DefaultValue,
                COLUMN_KEY       AS KeyType,
                CHARACTER_MAXIMUM_LENGTH AS MaxLength
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE()
            AND TABLE_NAME = @table
            AND COLUMN_NAME = @column;
        ";

        await using var conn = await OpenAsync();
        return await conn.QueryFirstOrDefaultAsync<DbColumn>(sql, new { table, column });
    }

    public async Task<List<string>> GetPrimaryKeysAsync(string table)
    {
        const string sql = @"
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
            WHERE TABLE_SCHEMA = DATABASE()
            AND TABLE_NAME = @table
            AND CONSTRAINT_NAME = 'PRIMARY'
            ORDER BY ORDINAL_POSITION;
        ";

        await using var conn = await OpenAsync();
        var result = await conn.QueryAsync<string>(sql, new { table });
        return result.ToList();
    }

    public async Task<List<string>> GetIndexesAsync(string table)
    {
        const string sql = @"
            SELECT DISTINCT INDEX_NAME
            FROM INFORMATION_SCHEMA.STATISTICS
            WHERE TABLE_SCHEMA = DATABASE()
            AND TABLE_NAME = @table;
        ";

        await using var conn = await OpenAsync();
        var result = await conn.QueryAsync<string>(sql, new { table });
        return result.ToList();
    }

    #endregion

    #region Update

    public async Task AddColumnAsync(string table, ColumnDef column)
    {
        var sql = $@"
            ALTER TABLE `{table}`
            ADD COLUMN `{column.Name}` {column.Type}
            {(column.Nullable ? "" : "NOT NULL")}
            {(column.Default != null ? $"DEFAULT {column.Default}" : "")};
        ";

        await ExecuteAsync(sql);
    }

    public async Task EnsureColumnAsync(string table, ColumnDef column)
    {
        if (!await ColumnExistsAsync("users", column.Name))
        {
            await AddColumnAsync("users", column);
        }
    }

    public async Task ModifyColumnAsync(string table, ColumnDef column)
    {
        var sql = $@"
            ALTER TABLE `{table}`
            MODIFY COLUMN `{column.Name}` {column.Type}
            {(column.Nullable ? "" : "NOT NULL")}
            {(column.Default != null ? $"DEFAULT {column.Default}" : "")};
        ";

        await ExecuteAsync(sql);
    }

    public async Task DropColumnAsync(string table, string column)
    {
        var sql = $@"
            ALTER TABLE `{table}`
            DROP COLUMN `{column}`;
        ";

        await ExecuteAsync(sql);
    }

    public async Task AddIndexAsync(string table, string indexName, params string[] columns)
    {
        var cols = string.Join(", ", columns.Select(c => $"`{c}`"));

        var sql = $@"
            ALTER TABLE `{table}`
            ADD INDEX `{indexName}` ({cols});
        ";

        await ExecuteAsync(sql);
    }

    public async Task AddPrimaryKeyAsync(string table, params string[] columns)
    {
        var cols = string.Join(", ", columns.Select(c => $"`{c}`"));

        var sql = $@"
            ALTER TABLE `{table}`
            ADD PRIMARY KEY ({cols});
        ";

        await ExecuteAsync(sql);
    }

    #endregion
}
#endif