using System;
using System.Collections.Generic;
using System.Net;

namespace Jamiras.Services
{
    /// <summary>
    /// Represents the data to required to make an HTTP request.
    /// </summary>
    public interface IHttpRequest
    {
        /// <summary>
        /// Gets or sets the URL of the request.
        /// </summary>
        string Url { get; set; }

        /// <summary>
        /// Gets the collection of additional headers to send when making the request.
        /// </summary>
        WebHeaderCollection Headers { get; }

        /// <summary>
        /// Gets or sets the data to POST to the Url.
        /// </summary>
        /// <remarks>
        /// If not set, a GET request will be made.
        /// </remarks>
        string PostData { get; set; }

        /// <summary>
        /// Gets or set the time-out period for the request.
        /// </summary>
        TimeSpan Timeout { get; set; }
    }
}
