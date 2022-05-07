using System;
using System.Net;

namespace Jamiras.Services
{
    /// <summary>
    /// Defines a service for listening for and responding to HTTP requests.
    /// </summary>
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

    /// <summary>
    /// Defines the response to send for a given request dispatched by the <see cref="IHttpListener"/>.
    /// </summary>
    public class HttpListenerResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpListenerResponse"/> class.
        /// </summary>
        public HttpListenerResponse()
        {
            Status = HttpStatusCode.OK;
            ContentType = "text/html";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpListenerResponse"/> class.
        /// </summary>
        /// <param name="response">The response.</param>
        public HttpListenerResponse(string response)
            : this()
        {
            Response = response;
        }

        /// <summary>
        /// Gets or sets the status of the response.
        /// </summary>
        public HttpStatusCode Status { get; set; }

        /// <summary>
        /// Gets or sets the type of the content.
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// Gets or sets the response.
        /// </summary>
        public string Response { get; set; }
    }
}
