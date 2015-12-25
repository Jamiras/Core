using System;
using System.Net;

namespace Jamiras.Services
{
    public interface IHttpListener
    {
        /// <summary>
        /// Registers a http request handler.
        /// </summary>
        /// <param name="urlPrefix">The prefix of the URL to handle (with optional port) "http://localhost:8080/foo"</param>
        /// <param name="handler">The delegate that should handle the request</param>
        void RegisterHandler(string urlPrefix, Func<IHttpRequest, HttpListenerResponse> handler);

        /// <summary>
        /// Starts listening for requests.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops listening for requests.
        /// </summary>
        void Stop();
    }

    public class HttpListenerResponse
    {
        public HttpListenerResponse()
        {
            Status = HttpStatusCode.OK;
            ContentType = "text/html";
        }

        public HttpListenerResponse(string response)
            : this()
        {
            Response = response;
        }

        public HttpStatusCode Status { get; set; }
        public string ContentType { get; set; }
        public string Response { get; set; }
    }
}
