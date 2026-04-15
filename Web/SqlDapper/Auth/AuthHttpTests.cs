#if SQL_DAPPER
namespace Cutulu.Web;

public class AuthHttpTests
{
    private static readonly HttpClient http = new() { BaseAddress = new Uri("http://localhost:5000") };

    [RouteHttp("/auth-test/all")]
    public static async Task<string> RunAll()
    {
        return string.Join("\n",
            [
                await TestRegister(),
                await TestDoubleRegister(),
                await TestLoginWrongPassword(),
                await TestLoginAndValidate(),
                await TestMe(),
                await TestLogout(),
                await TestLogoutAll()
            ]
        );
    }

    // 1. Register a new user
    [RouteHttp("/auth-test/register")]
    private static async Task<string> TestRegister()
    {
        var payload = System.Text.Json.JsonSerializer.Serialize(new { Username = "auth_test_user", Password = "auth_test_pass" });
        Console.WriteLine($"[TEST] Sending: {payload}");

        var res = await http.PostAsync("/auth/register", new StringContent(payload, System.Text.Encoding.UTF8, "application/json"));
        var body = await res.Content.ReadAsStringAsync();

        Console.WriteLine($"[TEST] Status: {(int)res.StatusCode}");
        Console.WriteLine($"[TEST] Body: {body}");

        return res.IsSuccessStatusCode ? "PASS register" : $"FAIL register ({(int)res.StatusCode}): {body}";
    }

    // 2. Registering the same user again should return 400
    [RouteHttp("/auth-test/double-register")]
    public static async Task<string> TestDoubleRegister()
    {
        await Post("/auth/register", "auth_test_user", "auth_test_pass"); // ensure exists
        var res = await Post("/auth/register", "auth_test_user", "auth_test_pass");
        return res.StatusCode == System.Net.HttpStatusCode.BadRequest
            ? "PASS double-register"
            : $"FAIL double-register ({(int)res.StatusCode})";
    }

    // 3. Login with wrong password should return 401
    [RouteHttp("/auth-test/login-wrong-pwd")]
    public static async Task<string> TestLoginWrongPassword()
    {
        var res = await Post("/auth/login", "auth_test_user", "wrong_password");
        return res.StatusCode == System.Net.HttpStatusCode.Unauthorized
            ? "PASS login-wrong-pwd"
            : $"FAIL login-wrong-pwd ({(int)res.StatusCode})";
    }

    // 4. Login with correct password should return a token
    [RouteHttp("/auth-test/login-validate")]
    public static async Task<string> TestLoginAndValidate()
    {
        var token = await Login("auth_test_user", "auth_test_pass");
        return token != null ? "PASS login-validate" : "FAIL login-validate (no token)";
    }

    // 5. /me with valid token should return 200
    [RouteHttp("/auth-test/me")]
    public static async Task<string> TestMe()
    {
        var token = await Login("auth_test_user", "auth_test_pass");
        if (token == null) return "FAIL me (no token)";

        var res = await GetWithToken("/me", token);
        return res.IsSuccessStatusCode ? "PASS me" : $"FAIL me ({(int)res.StatusCode})";
    }

    // 6. After logout the token should be rejected on /me
    [RouteHttp("/auth-test/logout")]
    public static async Task<string> TestLogout()
    {
        var token = await Login("auth_test_user", "auth_test_pass");
        if (token == null) return "FAIL logout (no token)";

        await PostWithToken("/auth/logout", token);

        var res = await GetWithToken("/me", token);
        return res.StatusCode == System.Net.HttpStatusCode.Unauthorized
            ? "PASS logout"
            : $"FAIL logout (token still valid, got {(int)res.StatusCode})";
    }

    // 7. /me without token should return 401
    [RouteHttp("/auth-test/me-unauth")]
    public static async Task<string> TestLogoutAll()
    {
        var res = await http.GetAsync("/me");
        return res.StatusCode == System.Net.HttpStatusCode.Unauthorized
            ? "PASS me-unauth"
            : $"FAIL me-unauth ({(int)res.StatusCode})";
    }

    // --- Helpers ---

    private static async Task<string?> Login(string username, string password)
    {
        var res = await Post("/auth/login", username, password);
        if (!res.IsSuccessStatusCode) return null;

        var json = await res.Content.ReadAsStringAsync();
        var doc = System.Text.Json.JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("token", out var t) ? t.GetString() : null;
    }

    private static Task<HttpResponseMessage> Post(string path, string username, string password)
    {
        var payload = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(new { Username = username, Password = password }),
            System.Text.Encoding.UTF8,
            "application/json"
        );
        return http.PostAsync(path, payload);
    }

    private static Task<HttpResponseMessage> PostWithToken(string path, string token)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, path);
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return http.SendAsync(req);
    }

    private static Task<HttpResponseMessage> GetWithToken(string path, string token)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, path);
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return http.SendAsync(req);
    }
}
#endif