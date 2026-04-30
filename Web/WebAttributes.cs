#if WEB_APP
namespace Cutulu.Web;

using System;

[AttributeUsage(AttributeTargets.Method)]
public class RouteHttp(string route, bool requireAuth = false, bool allowRemoteAccess = true, RouteHttpMethod method = RouteHttpMethod.Get, uint mask = uint.MaxValue) : Attribute
{
    public readonly uint Mask = mask;
    public readonly string Route = route;
    public readonly RouteHttpMethod Method = method;
    public readonly bool RequireAuth = requireAuth;
    public readonly bool AllowRemoteAccess = allowRemoteAccess;

    public RouteHttp(string route, uint mask) : this(route, false, true, RouteHttpMethod.Get, mask) { }
}

public enum RouteHttpMethod { Get, Post, Put, Patch, Delete }
#endif