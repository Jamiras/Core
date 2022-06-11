using System;

namespace Jamiras.Services
{
    /// <summary>
    /// Service for measuring and interacting with time.
    /// </summary>
    public interface ITimerService
    {
        /// <summary>
        /// Gets the current time (in UTC).
        /// </summary>
        DateTime UtcNow { get; }

        /// <summary>
        /// Schedules the specified callback to be called.
        /// </summary>
        /// <param name="callback">The callback to call.</param>
        /// <param name="delay">How long to wait before calling it.</param>
        void Schedule(Action callback, TimeSpan delay);

        /// <summary>
        /// Unschedules the specified callback.
        /// </summary>
        /// <param name="callback">The callback that should no longer be called.</param>
        void Unschedule(Action callback);

        /// <summary>
        /// Schedules the specified callback to be called. If the callback is already scheduled, the existing delay will be replaced.
        /// </summary>
        /// <param name="callback">The callback to call.</param>
        /// <param name="delay">How long to wait before calling it.</param>
        void Reschedule(Action callback, TimeSpan delay);
    }
}
