#if WEB_APP
namespace Cutulu.Web;

[System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = false)]
public class RouteHttp : System.Attribute
{
    public readonly string Route;

    /// <summary>
    /// The path's {arg_name}s have to match the method's parameters. Mapping is done by system on application startup.
    /// </summary>
    public RouteHttp(string route = "/install/{name}:{age}")
    {
        Route = route;
    }
}
#endif