using System.Text;
using Godot;

namespace Cutulu
{
    /// <summary>
    /// HTTP request based web request system for fast and easy usage
    /// </summary>
    public class WebRequest
    {
        public delegate void Result(bool success, string result);
        private readonly HttpRequest request;
        private readonly Result onReceive;

        /// <summary>
        /// Request data from an url.
        /// </summary>
        public WebRequest(Node node, string url, Result result = null) : this(ref node, result)
        {
            request.Request(url, null, HttpClient.Method.Get, "");
        }

        /// <summary>
        /// Request data from an url by headers.
        /// </summary>
        public WebRequest(Node node, string url, string[] headers, Result result) : this(node, url, "", headers, result) { }

        /// <summary>
        /// Request data from an url by json.
        /// </summary>
        public WebRequest(Node node, string url, string json, Result result) : this(node, url, json, null, result) { }

        /// <summary>
        /// Request data from an url by headers and json.
        /// </summary>
        public WebRequest(Node node, string url, string json, string[] headers, Result result = null) : this(ref node, result)
        {
            request.Request(url, headers, HttpClient.Method.Post, json);
        }

        /// <summary>
        /// Base constructor for web requests
        /// </summary>
        private WebRequest(ref Node node, Result result)
        {
            request = new HttpRequest();
            node.AddChild(request);
            onReceive = result;

            request.RequestCompleted += OnRequestCompleted;
        }

        /// <summary>
        /// Invokes the callback function for completed requests and ends its life cycle
        /// </summary>
        private void OnRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
        {
            request.Destroy();

            onReceive?.Invoke(responseCode == (byte)HttpClient.ResponseCode.Ok, Encoding.UTF8.GetString(body).Trim()); // Json.ParseString(Encoding.UTF8.GetString(body)).AsGodotDictionary());
        }
    }
}