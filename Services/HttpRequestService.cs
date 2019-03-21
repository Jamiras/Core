using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Jamiras.Components;
using Jamiras.IO.Serialization;

namespace Jamiras.Services
{
    [Export(typeof(IHttpRequestService))]
    internal class HttpRequestService : IHttpRequestService
    {
        [ImportingConstructor]
        public HttpRequestService(ILogService logService)
        {
            _logger = logService.GetLogger("Jamiras.Core");
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
        /// Requests the document at a URL.
        /// </summary>
        /// <param name="request">Information about the request.</param>
        /// <returns><see cref="IHttpResponse"/> wrapping the response.</returns>
        public IHttpResponse Request(IHttpRequest request)
        {
            var webRequest = CreateWebRequest(request);
            _logger.Write("Requesting " + request.Url);
            HttpWebResponse webResponse;
            try
            {
                var response = webRequest.GetResponse();
                webResponse = (HttpWebResponse)response;
            }
            catch (WebException webEx)
            {
                _logger.WriteError(webEx.Message + ": " + webRequest.RequestUri);

                webResponse = webEx.Response as HttpWebResponse;
                if (webResponse == null)
                    throw;
            }

            return new HttpResponse(webResponse);
        }

        private HttpWebRequest CreateWebRequest(IHttpRequest request)
        {
            var webRequest = (HttpWebRequest)WebRequest.Create(request.Url);
            webRequest.Timeout = webRequest.ReadWriteTimeout = (int)(request.Timeout.Ticks/TimeSpan.TicksPerMillisecond);

            if (webRequest.Proxy == null)
                webRequest.Proxy = WebRequest.GetSystemWebProxy();
            //if (webRequest.Proxy != null)
            //{
            //    var proxyUri = webRequest.Proxy.GetProxy(webRequest.RequestUri);
            //    webRequest.Proxy.Credentials = _httpConnectionService.GetCredentials(proxyUri);
            //}

            //Passing empty cookies containter. If we pass null cookie container we will not get cookies back.
            webRequest.CookieContainer = new CookieContainer();

            if (request.Headers.Count > 0)
            {
                foreach (var header in request.Headers.AllKeys)
                {
                    switch (header)
                    {
                        case "Referer":
                            webRequest.Referer = request.Headers[header];
                            break;
                        case "Accept":
                            webRequest.Accept = request.Headers[header];
                            break;
                        case "User-Agent":
                            webRequest.UserAgent = request.Headers[header];
                            break;
                        default:
                            webRequest.Headers.Add(header, request.Headers[header]);
                            break;
                    }
                }
            }

            webRequest.ContentType = "application/x-www-form-urlencoded";

            var postData = GetPostData(request);
            if (postData != null)
            {
                webRequest.Method = "POST";
                webRequest.ContentLength = postData.Length;

                using (var dataStream = webRequest.GetRequestStream())
                {
                    dataStream.Write(postData, 0, postData.Length);
                }   
            }

            return webRequest;
        }

        internal byte[] GetPostData(IHttpRequest request)
        {
            if (!String.IsNullOrEmpty(request.PostData))
                return Encoding.UTF8.GetBytes(request.PostData);

            return null;
        }

        /// <summary>
        /// Creates a string formatted with the name value pairs that can be used
        /// as post data or a query string
        /// </summary>
        /// <param name="data">dictionary of name value pairs to use in the query string</param>
        /// <returns>string formatted</returns>
        private string CreateQueryString(Dictionary<string, string> data)
        {
            var builder = new StringBuilder();

            foreach (var dataKey in data.Keys)
            {
                if (builder.Length > 0)
                    builder.Append("&");

                builder.Append(dataKey);
                builder.Append("=");
                builder.Append(data[dataKey]);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Requests the document at a URL asynchronously.
        /// </summary>
        /// <param name="request">Information about the request.</param>
        /// <param name="callback">Method to call when the response is received.</param>
        public void BeginRequest(IHttpRequest request, Action<IHttpResponse> callback)
        {
            var webRequest = CreateWebRequest(request);
            var callbackData = new KeyValuePair<HttpWebRequest, Action<IHttpResponse>>(webRequest, callback);
            webRequest.BeginGetResponse(AsyncRequestCallback, callbackData);
        }

        private void AsyncRequestCallback(IAsyncResult asyncResult)
        {
            var callbackData = (KeyValuePair<HttpWebRequest, Action<IHttpResponse>>)asyncResult.AsyncState;
            var webRequest = callbackData.Key;
            var callback = callbackData.Value;

            HttpWebResponse webResponse;
            try
            {
                webResponse = (HttpWebResponse)webRequest.EndGetResponse(asyncResult);
            }
            catch (WebException webEx)
            {
                _logger.WriteError(webEx.Message + ": " + webRequest.RequestUri);

                webResponse = webEx.Response as HttpWebResponse;
                if (webResponse == null)
                {
                    if (TryHandleException(webEx))
                        return;

                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.WriteError(ex.Message + ": " + webRequest.RequestUri);

                if (TryHandleException(ex))
                    return;
                throw;
            }

            if (callback != null)
                callback(new HttpResponse(webResponse));
            else
                webResponse.Close();
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
            if (html.IndexOfAny(new char[] {'<', '&'}) == -1)
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
