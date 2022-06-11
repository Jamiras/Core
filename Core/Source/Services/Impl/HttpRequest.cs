﻿using Jamiras.Services;
using System;
using System.Diagnostics;
using System.Net;

namespace Jamiras.Core.Services.Impl
{
    /// <summary>
    /// Default implementation of the <see cref="IHttpRequest"/> interface.
    /// </summary>
    [DebuggerDisplay("{Url}")]
    internal class HttpRequest : IHttpRequest
    {
        /// <summary>
        /// Constructs a new <see cref="HttpRequest"/> object.
        /// </summary>
        /// <param name="url">Initial value for the <see cref="Url"/> property.</param>
        public HttpRequest(string url)
        {
            Url = url;
            Headers = new WebHeaderCollection();
            Timeout = TimeSpan.FromSeconds(100);
        }

        /// <summary>
        /// Gets or sets the URL of the request.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Gets the collection of additional headers to send when making the request.
        /// </summary>
        public WebHeaderCollection Headers { get; private set; }

        /// <summary>
        /// Gets or sets the data to POST to the Url.
        /// </summary>
        /// <remarks>
        /// If not set, a GET request will be made.
        /// </remarks>
        public string PostData { get; set; }

        /// <summary>
        /// Gets or set the time-out period for the request.
        /// </summary>
        public TimeSpan Timeout { get; set; }
    }
}
