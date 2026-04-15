#if SQL_DAPPER
namespace Cutulu.Web;

using System.Collections.Concurrent;
using Encryption;

public class AuthService
{
    private readonly DatabaseClient _db;

    private readonly ConcurrentDictionary<string, (long userId, DateTime expires)> _cache = new();

    public AuthService(DatabaseClient db)
    {
        _db = db;
    }

    public async Task EnsureTableAsync()
    {
        // Users table
        await _db.EnsureTableAsync(
            "users",
            [
                new ColumnDef("id", "BIGINT", false, true),
                new ColumnDef("name", "VARCHAR(100)", false),
                new ColumnDef("email", "VARCHAR(100)", false),
                new ColumnDef("password_hash", "TEXT", false)
            ],
            "id"
        );

        // Tokens table
        await _db.EnsureTableAsync(
            "tokens",
            [
                new ColumnDef("id", "BIGINT", false, true),
                new ColumnDef("token", "VARCHAR(255)", false),
                new ColumnDef("user_id", "BIGINT", false),
                new ColumnDef("created_at", "DATETIME", false),
                new ColumnDef("expires_at", "DATETIME", false)
            ],
            "id"
        );
    }

    // Register
    public async Task<bool> RegisterAsync(string username, string password)
    {
        await EnsureTableAsync();
        var existing = await _db.fetchOne<long?>(
            "SELECT id FROM users WHERE name = @n",
            new { n = username }
        );

        if (existing != null)
            return false;

        var hash = SecureHash.HashPassword(password);

        await _db.insert(
            "INSERT INTO users (name, password_hash) VALUES (@n, @p)",
            new { n = username, p = hash }
        );

        return true;
    }

    // Login
    public async Task<string?> LoginAsync(string username, string password)
    {
        await EnsureTableAsync();
        var user = await _db.fetchOne<(long Id, string PasswordHash)>(
            "SELECT id, password_hash FROM users WHERE name = @n",
            new { n = username }
        );

        if (user == default)
            return null;

        if (!SecureHash.VerifyPassword(password, user.PasswordHash))
            return null;

        var token = CreateToken();
        var tokenHash = SecureHash.HashToken(token); // store hash
        var expires = DateTime.UtcNow.AddDays(7);
        await _db.insert(
            @"INSERT INTO tokens (token, user_id, created_at, expires_at) VALUES (@t, @u, NOW(), @e)",
            new { t = tokenHash, u = user.Id, e = expires }
        );
        _cache[token] = (user.Id, expires); // cache keeps real token

        return token;
    }

    // Validate token
    public async Task<long?> ValidateAsync(string token)
    {
        // fast path (cache)
        if (_cache.TryGetValue(token, out var cached))
        {
            if (cached.expires > DateTime.UtcNow)
                return cached.userId;

            _cache.TryRemove(token, out _);
        }

        var tokenHash = SecureHash.HashToken(token);
        var session = await _db.fetchOne<(long UserId, DateTime ExpiresAt)>(
            @"SELECT user_id, expires_at 
            FROM tokens 
            WHERE token = @t",
            new { t = tokenHash }
        );

        if (session == default || session.ExpiresAt <= DateTime.UtcNow)
            return null;

        _cache[token] = (session.UserId, session.ExpiresAt);

        return session.UserId;
    }

    // Logout (single session)
    public async Task LogoutAsync(string token)
    {
        _cache.TryRemove(token, out _);
        var tokenHash = SecureHash.HashToken(token);
        await _db.delete(
            "DELETE FROM tokens WHERE token = @t",
            new { t = tokenHash }
        );
    }

    // Logout all sessions for user
    public async Task LogoutAllAsync(long userId)
    {
        // remove from cache
        foreach (var key in _cache
            .Where(x => x.Value.userId == userId)
            .Select(x => x.Key)
            .ToList())
        {
            _cache.TryRemove(key, out _);
        }

        await _db.delete(
            "DELETE FROM tokens WHERE user_id = @u",
            new { u = userId }
        );
    }

    // Cleanup expired tokens
    public async Task CleanupAsync()
    {
        await _db.delete(
            "DELETE FROM tokens WHERE expires_at <= NOW()"
        );

        // clean cache
        var now = DateTime.UtcNow;

        foreach (var key in _cache
            .Where(x => x.Value.expires <= now)
            .Select(x => x.Key)
            .ToList())
        {
            _cache.TryRemove(key, out _);
        }
    }

    // Token generator
    private static string CreateToken(int size = 32)
    {
        return SecureHash.GenerateToken(size);
    }

    public void Apply(WebApplication app)
    {
        app.MapPost("/auth/register", async (AuthRegisterRequest req) =>
        {
            var ok = await RegisterAsync(req.Username, req.Password);
            return ok ? Results.Ok() : Results.BadRequest("User exists");
        });

        app.MapPost("/auth/login", async (AuthRegisterRequest req) =>
        {
            var token = await LoginAsync(req.Username, req.Password);
            return token != null
                ? Results.Ok(new { token })
                : Results.Unauthorized();
        });

        app.MapPost("/auth/logout", async (HttpContext ctx) =>
        {
            var token = GetToken(ctx);
            if (token == null) return Results.Unauthorized();

            await LogoutAsync(token);
            return Results.Ok();
        });

        app.MapGet("/me", (HttpContext ctx) =>
        {
            if (!ctx.Items.TryGetValue("UserId", out var userId))
                return Results.Unauthorized();

            return Results.Ok(new { userId });
        });

        app.Use(async (ctx, next) =>
        {
            var token = GetToken(ctx);

            if (token != null)
            {
                var userId = await ValidateAsync(token);

                if (userId != null)
                    ctx.Items["UserId"] = userId.Value;
            }

            await next();
        });

        // Clean up cycle
        _ = Task.Run(async () =>
        {
            while (true)
            {
                await CleanupAsync();
                await Task.Delay(TimeSpan.FromMinutes(10));
            }
        });
    }

    static string? GetToken(HttpContext ctx)
    {
        var auth = ctx.Request.Headers.Authorization.ToString();

        if (auth.StartsWith("Bearer "))
            return auth.Substring("Bearer ".Length);

        return null;
    }
}
#endif