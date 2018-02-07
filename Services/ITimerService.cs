using System;

namespace Jamiras.Services
{
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
        /// Provides a callback to be called in 300ms, unless <see cref="WaitForTyping"/> is called again before then.
        /// </summary>
        /// <param name="callback">The callback to call.</param>
        void WaitForTyping(Action callback);
    }
}
