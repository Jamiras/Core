using Jamiras.Services;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace Jamiras.Core.Services.Impl
{
    [DebuggerDisplay("{Status}")]
    internal class HttpResponse : IHttpResponse
    {
        public HttpResponse(HttpResponseMessage response)
        {
            _response = response;
        }

        private readonly HttpResponseMessage _response;

        /// <summary>
        /// Gets the response status.
        /// </summary>
        public HttpStatusCode Status
        {
            get { return _response.StatusCode; }
        }

        /// <summary>
        /// Gets the response stream.
        /// </summary>
        public Stream GetResponseStream()
        {
            return _response.Content.ReadAsStream();
        }

        /// <summary>
        /// Gets the header data associated to the specified tag.
        /// </summary>
        /// <param name="name">Name of tag to get value of.</param>
        /// <returns>Value of tag, null if tag not found.</returns>
        public string GetHeader(string name)
        {
            return _response.Headers.GetValues(name).First();
        }

        /// <summary>
        /// Releases the connection to the resource for reuse by other requests. 
        /// </summary>
        /// <remarks>
        /// If you use the response stream, closing it will release the connection. You only need to call 
        /// this if you don't access the response stream (for example if the status was unexpected).
        /// </remarks>
        public void Close()
        {
            _response.Dispose();
        }
    }
}
