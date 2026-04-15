#if SQL_DAPPER
namespace Cutulu.Web;

using System.Runtime.CompilerServices;
using MySqlConnector;
using System.Data;
using Dapper;

/// <summary>
/// A generic async MariaDB/MySQL client built on Dapper.
/// Covers: single fetch, list fetch, paged fetch, insert, update, delete,
/// bulk insert, upsert, scalar queries, raw SQL, and transactions.
/// </summary>
public partial class DatabaseClient : IAsyncDisposable
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