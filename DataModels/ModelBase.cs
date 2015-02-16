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
            _propertyChangedHandlers = EmptyTinyDictionary<int, List<WeakAction<object, ModelPropertyChangedEventArgs>>>.Instance;
            _lockObject = new object();
        }

        private ITinyDictionary<int, object> _values;
        private ITinyDictionary<int, List<WeakAction<object, ModelPropertyChangedEventArgs>>> _propertyChangedHandlers;
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
            {
                value = property.DefaultValue;

                var uninitializedValue = value as ModelProperty.UnitializedValue;
                if (uninitializedValue != null)
                {
                    value = uninitializedValue.GetValue(this);
                    SetValueCore(property, value);
                }
            }

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

            if (!Object.Equals(value, currentValue))
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
            if (!property.IsValueValid(value))
                throw new InvalidCastException("Cannot store " + ((value != null) ? value.GetType().Name : "null") + " in " + property.FullName + " (" + property.PropertyType.Name + ")");

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
            if (e.Property.PropertyChangedHandler != null)
                e.Property.PropertyChangedHandler(this, e);

            if (_propertyChangedHandlers.Count > 0)
            {
                WeakAction<object, ModelPropertyChangedEventArgs>[] handlers = null;
                lock (_lockObject)
                {
                    List<WeakAction<object, ModelPropertyChangedEventArgs>> propertyChangedHandlers;
                    if (_propertyChangedHandlers.TryGetValue(e.Property.Key, out propertyChangedHandlers))
                    {
                        for (int i = propertyChangedHandlers.Count - 1; i >= 0; i--)
                        {
                            if (!propertyChangedHandlers[i].IsAlive)
                                propertyChangedHandlers.RemoveAt(i);
                        }

                        if (propertyChangedHandlers.Count == 0)
                            _propertyChangedHandlers = _propertyChangedHandlers.Remove(e.Property.Key);
                        else
                            handlers = propertyChangedHandlers.ToArray();
                    }
                }

                if (handlers != null)
                {
                    foreach (var handler in handlers)
                        handler.Invoke(this, e);
                }
            }

            if (!String.IsNullOrEmpty(e.Property.PropertyName))
                OnPropertyChanged(new PropertyChangedEventArgs(e.Property.PropertyName));

            if (e.Property.DependantProperties != null)
            {
                var changedProperties = new List<ModelPropertyChangedEventArgs>();
                foreach (var propertyKey in e.Property.DependantProperties)
                {
                    object currentValue;
                    if (_values.TryGetValue(propertyKey, out currentValue))
                    {
                        var property = ModelProperty.GetPropertyForKey(propertyKey);
                        var value = ((ModelProperty.UnitializedValue)property.DefaultValue).GetValue(this);
                        if (!Object.Equals(value, currentValue))
                        {
                            SetValueCore(property, value);
                            changedProperties.Add(new ModelPropertyChangedEventArgs(property, currentValue, value));
                        }
                    }
                }

                foreach (var changedProperty in changedProperties)
                    OnModelPropertyChanged(changedProperty);
            }
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
                List<WeakAction<object, ModelPropertyChangedEventArgs>> handlers;
                if (!_propertyChangedHandlers.TryGetValue(property.Key, out handlers))
                {
                    handlers = new List<WeakAction<object, ModelPropertyChangedEventArgs>>();
                    _propertyChangedHandlers = _propertyChangedHandlers.AddOrUpdate(property.Key, handlers);
                }

                handlers.Add(new WeakAction<object, ModelPropertyChangedEventArgs>(handler.Method, handler.Target));
            }

            var uninitializedValue = property.DefaultValue as ModelProperty.UnitializedValue;
            if (uninitializedValue != null && !_values.ContainsKey(property.Key))
            {
                var value = uninitializedValue.GetValue(this);
                SetValueCore(property, value);
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
                List<WeakAction<object, ModelPropertyChangedEventArgs>> handlers;
                if (_propertyChangedHandlers.TryGetValue(property.Key, out handlers))
                {
                    for (int i = handlers.Count - 1; i >= 0; i--)
                    {
                        var wr = handlers[i];
                        if (!wr.IsAlive)
                            handlers.RemoveAt(i);
                        else if (wr.Method == handler.Method && ReferenceEquals(wr.Target, handler.Target))
                            handlers.RemoveAt(i);
                    }

                    if (handlers.Count == 0)
                        _propertyChangedHandlers = _propertyChangedHandlers.Remove(property.Key);
                }
            }
        }
    }
}
