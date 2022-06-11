using System;
using System.Collections.Generic;
using Jamiras.Components;
using Jamiras.Services;

namespace Jamiras.Core.Services.Impl
{
    [Export(typeof(IEventBus))]
    internal class EventBus : IEventBus
    {
        private ITinyDictionary<Type, List<WeakReference>> _events = EmptyTinyDictionary<Type, List<WeakReference>>.Instance;

        public void PublishEvent<T>(T eventData)
        {
            List<WeakReference> delegates;
            if (_events.TryGetValue(typeof(T), out delegates))
            {
                List<WeakAction<T>> deadHandlers = new List<WeakAction<T>>();
                foreach (WeakAction<T> handler in delegates)
                {
                    if (!handler.Invoke(eventData))
                        deadHandlers.Add(handler);
                }

                foreach (var handler in deadHandlers)
                    delegates.Remove(handler);
            }
        }

        public void Subscribe<T>(Action<T> handler)
        {
            List<WeakReference> delegates;
            if (!_events.TryGetValue(typeof(T), out delegates))
            {
                delegates = new List<WeakReference>();
                _events = _events.AddOrUpdate(typeof(T), delegates);
            }

            delegates.Add(new WeakAction<T>(handler));
        }
    }
}
