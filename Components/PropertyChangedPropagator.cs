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
            _propertyMap = EmptyTinyDictionary<string, string>.Instance;
        }

        private INotifyPropertyChanged _source;
        private ITinyDictionary<string, string> _propertyMap;
        private readonly Action<PropertyChangedEventArgs> _handler;

        /// <summary>
        /// Gets or sets the object being observed.
        /// </summary>
        public INotifyPropertyChanged Source
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

        private void SourcePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            string targetProperty;
            if (_propertyMap.TryGetValue(e.PropertyName, out targetProperty))
                _handler(new PropertyChangedEventArgs(targetProperty));
        }

        /// <summary>
        /// Registers the property pass through.
        /// </summary>
        /// <param name="sourcePropertyName">The property on <typeparamref name="TSource"/> to watch for changes.</param>
        /// <param name="targetPropertyName">The property on the target object to raise when the source property changed.</param>
        public void RegisterPropertyPassThrough(string sourcePropertyName, string targetPropertyName)
        {
            // TODO: support multiple targets dependant on single source?
            _propertyMap = _propertyMap.Add(sourcePropertyName, targetPropertyName);
        }
    }
}
