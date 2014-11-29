using System;

namespace Jamiras.Services
{
    public interface IExceptionDispatcher
    {
        /// <summary>
        /// EventHandler to allow consumers to preview an exception before it's reported (and possibly suppress reporting it).
        /// </summary>
        event EventHandler<DispatchExceptionEventArgs> PreviewException;

        /// <summary>
        /// Attempt to handle/report an exception.
        /// </summary>
        /// <param name="ex">Exception to handle.</param>
        /// <returns><c>true</c> if the exception was handled, <c>false</c> if it should be rethrown.</returns>
        bool TryHandleException(Exception ex);

        /// <summary>
        /// Sets the handler to call when an exception is reported.
        /// </summary>
        void SetExceptionHandler(EventHandler<DispatchExceptionEventArgs> handler);
    }

    public class DispatchExceptionEventArgs : EventArgs
    {
        public DispatchExceptionEventArgs(Exception ex, bool isUnhandled)
        {
            Exception = ex;
            IsUnhandled = isUnhandled;
            ShouldReport = true;
            ShouldRethrow = true;
        }

        /// <summary>
        /// Gets the exception that was raised.
        /// </summary>
        public Exception Exception { get; private set; }

        /// <summary>
        /// Gets whether or not the exception was caught by the UnhandledException handler
        /// </summary>
        public bool IsUnhandled { get; private set; }

        /// <summary>
        /// Gets or sets whether the exception should be reported.
        /// </summary>
        public bool ShouldReport { get; set; }

        /// <summary>
        /// Gets or sets whether the application should terminate after the exception is reported.
        /// </summary>
        public bool ShouldTerminate { get; set; }

        /// <summary>
        /// Gets or sets whether the exception should be rethrown.
        /// </summary>
        public bool ShouldRethrow { get; set; }
    }
}
