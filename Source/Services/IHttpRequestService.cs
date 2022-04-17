using System;

namespace Jamiras.Services
{
    /// <summary>
    /// Defines the service for requesting HTTP documents.
    /// </summary>
    public interface IHttpRequestService
    {
        /// <summary>
        /// Requests the document at a URL.
        /// </summary>
        /// <param name="url">URL of document to request.</param>
        /// <returns><see cref="IHttpResponse"/> wrapping the response.</returns>
        /// <remarks>
        /// Make sure to close the stream associated to the <see cref="IHttpResponse"/> 
        /// (or the <see cref="IHttpResponse"/> if you don't use the stream) when you're done with them.
        /// </remarks>
        IHttpResponse Request(string url);

        /// <summary>
        /// Requests the document at a URL.
        /// </summary>
        /// <param name="request">Information about the request.</param>
        /// <returns><see cref="IHttpResponse"/> wrapping the response.</returns>
        /// <remarks>
        /// Make sure to close the stream associated to the <see cref="IHttpResponse"/> 
        /// (or the <see cref="IHttpResponse"/> if you don't use the stream) when you're done with them.
        /// </remarks>
        IHttpResponse Request(IHttpRequest request);

        /// <summary>
        /// Removes HTML tags from a string and converts escaped characters.
        /// </summary>
        string HtmlToText(string html);
    }
}
