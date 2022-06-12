using System;
using Jamiras.Components;
using Jamiras.Services;

namespace Jamiras.Core.Services.Impl
{
    [Export(typeof(IExceptionDispatcher))]
    internal class ExceptionDispatcher : IExceptionDispatcher
    {
        private readonly ILogger _logger = Logger.GetLogger("ExceptionDispatcher");
        private EventHandler<DispatchExceptionEventArgs> _reportHandler;
        private bool _isTerminating;

        public ExceptionDispatcher()
        {
            AppDomain.CurrentDomain.UnhandledException += DispatchUnhandledException;
        }

        private void DispatchUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.IsTerminating)
                _isTerminating = true;

            var args = new DispatchExceptionEventArgs((Exception)e.ExceptionObject, true);
            HandleException(args);
        }

        /// <summary>
        /// Sets the handler to call when an exception is reported.
        /// </summary>
        public void SetExceptionHandler(EventHandler<DispatchExceptionEventArgs> handler)
        {
            _reportHandler = handler;
        }

        /// <summary>
        /// EventHandler to allow consumers to preview an exception before it's reported (and possibly suppress reporting it).
        /// </summary>
        public event EventHandler<DispatchExceptionEventArgs> PreviewException;

        /// <summary>
        /// Attempt to handle/report an exception.
        /// </summary>
        /// <param name="ex">Exception to handle.</param>
        /// <returns><c>true</c> if the exception was handled, <c>false</c> if it should be rethrown.</returns>
        public bool TryHandleException(Exception ex)
        {
            var args = new DispatchExceptionEventArgs(ex, false);
            HandleException(args);
            return args.ShouldRethrow;
        }

        private void HandleException(DispatchExceptionEventArgs e)
        {
            if (PreviewException != null)
                PreviewException(this, e);

            if (e.ShouldReport)
            {
                _logger.WriteError(e.IsUnhandled ? "Unhandled exception: {0}\r\n{1}" : "Exception: {0}\r\n{1}", e.Exception.Message, e.Exception.StackTrace);

                if (_reportHandler != null)
                    _reportHandler(this, e);
            }

            if (e.ShouldTerminate && !_isTerminating)
            {
                _logger.WriteWarning("Terminating application");

                _isTerminating = true;
                Environment.Exit(-1);
            }
        }
    }
}
