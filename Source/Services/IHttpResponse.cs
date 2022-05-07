using System.IO;
using System.Net;

namespace Jamiras.Services
{
    /// <summary>
    /// Represents the response of an HTTP request
    /// </summary>
    public interface IHttpResponse
    {
        /// <summary>
        /// Gets the response status.
        /// </summary>
        HttpStatusCode Status { get; }

        /// <summary>
        /// Gets the response stream. You are responsible for closing the stream when you're done.
        /// </summary>
        Stream GetResponseStream();

        /// <summary>
        /// Gets the header data associated to the specified tag.
        /// </summary>
        /// <param name="name">Name of tag to get value of.</param>
        /// <returns>Value of tag, null if tag not found.</returns>
        string GetHeader(string name);

        /// <summary>
        /// Releases the connection to the resource for reuse by other requests. 
        /// </summary>
        /// <remarks>
        /// If you use the response stream, closing it will release the connection. You only need to call 
        /// this if you don't access the response stream (for example if the status was unexpected).
        /// </remarks>
        void Close();
    }
}
