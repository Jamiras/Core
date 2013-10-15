using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Jamiras.Components
{
    /// <summary>
    /// Helper class that propagates PropertyChanged events from one object to another, allowing for mapping of the property name.
    /// </summary>
    /// <remarks>This class uses weak eventing, so you need to hold a reference to it as long as you require events to be propagated.</remarks>
    [DebuggerDisplay("{typeof(TSource).Name,nq} PropertyChangedPropagator")]
    public class PropertyChangedPropagator<TSource>
        where TSource : class, INotifyPropertyChanged
    {
        public PropertyChangedPropagator(TSource source, Action<PropertyChangedEventArgs> handler)
        {
            _handler = handler;
            _watcher = new NoPropertyWatcher(source, DispatchPropertyChanged);
        }

        private IPropertyWatcher _watcher;
        private readonly Action<PropertyChangedEventArgs> _handler;

        /// <summary>
        /// Gets or sets the object being observed.
        /// </summary>
        public INotifyPropertyChanged Source
        {
            get { return _watcher.Source; }
            set { _watcher.Source = value; }
        }

        /// <summary>
        /// Registers the property pass through.
        /// </summary>
        /// <param name="sourcePropertyName">The property on <typeparamref name="TSource"/> to watch for changes.</param>
        /// <param name="targetPropertyName">The property on the target object to raise when the source property changed.</param>
        public void RegisterPropertyPassThrough(string sourcePropertyName, string targetPropertyName)
        {
            // TODO: support multiple targets dependant on single source?
            _watcher = _watcher.AddHandler(sourcePropertyName, targetPropertyName);
        }

        private void DispatchPropertyChanged(string propertyName, object callbackData)
        {
            _handler(new PropertyChangedEventArgs((string)callbackData));
        }
    }
}
