using System.Diagnostics;
using System.IO;
using System.Net;

namespace Jamiras.Services
{
    [DebuggerDisplay("{Status}")]
    internal class HttpResponse : IHttpResponse
    {
        public HttpResponse(HttpWebResponse response)
        {
            _response = response;
        }

        private readonly HttpWebResponse _response;

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
            var stream = _response.GetResponseStream();

            if (_response.Headers[HttpResponseHeader.ContentEncoding] == "gzip")
                return UnwrapGZipStream(stream);

            return stream;
        }

        private static Stream UnwrapGZipStream(Stream stream)
        {
            // put this in a separate function so the JIT compiler doesn't evaluate GZipStream unless it's used
            return new System.IO.Compression.GZipStream(stream, System.IO.Compression.CompressionMode.Decompress);
        }

        /// <summary>
        /// Gets the header data associated to the specified tag.
        /// </summary>
        /// <param name="name">Name of tag to get value of.</param>
        /// <returns>Value of tag, null if tag not found.</returns>
        public string GetHeader(string name)
        {
            return _response.Headers.Get(name);
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
            _response.Close();
        }
    }
}
