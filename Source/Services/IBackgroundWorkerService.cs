using System;

namespace Jamiras.Services
{
    /// <summary>
    /// Service for running code asynchronously.
    /// </summary>
    public interface IBackgroundWorkerService
    {
        /// <summary>
        /// Runs the provided method on a worker thread.
        /// </summary>
        void RunAsync(Action action);

        /// <summary>
        /// Runs the provided method on the UI thread.
        /// </summary>
        void InvokeOnUiThread(Action action);
    }
}
