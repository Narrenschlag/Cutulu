#if SQL_DAPPER
namespace Cutulu.Web;

using MySqlConnector;
using System.Data;
using Dapper;

/// <summary>
/// A generic async MariaDB/MySQL client built on Dapper.
/// Covers: single fetch, list fetch, paged fetch, insert, update, delete,
/// bulk insert, upsert, scalar queries, raw SQL, and transactions.
/// </summary>
public class DatabaseClient : IAsyncDisposable
{
    private readonly string ConnectString,
        Host, Database, User, Password;
    private readonly int Port;

    public DatabaseClient(
        string host = "localhost",
        int port = 3306,
        string database = "mydb",
        string user = "myuser",
        string password = "mypassword"
    )
    {
        ConnectString = $"Server={host};Port={port};Database={database};User={user};Password={password};";

        Host = host;
        Port = port;

        Database = database;

        User = user;
        Password = password;
    }

    // ── Connection factory ────────────────────────────────────────────────────

    private async Task<MySqlConnection> OpenAsync()
    {
        var conn = new MySqlConnection(ConnectString);
        await conn.OpenAsync();
        return conn;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // PHP-style simple API
    // ─────────────────────────────────────────────────────────────────────────
    // Short names, anonymous objects for params, predictable return types.
    //
    //   var user  = await db.fetchOne<User>("SELECT * FROM users WHERE id = @id", new { id = 5 });
    //   var users = await db.fetchAll<User>("SELECT * FROM users WHERE active = @active", new { active = true });
    //   var count = await db.fetchValue<int>("SELECT COUNT(*) FROM users");
    //   long id   = await db.insert("INSERT INTO users (name, email) VALUES (@name, @email)", new { name, email });
    //   int  rows = await db.update("UPDATE users SET name = @name WHERE id = @id", new { name, id });
    //   int  rows = await db.delete("DELETE FROM users WHERE id = @id", new { id });
    //   int  rows = await db.query("ALTER TABLE users ADD COLUMN bio TEXT");
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Fetch the first matching row, or null if not found.</summary>
    public async Task<T?> fetchOne<T>(string sql, object? param = null)
    {
        await using var conn = await OpenAsync();
        return await conn.QueryFirstOrDefaultAsync<T>(sql, param);
    }

    /// <summary>Fetch all matching rows as a list.</summary>
    public async Task<List<T>> fetchAll<T>(string sql, object? param = null)
    {
        await using var conn = await OpenAsync();
        return (await conn.QueryAsync<T>(sql, param)).ToList();
    }

    /// <summary>Fetch a single scalar value (COUNT, MAX, a name, etc.).</summary>
    public async Task<T?> fetchValue<T>(string sql, object? param = null)
    {
        await using var conn = await OpenAsync();
        return await conn.ExecuteScalarAsync<T>(sql, param);
    }

    /// <summary>Run an INSERT. Returns the new auto-increment ID.</summary>
    public async Task<long> insert(string sql, object param)
    {
        await using var conn = await OpenAsync();
        await conn.ExecuteAsync(sql, param);
        return await conn.ExecuteScalarAsync<long>("SELECT LAST_INSERT_ID()");
    }

    /// <summary>Run an UPDATE. Returns the number of affected rows.</summary>
    public async Task<int> update(string sql, object param)
    {
        await using var conn = await OpenAsync();
        return await conn.ExecuteAsync(sql, param);
    }

    /// <summary>Run a DELETE. Returns the number of affected rows.</summary>
    public async Task<int> delete(string sql, object? param = null)
    {
        await using var conn = await OpenAsync();
        return await conn.ExecuteAsync(sql, param);
    }

    /// <summary>Run any SQL that doesn't return rows (DDL, stored procs, etc.). Returns affected-row count.</summary>
    public async Task<int> query(string sql, object? param = null)
    {
        await using var conn = await OpenAsync();
        return await conn.ExecuteAsync(sql, param);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Full async API
    // ═════════════════════════════════════════════════════════════════════════

    // ── Fetch: single row ─────────────────────────────────────────────────────

    /// <summary>Returns a single T or null. Throws if more than one row is found.</summary>
    public async Task<T?> FetchOneAsync<T>(string sql, object? param = null)
    {
        await using var conn = await OpenAsync();
        return await conn.QuerySingleOrDefaultAsync<T>(sql, param);
    }

    /// <summary>Returns the first row or null (ignores extras).</summary>
    public async Task<T?> FetchFirstAsync<T>(string sql, object? param = null)
    {
        await using var conn = await OpenAsync();
        return await conn.QueryFirstOrDefaultAsync<T>(sql, param);
    }

    // ── Fetch: multiple rows ──────────────────────────────────────────────────

    /// <summary>Returns all matching rows as an IEnumerable.</summary>
    public async Task<IEnumerable<T>> FetchManyAsync<T>(string sql, object? param = null)
    {
        await using var conn = await OpenAsync();
        return await conn.QueryAsync<T>(sql, param);
    }

    /// <summary>Returns all matching rows as a List.</summary>
    public async Task<List<T>> FetchListAsync<T>(string sql, object? param = null)
    {
        var result = await FetchManyAsync<T>(sql, param);
        return result.ToList();
    }

    // ── Fetch: paged ──────────────────────────────────────────────────────────

    public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
    {
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasNext => Page < TotalPages;
        public bool HasPrev => Page > 1;
    }

    /// <summary>
    /// Returns a page of results plus total row count.
    /// <paramref name="countSql"/> should be a COUNT(*) version of the same query.
    /// </summary>
    public async Task<PagedResult<T>> FetchPagedAsync<T>(
        string dataSql,
        string countSql,
        int page,
        int pageSize,
        object? param = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        var pagedParam = new DynamicParameters(param);
        pagedParam.Add("Limit", pageSize);
        pagedParam.Add("Offset", (page - 1) * pageSize);

        await using var conn = await OpenAsync();
        var items = await conn.QueryAsync<T>($"{dataSql} LIMIT @Limit OFFSET @Offset", pagedParam);
        var totalCount = await conn.ExecuteScalarAsync<int>(countSql, param);

        return new PagedResult<T>(items.ToList(), totalCount, page, pageSize);
    }

    // ── Scalar ────────────────────────────────────────────────────────────────

    /// <summary>Returns a single scalar value (e.g. COUNT, MAX, a generated ID).</summary>
    public async Task<T?> ScalarAsync<T>(string sql, object? param = null)
    {
        await using var conn = await OpenAsync();
        return await conn.ExecuteScalarAsync<T>(sql, param);
    }

    // ── Insert ────────────────────────────────────────────────────────────────

    /// <summary>Executes an INSERT and returns the new auto-increment ID.</summary>
    public async Task<long> InsertAsync(string sql, object param)
    {
        await using var conn = await OpenAsync();
        await conn.ExecuteAsync(sql, param);
        return await conn.ExecuteScalarAsync<long>("SELECT LAST_INSERT_ID()");
    }

    /// <summary>
    /// Convenience insert: builds the INSERT from a dictionary of column→value pairs.
    /// Returns the new auto-increment ID.
    /// </summary>
    public async Task<long> InsertDictAsync(string table, Dictionary<string, object?> values)
    {
        var cols = string.Join(", ", values.Keys.Select(k => $"`{k}`"));
        var pars = string.Join(", ", values.Keys.Select(k => $"@{k}"));
        var sql = $"INSERT INTO `{table}` ({cols}) VALUES ({pars})";

        await using var conn = await OpenAsync();
        await conn.ExecuteAsync(sql, values);
        return await conn.ExecuteScalarAsync<long>("SELECT LAST_INSERT_ID()");
    }

    // ── Upsert (INSERT … ON DUPLICATE KEY UPDATE) ─────────────────────────────

    /// <summary>
    /// Upsert using MariaDB's INSERT … ON DUPLICATE KEY UPDATE.
    /// <paramref name="updateCols"/> are the columns to update on conflict.
    /// </summary>
    public async Task<int> UpsertAsync(
        string table,
        Dictionary<string, object?> values,
        IEnumerable<string> updateCols)
    {
        var cols = string.Join(", ", values.Keys.Select(k => $"`{k}`"));
        var pars = string.Join(", ", values.Keys.Select(k => $"@{k}"));
        var updates = string.Join(", ", updateCols.Select(c => $"`{c}` = VALUES(`{c}`)"));
        var sql = $"INSERT INTO `{table}` ({cols}) VALUES ({pars}) ON DUPLICATE KEY UPDATE {updates}";

        await using var conn = await OpenAsync();
        return await conn.ExecuteAsync(sql, values);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    /// <summary>Executes an UPDATE and returns the number of affected rows.</summary>
    public async Task<int> UpdateAsync(string sql, object param)
    {
        await using var conn = await OpenAsync();
        return await conn.ExecuteAsync(sql, param);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    /// <summary>Executes a DELETE and returns the number of affected rows.</summary>
    public async Task<int> DeleteAsync(string sql, object? param = null)
    {
        await using var conn = await OpenAsync();
        return await conn.ExecuteAsync(sql, param);
    }

    // ── Bulk insert ───────────────────────────────────────────────────────────

    /// <summary>
    /// Inserts a collection of rows in a single transaction.
    /// Returns total rows inserted.
    /// </summary>
    public async Task<int> BulkInsertAsync<T>(string sql, IEnumerable<T> rows)
    {
        await using var conn = await OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            int count = await conn.ExecuteAsync(sql, rows, tx);
            await tx.CommitAsync();
            return count;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // ── Multi-query (split results) ───────────────────────────────────────────

    /// <summary>
    /// Executes multiple SELECT statements in one round-trip.
    /// Usage: var (users, orders) = await db.FetchMultipleAsync(...);
    /// </summary>
    public async Task<(List<TFirst>, List<TSecond>)> FetchMultipleAsync<TFirst, TSecond>(
        string sql,
        object? param = null)
    {
        await using var conn = await OpenAsync();
        await using var multi = await conn.QueryMultipleAsync(sql, param);
        var first = (await multi.ReadAsync<TFirst>()).ToList();
        var second = (await multi.ReadAsync<TSecond>()).ToList();
        return (first, second);
    }

    // ── Transaction scope ─────────────────────────────────────────────────────

    /// <summary>
    /// Runs <paramref name="work"/> inside a transaction.
    /// Commits on success, rolls back and rethrows on any exception.
    /// </summary>
    public async Task TransactionAsync(Func<IDbConnection, IDbTransaction, Task> work)
    {
        await using var conn = await OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            await work(conn, tx);
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    /// <summary>Transaction overload that returns a value.</summary>
    public async Task<T> TransactionAsync<T>(Func<IDbConnection, IDbTransaction, Task<T>> work)
    {
        await using var conn = await OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            T result = await work(conn, tx);
            await tx.CommitAsync();
            return result;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // ── Raw execute (DDL, stored procs, etc.) ─────────────────────────────────

    /// <summary>Executes any SQL that doesn't return rows. Returns affected-row count.</summary>
    public async Task<int> ExecuteAsync(string sql, object? param = null)
    {
        await using var conn = await OpenAsync();
        return await conn.ExecuteAsync(sql, param);
    }

    // ── Health check ──────────────────────────────────────────────────────────

    /// <summary>Returns true if a connection can be opened and a ping succeeds.</summary>
    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            await using var conn = await OpenAsync();
            return await conn.PingAsync();
        }
        catch
        {
            return false;
        }
    }

    // ── IAsyncDisposable ──────────────────────────────────────────────────────

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
#endif