using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace Jamiras.Components
{
    internal class SinglePropertyWatcher : IPropertyWatcher
    {
        public SinglePropertyWatcher(INotifyPropertyChanged source, Action<string, object> handler, string propertyName, object callbackData)
        {
            Source = source;
            _handler = handler;
            _propertyName = propertyName;
            _callbackData = callbackData;
        }

        private readonly Action<string, object> _handler;
        private readonly string _propertyName;
        private readonly object _callbackData;

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            return String.Format("PropertyWatcher: {0}", _propertyName);
        }

        /// <summary>
        /// Gets or sets the object being watched.
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

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private INotifyPropertyChanged _source;

        private void SourcePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == _propertyName)
                _handler(_propertyName, _callbackData);
        }

        /// <summary>
        /// Creates a new <see cref="IPropertyWatcher"/> that will watch the existing properties and the newly specified property.
        /// </summary>
        /// <param name="propertyName">Name of property to watch.</param>
        /// <param name="callbackData">Data to pass to the callback.</param>
        /// <returns>New <see cref="IPropertyWatcher"/> that will watch the existing properties and the newly specified property.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="propertyName"/> is already being watched.</exception>
        public IPropertyWatcher AddHandler(string propertyName, object callbackData)
        {
            if (propertyName == _propertyName)
                throw new InvalidOperationException("Already watching " + propertyName);

            return new DoublePropertyWatcher(Source, _handler, _propertyName, _callbackData, propertyName, callbackData);
        }

        /// <summary>
        /// Removes the handler for the specified property.
        /// </summary>
        /// <param name="propertyName">Name of property to stop watching.</param>
        /// <returns>New <see cref="IPropertyWatcher"/> that will watch the existing properties minus the specified property.</returns>
        public IPropertyWatcher RemoveHandler(string propertyName)
        {
            if (propertyName == _propertyName)
            {
                if (_source != null)
                    _source.PropertyChanged -= SourcePropertyChanged;

                return new NoPropertyWatcher(Source, _handler);
            }

            return this;
        }

        /// <summary>
        /// Determines whether or not the specified property is being watched.
        /// </summary>
        /// <param name="propertyName">Name of property to query.</param>
        /// <returns><c>true</c> if the property is being watched, <c>false</c> if not.</returns>
        public bool IsWatching(string propertyName)
        {
            return (propertyName == _propertyName);
        }

        /// <summary>
        /// Gets a list of all properties being watched.
        /// </summary>
        public IEnumerable<string> WatchedProperties
        {
            get { return new string[] { _propertyName}; }
        }

        /// <summary>
        /// Gets the callback data registered for a property.
        /// </summary>
        /// <param name="propertyName">Name of property to query.</param>
        public object GetCallbackData(string propertyName)
        {
            if (propertyName == _propertyName)
                return _callbackData;

            return null;
        }
    }
}
