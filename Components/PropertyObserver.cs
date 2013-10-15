using System;
using System.ComponentModel;

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
            _watcher = new NoPropertyWatcher(source, DispatchPropertyChanged);
        }

        private IPropertyWatcher _watcher;

        /// <summary>
        /// Gets or sets the object being observed.
        /// </summary>
        public INotifyPropertyChanged Source
        {
            get { return _watcher.Source; }
            set { _watcher.Source = value; }
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

            _watcher = _watcher.AddHandler(propertyName, new WeakAction<object, PropertyChangedEventArgs>(handler.Method, handler.Target));
        }

        private void DispatchPropertyChanged(string propertyName, object callbackData)
        {
            var weakAction = (WeakAction<object, PropertyChangedEventArgs>)callbackData;
            if (!weakAction.Invoke(_watcher.Source, new PropertyChangedEventArgs(propertyName)))
                AuditHandlers();
        }

        private void AuditHandlers()
        {
            var properties = _watcher.WatchedProperties;
            foreach (string property in properties)
            {
                var weakAction = _watcher.GetCallbackData(property) as WeakAction<object, PropertyChangedEventArgs>;
                if (weakAction != null && !weakAction.IsAlive)
                    _watcher = _watcher.RemoveHandler(property);
            }
        }
    }
}
