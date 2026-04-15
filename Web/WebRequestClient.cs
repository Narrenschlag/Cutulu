namespace Cutulu.Web;

using System.Text.Json;
using System.Text;
using Core;

public class WebRequestClient
{
    private readonly HttpClient http;
    private string? _token;

    public WebRequestClient(string path)
    {
        http = new() { BaseAddress = new Uri(path) };
    }

    // ─────────────────────────────────────────────
    // Auth
    // ─────────────────────────────────────────────

    public void SetToken(string token)
    {
        _token = token;
        http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    public void ClearToken()
    {
        _token = null;
        http.DefaultRequestHeaders.Authorization = null;
    }

    // ─────────────────────────────────────────────
    // GET
    // ─────────────────────────────────────────────

    public async Task<string?> GetAsync(string path)
    {
        var res = await http.GetAsync(path);
        return res.IsSuccessStatusCode ? await res.Content.ReadAsStringAsync() : null;
    }

    public async Task<T?> GetAsync<T>(string path)
    {
        var json = await GetAsync(path);
        return json == null ? default : JsonSerializer.Deserialize<T>(json);
    }

    public async Task<byte[]?> GetBytesAsync(string path)
    {
        var res = await http.GetAsync(path);
        if (!res.IsSuccessStatusCode) return null;

        return await res.Content.ReadAsByteArrayAsync();
    }

    public async Task<T?> GetDecodedAsync<T>(string path)
    {
        var bytes = await GetBytesAsync(path);
        if (bytes == null || bytes.Length == 0)
            return default;

        return bytes.Decode<T>();
    }

    // ─────────────────────────────────────────────
    // POST
    // ─────────────────────────────────────────────

    public async Task<string?> PostAsync(string path, object? body = null)
    {
        var res = await http.PostAsync(path, Serialize(body));
        return res.IsSuccessStatusCode ? await res.Content.ReadAsStringAsync() : null;
    }

    public async Task<T?> PostAsync<T>(string path, object? body = null)
    {
        var json = await PostAsync(path, body);
        return json == null ? default : JsonSerializer.Deserialize<T>(json);
    }

    public async Task<byte[]?> PostBytesAsync(string path, byte[] data)
    {
        var content = new ByteArrayContent(data);
        content.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

        var res = await http.PostAsync(path, content);
        if (!res.IsSuccessStatusCode) return null;

        return await res.Content.ReadAsByteArrayAsync();
    }

    public async Task<byte[]?> PostEncodedAsync<T>(string path, T obj)
    {
        var buffer = obj.Encode();
        return await PostBytesAsync(path, buffer);
    }

    public async Task<TResponse?> PostDecodedAsync<TRequest, TResponse>(string path, TRequest obj)
    {
        var requestBytes = obj.Encode();

        var responseBytes = await PostBytesAsync(path, requestBytes);
        if (responseBytes == null || responseBytes.Length == 0)
            return default;

        return responseBytes.Decode<TResponse>();
    }

    public async Task<LocalDecoder?> PostLocalDecoderAsync<T>(string path, T obj)
    {
        var bytes = obj.Encode();

        var responseBytes = await PostBytesAsync(path, bytes);
        if (responseBytes == null || responseBytes.Length == 0)
            return null;

        return new LocalDecoder(responseBytes);
    }

    // ─────────────────────────────────────────────
    // PUT
    // ─────────────────────────────────────────────

    public async Task<string?> PutAsync(string path, object? body = null)
    {
        var res = await http.PutAsync(path, Serialize(body));
        return res.IsSuccessStatusCode ? await res.Content.ReadAsStringAsync() : null;
    }

    public async Task<T?> PutAsync<T>(string path, object? body = null)
    {
        var json = await PutAsync(path, body);
        return json == null ? default : JsonSerializer.Deserialize<T>(json);
    }

    // ─────────────────────────────────────────────
    // PATCH
    // ─────────────────────────────────────────────

    public async Task<string?> PatchAsync(string path, object? body = null)
    {
        var res = await http.PatchAsync(path, Serialize(body));
        return res.IsSuccessStatusCode ? await res.Content.ReadAsStringAsync() : null;
    }

    public async Task<T?> PatchAsync<T>(string path, object? body = null)
    {
        var json = await PatchAsync(path, body);
        return json == null ? default : JsonSerializer.Deserialize<T>(json);
    }

    // ─────────────────────────────────────────────
    // DELETE
    // ─────────────────────────────────────────────

    public async Task<bool> DeleteAsync(string path)
    {
        var res = await http.DeleteAsync(path);
        return res.IsSuccessStatusCode;
    }

    // ─────────────────────────────────────────────
    // Status check
    // ─────────────────────────────────────────────

    public async Task<bool> PingAsync(string path = "/")
    {
        try
        {
            var res = await http.GetAsync(path);
            return res.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    // ─────────────────────────────────────────────
    // Helper
    // ─────────────────────────────────────────────

    private static StringContent? Serialize(object? body)
    {
        if (body == null) return null;

        return new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json"
        );
    }

    // ─────────────────────────────────────────────
    // Send LocalEncoder, receive raw bytes
    // ─────────────────────────────────────────────

    public async Task<byte[]?> PostAsync(string path, LocalEncoder encoder)
    {
        return await PostBytesAsync(path, encoder.GetBuffer());
    }

    // ─────────────────────────────────────────────
    // Send LocalEncoder, receive LocalDecoder
    // ─────────────────────────────────────────────

    public async Task<LocalDecoder?> PostDecoderAsync(string path, LocalEncoder encoder)
    {
        var bytes = await PostBytesAsync(path, encoder.GetBuffer());
        return bytes == null || bytes.Length == 0 ? null : new LocalDecoder(bytes);
    }

    // ─────────────────────────────────────────────
    // Send LocalEncoder, receive decoded T
    // ─────────────────────────────────────────────

    public async Task<T?> PostDecodedAsync<T>(string path, LocalEncoder encoder)
    {
        var bytes = await PostBytesAsync(path, encoder.GetBuffer());
        if (bytes == null || bytes.Length == 0) return default;

        return bytes.TryDecode(out T value) ? value : default;
    }

    // ─────────────────────────────────────────────
    // GET → LocalDecoder
    // ─────────────────────────────────────────────

    public async Task<LocalDecoder?> GetDecoderAsync(string path)
    {
        var bytes = await GetBytesAsync(path);
        return bytes == null || bytes.Length == 0 ? null : new LocalDecoder(bytes);
    }
}