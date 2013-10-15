using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Jamiras.Components
{
    internal class NoPropertyWatcher : IPropertyWatcher
    {
        public NoPropertyWatcher(INotifyPropertyChanged source, Action<string, object> handler)
        {
            Source = source;
            _handler = handler;
        }

        private readonly Action<string, object> _handler;

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            return "PropertyWatcher: none";
        }

        /// <summary>
        /// Gets or sets the object being watched.
        /// </summary>
        public INotifyPropertyChanged Source { get; set; }

        /// <summary>
        /// Creates a new <see cref="IPropertyWatcher"/> that will watch the existing properties and the newly specified property.
        /// </summary>
        /// <param name="propertyName">Name of property to watch.</param>
        /// <param name="callbackData">Data to pass to the callback.</param>
        /// <returns>New <see cref="IPropertyWatcher"/> that will watch the existing properties and the newly specified property.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="propertyName"/> is already being watched.</exception>
        public IPropertyWatcher AddHandler(string propertyName, object callbackData)
        {
            return new SinglePropertyWatcher(Source, _handler, propertyName, callbackData);
        }

        /// <summary>
        /// Removes the handler for the specified property.
        /// </summary>
        /// <param name="propertyName">Name of property to stop watching.</param>
        /// <returns>New <see cref="IPropertyWatcher"/> that will watch the existing properties minus the specified property.</returns>
        public IPropertyWatcher RemoveHandler(string propertyName)
        {
            return this;
        }

        /// <summary>
        /// Determines whether or not the specified property is being watched.
        /// </summary>
        /// <param name="propertyName">Name of property to query.</param>
        /// <returns><c>true</c> if the property is being watched, <c>false</c> if not.</returns>
        public bool IsWatching(string propertyName)
        {
            return false;
        }

        /// <summary>
        /// Gets a list of all properties being watched.
        /// </summary>
        public IEnumerable<string> WatchedProperties
        {
            get { return new string[0]; }
        }

        /// <summary>
        /// Gets the callback data registered for a property.
        /// </summary>
        /// <param name="propertyName">Name of property to query.</param>
        public object GetCallbackData(string propertyName)
        {
            return null;
        }
    }
}
