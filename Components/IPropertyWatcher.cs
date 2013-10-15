using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Jamiras.Components
{
    internal interface IPropertyWatcher
    {
        /// <summary>
        /// Gets or sets the object being watched.
        /// </summary>
        INotifyPropertyChanged Source { get; set; }

        /// <summary>
        /// Creates a new <see cref="IPropertyWatcher"/> that will watch the existing properties and the newly specified property.
        /// </summary>
        /// <param name="propertyName">Name of property to watch.</param>
        /// <param name="callbackData">Data to pass to the callback.</param>
        /// <returns>New <see cref="IPropertyWatcher"/> that will watch the existing properties and the newly specified property.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="propertyName"/> is already being watched.</exception>
        IPropertyWatcher AddHandler(string propertyName, object callbackData);

        /// <summary>
        /// Removes the handler for the specified property.
        /// </summary>
        /// <param name="propertyName">Name of property to stop watching.</param>
        /// <returns>New <see cref="IPropertyWatcher"/> that will watch the existing properties minus the specified property.</returns>
        IPropertyWatcher RemoveHandler(string propertyName);

        /// <summary>
        /// Determines whether or not the specified property is being watched.
        /// </summary>
        /// <param name="propertyName">Name of property to query.</param>
        /// <returns><c>true</c> if the property is being watched, <c>false</c> if not.</returns>
        bool IsWatching(string propertyName);

        /// <summary>
        /// Gets a list of all properties being watched.
        /// </summary>
        IEnumerable<string> WatchedProperties { get; }

        /// <summary>
        /// Gets the callback data registered for a property.
        /// </summary>
        /// <param name="propertyName">Name of property to query.</param>
        object GetCallbackData(string propertyName);
    }
}
