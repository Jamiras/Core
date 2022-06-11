using System;

namespace Jamiras.Services
{
    /// <summary>
    /// Service for running code asynchronously.
    /// </summary>
    public interface IAsyncDispatcher
    {
        /// <summary>
        /// Runs the provided method on a worker thread.
        /// </summary>
        void RunAsync(Action action);
    }
}
