#if WEB_APP
namespace Cutulu.Web;

using System.Reflection;
using System;
using Core;

public static class RouteHttpFetcher
{
    public record RouteEntry(MethodInfo Method, RouteHttp Attribute, Type DeclaringType);

    /// <summary>
    /// Scans all loaded assemblies and returns every method decorated with [RouteHttp].
    /// </summary>
    public static IEnumerable<RouteEntry> GetAllRoutes()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var type in assembly.GetTypes())
            {
                foreach (var method in type.GetMethods(
                    BindingFlags.Instance |
                    BindingFlags.Static |
                    BindingFlags.Public |
                    BindingFlags.NonPublic))
                {
                    var attr = method.GetCustomAttribute<RouteHttp>();
                    if (attr is not null)
                        yield return new RouteEntry(method, attr, type);
                }
            }
        }
    }

    public static void RegisterRoutes(WebApplication app)
    {
        var routes = RouteHttpFetcher.GetAllRoutes();

        foreach (var route in routes)
        {
            if (!route.Method.IsStatic) continue;

            var parameters = route.Method.GetParameters();
            var typeArgs = parameters
                .Select(p => p.ParameterType)
                .ToArray();

            Delegate handler;

            if (route.Method.ReturnType == typeof(void))
            {
                var delegateType = System.Linq.Expressions.Expression.GetActionType(typeArgs);
                handler = route.Method.CreateDelegate(delegateType);
            }
            else
            {
                var delegateType = System.Linq.Expressions.Expression.GetFuncType(
                    [.. typeArgs, route.Method.ReturnType] // append return type at end
                );
                handler = route.Method.CreateDelegate(delegateType);
            }

            app.MapGet(route.Attribute.Route, handler);
        }
    }
}
#endif