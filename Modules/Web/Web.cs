using System.Net;
using System.Text;
using Godot;

namespace Cutulu
{
    public class WebRequest
    {
        public delegate void Result(bool success, string result);
        private readonly HttpRequest request;
        private readonly Result onReceive;

        public WebRequest(Node node, string url, Result result = null) : this(node, result)
        {
            request.Request(url, null, HttpClient.Method.Get);
        }

        public WebRequest(Node node, string url, string[] headers, Result result) : this(node, url, "", headers, result) { }
        public WebRequest(Node node, string url, string json, Result result) : this(node, url, json, null, result) { }

        public WebRequest(Node node, string url, string json, string[] headers, Result result = null) : this(node, result)
        {
            if (url.IsEmpty())
            {
                url = "127.0.0.1";
            }

            request.Request(url, headers, HttpClient.Method.Post, json);
        }

        private WebRequest(Node node, Result result)
        {
            request = new HttpRequest();
            node.AddChild(request);
            onReceive = result;

            request.RequestCompleted += OnRequestCompleted;
        }

        private void OnRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
        {
            request.Destroy();

            onReceive?.Invoke(responseCode == (byte)HttpClient.ResponseCode.Ok, Encoding.UTF8.GetString(body).Trim()); // Json.ParseString(Encoding.UTF8.GetString(body)).AsGodotDictionary());
        }
    }
}