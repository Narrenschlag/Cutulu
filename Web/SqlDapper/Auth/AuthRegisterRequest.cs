namespace Cutulu.Web;

using System.Text.Json.Serialization;

public struct AuthRegisterRequest(string username, string password)
{
    [JsonPropertyName("Username")] public string Username { get; set; } = username;
    [JsonPropertyName("Password")] public string Password { get; set; } = password;
}