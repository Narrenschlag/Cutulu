#if WEB_APP
namespace Cutulu.Web;

using Core;

public class WebApp : IAsyncDisposable
{
    public readonly WebApplication App;

    public WebApp(string host, int port, Delegate? defaultRouting = null)
    {
        App = WebApplication.Create();

        if (defaultRouting != null)
            App.MapGet("/", defaultRouting);

        RouteHttpFetcher.RegisterRoutes(App);

        Debug.Log($"Starting web server on {host}:{port}");
        App.Run($"{host}:{port}");
    }

    public ValueTask DisposeAsync()
    {
        return App.NotNull() ?
            new(DisposeInternalAsync()) :
            ValueTask.CompletedTask;
    }

    private async Task DisposeInternalAsync()
    {
        await App.StopAsync();
        await App.DisposeAsync();
    }

    /* SAMPLES
    [RouteHttp("/login/{username}")]
    public static async Task<IResult> CanLogin(string username, HttpContext http)
    {
        var decoder = await LocalDecoder.Create(http);

        if (decoder.TryDecode(out string pwd) == false) return Results.Unauthorized();

        var pwdHash = await db.FetchFirstAsync<string>(
            $"SELECT pwd FROM users WHERE name = '{username}'"
        );

        if (pwd.EncryptStringGcm(username) != pwdHash) return Results.Unauthorized();

        return Results.Ok();
    }

    [RouteHttp("/user/{username}")]
    public static async Task<string> GetUsers(string username)
    {
        await db.EnsureColumnAsync("users", new ColumnDef("pwd", "TEXT", false, false));

        var email = await db.FetchFirstAsync<string>(
            $"SELECT email FROM users WHERE BINARY name = '{username}'" // BINARY: Case insensitive
        );

        return string.Join("\n", email);
    }

    [RouteHttp("/users")]
    public static async Task<string> GetUsers()
    {
        var users = await db.FetchListAsync<(int Id, string Name, string Email)>(
            $"SELECT * FROM users"
        );

        return string.Join("\n", users.Select(u => $"{u.Id}: {u.Name} - {u.Email}"));
    }
    */
}
#endif