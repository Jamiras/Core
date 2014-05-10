using System;
using System.Collections.Generic;
using System.ComponentModel;
using Jamiras.Components;

namespace Jamiras.DataModels
{
    /// <summary>
    /// The base class for objects that support <see cref="ModelProperty"/>s.
    /// </summary>
    public abstract class ModelBase : PropertyChangedObject
    {
        /// <summary>
        /// Constructs a new <see cref="ModelBase"/>.
        /// </summary>
        protected ModelBase()
        {
            _values = EmptyTinyDictionary<int, object>.Instance;
            _propertyChangedHandlers = EmptyTinyDictionary<int, List<WeakReference>>.Instance;
            _lockObject = new object();
        }

        private ITinyDictionary<int, object> _values;
        private ITinyDictionary<int, List<WeakReference>> _propertyChangedHandlers;
        internal object _lockObject;

        /// <summary>
        /// Gets the value of a <see cref="ModelProperty"/> for this instance.
        /// </summary>
        /// <param name="property">The <see cref="ModelProperty"/> to query.</param>
        /// <returns>The current value of the <see cref="ModelProperty"/> for this instance.</returns>
        public virtual object GetValue(ModelProperty property)
        {
            object value;
            if (!_values.TryGetValue(property.Key, out value))
                value = property.DefaultValue;

            return value;
        }

        /// <summary>
        /// Sets the value of a <see cref="ModelProperty"/> for this instance.
        /// </summary>
        /// <param name="property">The <see cref="ModelProperty"/> to update.</param>
        /// <param name="value">The new value for the <see cref="ModelProperty"/>.</param>
        public virtual void SetValue(ModelProperty property, object value)
        {
            object currentValue;
            if (!_values.TryGetValue(property.Key, out currentValue))
                currentValue = property.DefaultValue;

            if (value != currentValue)
            {
                SetValueCore(property, value);
                OnModelPropertyChanged(new ModelPropertyChangedEventArgs(property, currentValue, value));
            }
        }

        /// <summary>
        /// Sets the value of a <see cref="ModelProperty"/> for this instance without checking for changes or raising the change-related events.
        /// </summary>
        internal void SetValueCore(ModelProperty property, object value)
        {
            if (value != property.DefaultValue)
                _values = _values.AddOrUpdate(property.Key, value);
            else
                _values = _values.Remove(property.Key);
        }

        /// <summary>
        /// Notifies any subscribers that the value of a <see cref="ModelProperty"/> has changed.
        /// </summary>
        protected virtual void OnModelPropertyChanged(ModelPropertyChangedEventArgs e)
        {
            if (_propertyChangedHandlers.Count > 0)
            {
                List<EventHandler<ModelPropertyChangedEventArgs>> handlers = null;

                lock (_lockObject)
                {
                    List<WeakReference> dependencies;
                    if (_propertyChangedHandlers.TryGetValue(e.Property.Key, out dependencies))
                    {
                        handlers = new List<EventHandler<ModelPropertyChangedEventArgs>>();
                        for (int i = dependencies.Count - 1; i >= 0; i--)
                        {
                            var wr = dependencies[i];
                            var handler = wr.Target as EventHandler<ModelPropertyChangedEventArgs>;
                            if (handler != null && wr.IsAlive)
                                handlers.Add(handler);
                            else
                                dependencies.RemoveAt(i);
                        }
                    }

                    if (dependencies.Count == 0)
                        _propertyChangedHandlers = _propertyChangedHandlers.Remove(e.Property.Key);
                }

                if (handlers != null)
                {
                    foreach (var handler in handlers)
                        handler(this, e);
                }
            }

            OnPropertyChanged(new PropertyChangedEventArgs(e.Property.PropertyName));
        }

        /// <summary>
        /// Registers a callback to call when a specified property changes.
        /// </summary>
        /// <param name="property">The property to monitor.</param>
        /// <param name="handler">The method to call when the property changes.</param>
        public void AddPropertyChangedHandler(ModelProperty property, EventHandler<ModelPropertyChangedEventArgs> handler)
        {
            lock (_lockObject)
            {
                List<WeakReference> dependencies;
                if (!_propertyChangedHandlers.TryGetValue(property.Key, out dependencies))
                {
                    dependencies = new List<WeakReference>();
                    _propertyChangedHandlers = _propertyChangedHandlers.AddOrUpdate(property.Key, dependencies);
                }

                dependencies.Add(new WeakReference(handler));
            }
        }

        /// <summary>
        /// Unregisters a callback to call when a specified property changes.
        /// </summary>
        /// <param name="property">The property being monitored.</param>
        /// <param name="handler">The method to no longer call when the property changes.</param>
        public void RemovePropertyChangedHandler(ModelProperty property, EventHandler<ModelPropertyChangedEventArgs> handler)
        {
            if (_propertyChangedHandlers.Count == 0)
                return;

            lock (_lockObject)
            {
                List<WeakReference> dependencies;
                if (_propertyChangedHandlers.TryGetValue(property.Key, out dependencies))
                {
                    for (int i = dependencies.Count - 1; i >= 0; i--)
                    {
                        var wr = dependencies[i];
                        var h = wr.Target as EventHandler<ModelPropertyChangedEventArgs>;
                        if (h == null || !wr.IsAlive)
                            dependencies.RemoveAt(i);
                        else if (h == handler)
                            dependencies.RemoveAt(i);
                    }

                    if (dependencies.Count == 0)
                        _propertyChangedHandlers = _propertyChangedHandlers.Remove(property.Key);
                }
            }
        }
    }
}
