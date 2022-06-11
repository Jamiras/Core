using System;

namespace Jamiras.Services
{
    /// <summary>
    /// Service for subscribing to and publishing global events.
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// Passes the <paramref name="eventData"/> to any subscribers watching for <typeparamref name="T"/> events.
        /// </summary>
        void PublishEvent<T>(T eventData);

        /// <summary>
        /// Provides a method to be called any time a <typeparamref name="T"/> event is published.
        /// </summary>
        /// <remarks>
        /// <paramref name="handler"/> is held with a <see cref="WeakReference"/>, so it is not necessary to unsubscribe.
        /// </remarks>
        void Subscribe<T>(Action<T> handler);
    }
}
