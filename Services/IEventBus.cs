using System;

namespace Jamiras.Services
{
    public interface IEventBus
    {
        void PublishEvent<T>(T eventData);

        void Subscribe<T>(Action<T> handler);
    }
}
