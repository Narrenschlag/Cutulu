#if WEB_APP
namespace Cutulu.Web;

using Core;

public class WebApp
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
}
#endif