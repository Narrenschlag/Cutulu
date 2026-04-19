#if WEB_APP
namespace Cutulu.Web;

public static class Cookief
{
    public static void InsertCookie(this HttpContext ctx, string cookieId, string dataStr)
    {
        ctx.Response.Cookies.Append(cookieId, dataStr, new CookieOptions
        {
            HttpOnly = true,
            Secure = false, //true,        // HTTPS only
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(7),
            Path = "/",
        });
    }

    public static void DeleteCookie(this HttpContext ctx, string cookieId)
    {
        ctx.Response.Cookies.Delete(cookieId);
    }

    public static string? GetCookie(this HttpContext ctx, string cookieId)
    {
        return ctx.Request.Cookies[cookieId];
    }
}
#endif