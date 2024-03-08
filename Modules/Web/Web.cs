using System.Text;
using Godot;

namespace Cutulu
{
    /// <summary>
    /// HTTP request based web request system for fast and easy usage
    /// </summary>
    public class WebRequest
    {
        public delegate void Result(bool success, string result, params object[] given);
        private readonly HttpRequest Request;
        private readonly Result Receive;
        private object[] Given;

        /// <summary>
        /// Request data from an url.
        /// </summary>
        public WebRequest(Node node, string url, Result result = null, params object[] given) : this(ref node, result, given)
        {
            Request.Request(url, null, HttpClient.Method.Get, "");
        }

        /// <summary>
        /// Request data from an url by headers.
        /// </summary>
        public WebRequest(Node node, string url, string[] headers, Result result, params object[] given) : this(node, url, "", headers, result, given) { }

        /// <summary>
        /// Request data from an url by json.
        /// </summary>
        public WebRequest(Node node, string url, string json, Result result, params object[] given) : this(node, url, json, null, result, given) { }

        /// <summary>
        /// Request data from an url by headers and json.
        /// </summary>
        public WebRequest(Node node, string url, string json, string[] headers, Result result = null, params object[] given) : this(ref node, result, given)
        {
            Request.Request(url, headers, HttpClient.Method.Post, json);
        }

        /// <summary>
        /// Base constructor for web requests
        /// </summary>
        private WebRequest(ref Node Node, Result result, params object[] given)
        {
            Request = new HttpRequest();
            Node.AddChild(Request);
            Receive = result;
            Given = given;

            Request.RequestCompleted += OnRequestCompleted;
        }

        /// <summary>
        /// Invokes the callback function for completed requests and ends its life cycle
        /// </summary>
        private void OnRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
        {
            Request.Destroy();

            Receive?.Invoke(responseCode == (byte)HttpClient.ResponseCode.Ok, Encoding.UTF8.GetString(body).Trim(), Given); // Json.ParseString(Encoding.UTF8.GetString(body)).AsGodotDictionary());
        }
    }
}