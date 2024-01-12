using System.Text;
using Godot;

namespace Cutulu
{
    public class WebRequest
    {
        public delegate void Result(bool success, Godot.Collections.Dictionary result);
        private readonly HttpRequest request;
        private readonly Result onReceive;

        public WebRequest(string url, Result result = null) : this(result)
        {
            request.Request(url, null, HttpClient.Method.Get);
        }

        public WebRequest(string url, string[] headers, Result result) : this(url, "", headers, result) { }
        public WebRequest(string url, string json, Result result) : this(url, json, null, result) { }

        public WebRequest(string url, string json, string[] headers, Result result = null) : this(result)
        {
            if (url.IsEmpty())
            {
                url = "127.0.0.1";
            }

            request.Request(url, headers, HttpClient.Method.Post, json);
        }

        private WebRequest(Result result)
        {
            request = new HttpRequest();
            onReceive = result;

            request.RequestCompleted += OnRequestCompleted;
        }

        private void OnRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
        {
            request.Destroy();

            onReceive?.Invoke(responseCode == 0, Json.ParseString(Encoding.UTF8.GetString(body)).AsGodotDictionary());
        }
    }
}