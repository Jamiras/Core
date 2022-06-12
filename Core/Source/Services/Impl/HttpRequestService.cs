using Jamiras.Components;
using Jamiras.IO.Serialization;
using Jamiras.Services;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Jamiras.Core.Services.Impl
{
    [Export(typeof(IHttpRequestService))]
    internal class HttpRequestService : IHttpRequestService
    {
        [ImportingConstructor]
        public HttpRequestService(ILogService logService)
        {
            _logger = logService.GetLogger("Jamiras.Core");

            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; // Tls12 (.NET45 constant)
        }

        private readonly ILogger _logger;

        /// <summary>
        /// Requests the document at a URL.
        /// </summary>
        /// <param name="url">URL of document to request.</param>
        /// <returns><see cref="IHttpResponse"/> wrapping the response.</returns>
        public IHttpResponse Request(string url)
        {
            return Request(new HttpRequest(url));
        }

        /// <summary>
        /// Create an <see cref="IHttpRequest"/> object to pass to the <see cref="Request(IHttpRequest)"/> method.
        /// </summary>
        /// <param name="url">URL of document to request.</param>
        /// <returns><see cref="IHttpRequest"/> to configure.</returns>
        public IHttpRequest CreateRequest(string url)
        {
            return new HttpRequest(url);
        }

        /// <summary>
        /// Requests the document at a URL.
        /// </summary>
        /// <param name="request">Information about the request.</param>
        /// <returns><see cref="IHttpResponse"/> wrapping the response.</returns>
        public IHttpResponse Request(IHttpRequest request)
        {
            var client = CreateClient(request);
            var message = CreateRequestMessage(request);
            _logger.Write("Requesting " + request.Url);

            bool retry = false;

            HttpResponseMessage response = null;
            try
            {
                response = client.Send(message);
            }
            catch (TaskCanceledException taskEx)
            {
                _logger.WriteError(taskEx.Message + ": " + message.RequestUri);

                // timeout; immediately try again (once)
                retry = true;
            }
            catch (Exception ex)
            {
                _logger.WriteError(ex.Message + ": " + message.RequestUri);

                var socketException = ex.InnerException as System.Net.Sockets.SocketException;
                if (socketException != null &&
                    (socketException.SocketErrorCode == System.Net.Sockets.SocketError.TimedOut ||
                     socketException.SocketErrorCode == System.Net.Sockets.SocketError.HostNotFound))
                {
                    // timeout; immediately try again (once)
                    retry = true;
                }
                else
                {
                    if (!TryHandleException(ex))
                        throw;

                    return null;
                }
            }

            if (retry)
            {
                message = CreateRequestMessage(request);

                try
                {
                    response = client.Send(message);
                }
                catch (Exception ex)
                {
                    _logger.WriteError(ex.Message + ": " + message.RequestUri);

                    if (!TryHandleException(ex))
                        throw;

                    return null;
                }
            }

            return new HttpResponse(response);
        }

        private static HttpClient CreateClient(IHttpRequest request)
        {
            var client = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip
            });
            client.Timeout = request.Timeout;

            return client;
        }

        private static HttpRequestMessage CreateRequestMessage(IHttpRequest request)
        {
            HttpRequestMessage message;

            if (!string.IsNullOrEmpty(request.PostData))
                message = new HttpRequestMessage(HttpMethod.Post, request.Url) { Content = new StringContent(request.PostData, Encoding.UTF8) };
            else
                message = new HttpRequestMessage(HttpMethod.Get, request.Url);

            message.Headers.Add("ContentType", "application/x-www-form-urlencoded");

            foreach (var header in request.Headers.AllKeys)
                message.Headers.Add(header, request.Headers[header]);

            return message;
        }

        private static bool TryHandleException(Exception ex)
        {
            try
            {
                // lazy load exception dispatcher, it can't be constructed at the time HttpRequestService is created.
                var exceptionDispatcher = ServiceRepository.Instance.FindService<IExceptionDispatcher>();
                if (exceptionDispatcher.TryHandleException(ex))
                    return true;
            }
            catch
            {
                // ignore exception attempting to report exception
            }

            return false;
        }

        /// <summary>
        /// Removes HTML tags from a string and converts escaped characters.
        /// </summary>
        public string HtmlToText(string html)
        {
            if (html.IndexOfAny(new char[] { '<', '&' }) == -1)
                return html;

            var builder = new StringBuilder();
            var parser = new XmlParser(html);

            while (parser.NextTokenType != XmlTokenType.None)
            {
                if (parser.NextTokenType == XmlTokenType.Content)
                    builder.Append(System.Web.HttpUtility.HtmlDecode(parser.NextToken.ToString()));

                parser.Advance();
            }

            return builder.ToString();
        }
    }
}
