using System;
using System.ComponentModel;
using System.Collections.Generic;

namespace Jamiras.Components
{
    /// <summary>
    /// Helper class for subscribing to a PropertyChanged event in a way that doesn't cause
    /// the event listener to be held in memory after it should have gone out of scope.
    /// Also provides a way to only capture PropertyChanged events for specific properties.
    /// </summary>
    public class PropertyObserver<TSource>
        where TSource : class, INotifyPropertyChanged
    {
        /// <summary>
        /// Constructs a new PropertyObserver.
        /// </summary>
        /// <param name="source">Object to observe, may be <c>null</c>.</param>
        public PropertyObserver(TSource source)
        {
            _handlers = EmptyTinyDictionary<string, WeakAction<object, PropertyChangedEventArgs>>.Instance;
            Source = source;
        }

        private ITinyDictionary<string, WeakAction<object, PropertyChangedEventArgs>> _handlers;
        private TSource _source;

        /// <summary>
        /// Gets or sets the object being observed.
        /// </summary>
        public TSource Source
        {
            get { return _source; }
            set 
            {
                if (!ReferenceEquals(_source, value))
                {
                    if (_source != null)
                        _source.PropertyChanged -= SourcePropertyChanged;

                    _source = value;

                    if (_source != null)
                        _source.PropertyChanged += SourcePropertyChanged;
                }
            }
        }

        /// <summary>
        /// Registers a callback to be invoked when the PropertyChanged event has been raised for the specified property.
        /// You should use the RegisterHandler method that accepts a lambda expression for compile-time reference validation.
        /// </summary>
        /// <param name="propertyName">The name of the property to watch.</param>
        /// <param name="handler">The callback to invoke when the property has changed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> or <paramref name="handler"/> is null.</exception>
        /// <exception cref="InvalidOperationException">A handler is already registered for <paramref name="propertyName"/>.</exception>
        public void RegisterHandler(string propertyName, PropertyChangedEventHandler handler)
        {
            if (String.IsNullOrEmpty(propertyName))
                throw new ArgumentNullException("propertyName");
            if (handler == null)
                throw new ArgumentNullException("handler");

            _handlers = _handlers.Add(propertyName, new WeakAction<object, PropertyChangedEventArgs>(handler.Method, handler.Target));
        }

        private void SourcePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            WeakAction<object, PropertyChangedEventArgs> weakAction;
            if (_handlers.TryGetValue(e.PropertyName, out weakAction))
            {
                if (!weakAction.Invoke(Source, e))
                    AuditHandlers();
            }
        }

        private void AuditHandlers()
        {
            var deadHandlers = new List<string>();

            foreach (var kvp in _handlers)
            {
                var weakAction = kvp.Value;
                if (weakAction != null && !weakAction.IsAlive)
                    deadHandlers.Add(kvp.Key);
            }

            foreach (var key in deadHandlers)
                _handlers = _handlers.Remove(key);
        }
    }
}
