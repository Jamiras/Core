using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace Jamiras.Components
{
    internal class DoublePropertyWatcher : IPropertyWatcher
    {
        public DoublePropertyWatcher(INotifyPropertyChanged source, Action<string, object> handler,
                                     string propertyName, object callbackData,
                                     string propertyName2, object callbackData2)
        {
            Source = source;
            _handler = handler;
            _propertyName = propertyName;
            _callbackData = callbackData;
            _propertyName2 = propertyName2;
            _callbackData2 = callbackData2;
        }

        private readonly Action<string, object> _handler;
        private readonly string _propertyName;
        private readonly object _callbackData;
        private readonly string _propertyName2;
        private readonly object _callbackData2;

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            return String.Format("PropertyWatcher: {0}, {1}", _propertyName, _propertyName2);
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
            else if (e.PropertyName == _propertyName2)
                _handler(_propertyName2, _callbackData2);
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
            if (propertyName == _propertyName || propertyName == _propertyName2)
                throw new InvalidOperationException("Already watching " + propertyName);

            var watcher = new MultiplePropertyWatcher(Source, _handler);
            watcher.AddHandler(_propertyName, _callbackData);
            watcher.AddHandler(_propertyName2, _callbackData2);
            watcher.AddHandler(propertyName, callbackData);
            return watcher;
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

                return new SinglePropertyWatcher(Source, _handler, _propertyName2, _callbackData2);
            }

            if (propertyName == _propertyName2)
            {
                if (_source != null)
                    _source.PropertyChanged -= SourcePropertyChanged;

                return new SinglePropertyWatcher(Source, _handler, _propertyName, _callbackData);
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
            return (propertyName == _propertyName || propertyName == _propertyName2);
        }

        /// <summary>
        /// Gets a list of all properties being watched.
        /// </summary>
        public IEnumerable<string> WatchedProperties
        {
            get { return new string[] { _propertyName, _propertyName2 }; }
        }

        /// <summary>
        /// Gets the callback data registered for a property.
        /// </summary>
        /// <param name="propertyName">Name of property to query.</param>
        public object GetCallbackData(string propertyName)
        {
            if (propertyName == _propertyName)
                return _callbackData;
            else if (propertyName == _propertyName2)
                return _callbackData2;

            return null;
        }
    }
}
