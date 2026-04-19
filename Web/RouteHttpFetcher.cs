#if WEB_APP
namespace Cutulu.Web;

using System.Reflection;
using System.Net;
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

    public static void RegisterRoutes(
        WebApplication app,
        Func<HttpContext, Task<bool>>? authHandler = null,
        Func<HttpContext, IResult>? onAuthFailed = null
    )
    {
        foreach (var route in GetAllRoutes())
        {
            if (!route.Method.IsStatic) continue;

            RequestDelegate requestDelegate = async (HttpContext ctx) =>
            {
                if (route.Attribute.RequireAuth && authHandler != null)
                {
                    if (!await authHandler(ctx))
                    {
                        await (
                            onAuthFailed?.Invoke(ctx) ??
                            Results.Unauthorized()
                        ).ExecuteAsync(ctx);
                        return;
                    }
                }

                // Build parameters
                var parameters = route.Method.GetParameters();
                var args = new object?[parameters.Length];

                for (int i = 0; i < parameters.Length; i++)
                {
                    var param = parameters[i];

                    if (param.ParameterType == typeof(HttpContext))
                    {
                        args[i] = ctx;
                    }
                    else if (ctx.Request.RouteValues.TryGetValue(param.Name!, out var routeVal))
                    {
                        args[i] = Convert.ChangeType(routeVal, param.ParameterType);
                    }
                    else if (param.ParameterType == typeof(CancellationToken))
                    {
                        args[i] = ctx.RequestAborted;
                    }
                    else
                    {
                        // Try to deserialize from body
                        args[i] = await ctx.Request.ReadFromJsonAsync(param.ParameterType);
                    }
                }

                // Invoke
                var result = route.Method.Invoke(null, args);

                // Await if async
                if (result is Task task)
                {
                    await task;

                    // Extract result from Task<T>
                    var taskType = task.GetType();
                    if (taskType.IsGenericType)
                        result = taskType.GetProperty("Result")!.GetValue(task);
                    else
                        return;
                }

                // Write result
                if (result is IResult iresult)
                    await iresult.ExecuteAsync(ctx);
                else if (result is string str)
                    await ctx.Response.WriteAsync(str);
                else if (result != null)
                    await ctx.Response.WriteAsJsonAsync(result);
            };

            // Map by HTTP method
            var builder = route.Attribute.Method switch
            {
                RouteHttpMethod.Get => app.MapGet(route.Attribute.Route, requestDelegate),
                RouteHttpMethod.Post => app.MapPost(route.Attribute.Route, requestDelegate),
                RouteHttpMethod.Put => app.MapPut(route.Attribute.Route, requestDelegate),
                RouteHttpMethod.Patch => app.MapPatch(route.Attribute.Route, requestDelegate),
                RouteHttpMethod.Delete => app.MapDelete(route.Attribute.Route, requestDelegate),
                _ => app.MapGet(route.Attribute.Route, requestDelegate)
            };

            // Restrict to loopback if remote access not allowed
            if (!route.Attribute.AllowRemoteAccess)
            {
                builder.AddEndpointFilter(async (ctx, next) =>
                {
                    var ip = ctx.HttpContext.Connection.RemoteIpAddress;
                    if (ip == null || !IPAddress.IsLoopback(ip))
                        return Results.StatusCode(403);

                    return await next(ctx);
                });
            }
        }
    }
}
#endif