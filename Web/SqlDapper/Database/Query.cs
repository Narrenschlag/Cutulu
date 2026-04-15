#if SQL_DAPPER
namespace Cutulu.Web;

using System.Runtime.CompilerServices;
using Dapper;

public partial class DatabaseClient : IAsyncDisposable
{
    /// <summary>Plain SQL query. Returns changed/affected-row count. SELECT does not affect any rows.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<int> Query(string sql, object? param = null)
    {
        await using var conn = await OpenAsync();
        return await conn.ExecuteAsync(sql, param);
    }

    /// <summary>Fetch a single scalar value (COUNT, MAX, a name, etc.).</summary>
    public async Task<T?> Fetch<T>(string sql, object? param = null)
    {
        await using var conn = await OpenAsync();
        return await conn.ExecuteScalarAsync<T>(sql, param);
    }

    /// <summary>Returns the number of rows.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<int> GetRowCount(string table, string whereSql = "", object? param = null)
    {
        return await Fetch<int>($"SELECT COUNT(*) FROM {table} {whereSql}", param);
    }

    /// <summary>Returns the number of rows.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<bool> HasAnyRow(string table, string whereSql = "", object? param = null)
    {
        return await GetRowCount(table, whereSql, param) > 0;
    }

    /// <summary>Returns the number of affected rows.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<int> GetAffectedRowCount(string sql, object? param = null) => await Query(sql, param);

    /// <summary>Return true if the SQL statement affected any rows.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<bool> AffectsAnyRow(string sql, object? param = null) => await GetAffectedRowCount(sql, param) > 0;

    /// <summary>Run an INSERT. Returns the new auto-increment ID.</summary>
    public async Task<long> InsertRow(string sql, object param)
    {
        await using var conn = await OpenAsync();
        await conn.ExecuteAsync(sql, param);
        return await conn.ExecuteScalarAsync<long>("SELECT LAST_INSERT_ID()");
    }

    /// <summary>Fetch the first matching row, or null if not found.</summary>
    public async Task<T?> FetchFirstRow<T>(string sql, object? param = null)
    {
        await using var conn = await OpenAsync();
        return await conn.QueryFirstOrDefaultAsync<T>(sql, param);
    }

    /// <summary>Fetch a single matching row, or null if not found.</summary>
    public async Task<T?> FetchSingleRow<T>(string sql, object? param = null)
    {
        await using var conn = await OpenAsync();
        return await conn.QuerySingleOrDefaultAsync<T>(sql, param);
    }

    /// <summary>Fetch all matching rows as an enumerable.</summary>
    public async Task<IEnumerable<T>> FetchRows<T>(string sql, object? param = null)
    {
        await using var conn = await OpenAsync();
        return await conn.QueryAsync<T>(sql, param);
    }

    /// <summary>Returns all matching rows as a List.</summary>
    public async Task<List<T>> FetchRowsAsList<T>(string sql, object? param = null) => (await FetchRows<T>(sql, param)).ToList();
}
#endif