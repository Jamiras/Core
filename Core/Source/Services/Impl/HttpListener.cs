using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Jamiras.Components;
using Jamiras.Services;

namespace Jamiras.Core.Services.Impl
{
    [Export(typeof(IHttpListener))]
    internal class HttpListener : IHttpListener, IDisposable
    {
        [ImportingConstructor]
        public HttpListener(IAsyncDispatcher asyncDispatcher)
        {
            _asyncDispatcher = asyncDispatcher;
            _listener = new System.Net.HttpListener();
            _handlers = new List<HttpHandler>();
        }

        private readonly IAsyncDispatcher _asyncDispatcher;
        private readonly System.Net.HttpListener _listener;
        private readonly List<HttpHandler> _handlers;

        public void Dispose()
        {
            _listener.Stop();
            _listener.Close();
            _handlers.Clear();
        }

        private class HttpHandler
        {
            public string UrlPrefix { get; set; }
            public Func<IHttpRequest, HttpListenerResponse> Handler { get; set; }
        }

        public void RegisterHandler(string urlPrefix, Func<IHttpRequest, HttpListenerResponse> handler)
        {
            lock (_handlers)
            {
                var httpHandler = _handlers.FirstOrDefault(h => h.UrlPrefix == urlPrefix);
                if (httpHandler == null)
                {
                    httpHandler = new HttpHandler();
                    httpHandler.UrlPrefix = urlPrefix;
                    _handlers.Add(httpHandler);

                    _listener.Prefixes.Add(urlPrefix);
                }

                httpHandler.Handler = handler;
            }
        }

        public void Start()
        {
            if (_listener.IsListening)
                return;

            _listener.Start();
            _asyncDispatcher.RunAsync(Listen);
        }

        public void Stop()
        {
            _listener.Stop();
        }

        private void Listen()
        {
            System.Threading.Thread.CurrentThread.Name = "HttpListener";
            while (_listener.IsListening)
            {
                var result = _listener.BeginGetContext(HandleRequest, null);
                result.AsyncWaitHandle.WaitOne();
            }
        }

        private void HandleRequest(IAsyncResult result)
        {
            if (!_listener.IsListening)
                return;

            var context = _listener.EndGetContext(result);
            var url = context.Request.Url.ToString();
            var handler = _handlers.FirstOrDefault(h => url.StartsWith(h.UrlPrefix, StringComparison.OrdinalIgnoreCase));
            if (handler != null)
            {
                HttpRequest request = new HttpRequest(context.Request.Url.ToString());
                foreach (var header in context.Request.Headers.AllKeys)
                    request.Headers.Add(header, context.Request.Headers[header]);

                using (var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8))
                    request.PostData = reader.ReadToEnd();

                var response = handler.Handler(request);
                context.Response.StatusCode = (int)response.Status;
                context.Response.ContentType = response.ContentType;
                context.Response.ContentEncoding = Encoding.UTF8;

                if (!string.IsNullOrEmpty(response.Response))
                {
                    using (var writer = new StreamWriter(context.Response.OutputStream, Encoding.UTF8))
                        writer.Write(response.Response);
                }
            }
            else
            {
                context.Response.StatusCode = (int)System.Net.HttpStatusCode.NotFound;
            }
        }
    }
}
