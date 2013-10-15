using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace Jamiras.Components
{
    internal class MultiplePropertyWatcher : IPropertyWatcher
    {
        public MultiplePropertyWatcher(INotifyPropertyChanged source, Action<string, object> handler)
        {
            _handler = handler;
            Source = source;
        }

        private int _propertyCount;
        private string[] _properties;
        private object[] _callbackData;
        private readonly Action<string, object> _handler;

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("PropertyWatcher: ");
            
            for (int i = 0; i < _propertyCount; i++)
            {
                builder.Append(_properties[i]);
                builder.Append(", ");
            }
            builder.Length -= 2;

            return builder.ToString();
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
            int idx = FindHandler(e.PropertyName);
            if (idx >= 0)
                _handler(e.PropertyName, _callbackData[idx]);
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
            lock (this)
            {
                int idx = FindHandler(propertyName);
                if (idx >= 0)
                    throw new InvalidOperationException("Already watching " + propertyName);

                idx = ~idx;
                if (_properties == null)
                {
                    _properties = new string[4];
                    _callbackData = new object[4];
                }
                else if (_propertyCount < _properties.Length)
                {
                    for (int i = _propertyCount; i > idx; i--)
                    {
                        _properties[i] = _properties[i - 1];
                        _callbackData[i] = _callbackData[i - 1];
                    }
                }
                else
                {
                    var newProperties = new string[_properties.Length + 4];
                    Array.Copy(_properties, newProperties, idx);
                    Array.Copy(_properties, idx, newProperties, idx + 1, _propertyCount - idx);
                    _properties = newProperties;

                    var newCallbackData = new object[_properties.Length + 4];
                    Array.Copy(_callbackData, newCallbackData, idx);
                    Array.Copy(_callbackData, idx, newCallbackData, idx + 1, _propertyCount - idx);
                    _callbackData = newCallbackData;
                }

                _properties[idx] = propertyName;
                _callbackData[idx] = callbackData;               
                _propertyCount++;
            }

            return this;
        }

        /// <summary>
        /// Removes the handler for the specified property.
        /// </summary>
        /// <param name="propertyName">Name of property to stop watching.</param>
        /// <returns>New <see cref="IPropertyWatcher"/> that will watch the existing properties minus the specified property.</returns>
        public IPropertyWatcher RemoveHandler(string propertyName)
        {
            lock (this)
            {
                int idx = FindHandler(propertyName);
                if (idx >= 0)
                {
                    if (idx + 1 < _propertyCount)
                    {
                        Array.Copy(_properties, idx + 1, _properties, idx, _propertyCount - idx - 1);
                        Array.Copy(_callbackData, idx + 1, _callbackData, idx, _propertyCount - idx - 1);
                    }

                    _propertyCount--;
                    _properties[_propertyCount] = null;
                    _callbackData[_propertyCount] = null;
                }
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
            int idx = FindHandler(propertyName);
            return (idx >= 0);
        }

        private int FindHandler(string propertyName)
        {
            int low = 0;
            int high = _propertyCount;

            while (low < high)
            {
                int mid = (low + high) / 2;

                int diff = String.Compare(propertyName, _properties[mid], StringComparison.Ordinal);
                if (diff == 0)
                    return mid;

                if (diff > 0)
                    low = mid + 1;
                else
                    high = mid;
            }

            return ~low;
        }

        /// <summary>
        /// Gets a list of all properties being watched.
        /// </summary>
        public IEnumerable<string> WatchedProperties
        {
            get 
            { 
                var properties = new string[_propertyCount];
                Array.Copy(_properties, properties, _propertyCount);
                return properties;
            }
        }

        /// <summary>
        /// Gets the callback data registered for a property.
        /// </summary>
        /// <param name="propertyName">Name of property to query.</param>
        public object GetCallbackData(string propertyName)
        {
            int idx = FindHandler(propertyName);
            if (idx >= 0)
                return _callbackData[idx];

            return null;
        }
    }
}
