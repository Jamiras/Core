using System;

namespace Jamiras.Services
{
    /// <summary>
    /// Service for running code asynchronously.
    /// </summary>
    public interface IBackgroundWorkerService : IAsyncDispatcher
    {
        /// <summary>
        /// Runs the provided method on the UI thread.
        /// </summary>
        void InvokeOnUiThread(Action action);
    }
}
