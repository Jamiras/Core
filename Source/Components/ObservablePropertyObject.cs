using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Jamiras.Components
{
    /// <summary>
    /// Base class extending <see cref="PropertyChangedObject"/> with support for observing 
    /// specific properties, including having the new and old property values in the event args.
    /// </summary>
    public abstract class ObservablePropertyObject : PropertyChangedObject
    {
        protected ObservablePropertyObject()
        {
            _observedProperties = EmptyTinyDictionary<string, ObservedProperty>.Instance;
        }

        private ITinyDictionary<string, ObservedProperty> _observedProperties;

        private class ObservedProperty
        {
            public ObservedProperty(PropertyInfo propertyInfo)
            {
                PropertyInfo = propertyInfo;
            }

            public object CurrentValue { get; set; }
            public PropertyInfo PropertyInfo { get; private set; }
            public EventHandler<ObservablePropertyChangedEventArgs>[] Handlers { get; set; }
        }

        /// <summary>
        /// Subscribes to the PropertyChanged event for a single property.
        /// </summary>
        /// <param name="propertyName">The name of the property to watch.</param>
        /// <param name="handler">The method to call when the property changes.</param>
        public void ObserveProperty(string propertyName, EventHandler<ObservablePropertyChangedEventArgs> handler)
        {
            var propertyInfo = GetType().GetProperty(propertyName);
            if (propertyInfo == null)
                throw new ArgumentException("propertyName");

            ObserveProperty(propertyInfo, handler);
        }

        /// <summary>
        /// Subscribes to the PropertyChanged event for a single property.
        /// </summary>
        /// <typeparam name="TPropertyType">The type of the property (can be inferred)</typeparam>
        /// <param name="expression">A lambda expression for the property: () => PropertyName</param>
        /// <param name="handler">The method to call when the property changes.</param>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "Lambda inference at the call site doesn't work without the derived type.")]
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public void ObserveProperty<TPropertyType>(Expression<Func<TPropertyType>> expression, EventHandler<ObservablePropertyChangedEventArgs> handler)
        {
            var propertyInfo = expression.GetMemberInfo() as PropertyInfo;
            if (propertyInfo == null)
                throw new ArgumentException("expression");

            ObserveProperty(propertyInfo, handler);
        }

        private void ObserveProperty(PropertyInfo propertyInfo, EventHandler<ObservablePropertyChangedEventArgs> handler)
        {
            ObservedProperty observedProperty;
            if (!_observedProperties.TryGetValue(propertyInfo.Name, out observedProperty))
            {
                observedProperty = new ObservedProperty(propertyInfo);
                observedProperty.CurrentValue = propertyInfo.GetValue(this, null);
                observedProperty.Handlers = new EventHandler<ObservablePropertyChangedEventArgs>[1];

                _observedProperties = _observedProperties.Add(propertyInfo.Name, observedProperty);
            }

            int idx = 0;
            while (idx < observedProperty.Handlers.Length && observedProperty.Handlers[idx] != null)
                idx++;

            if (idx == observedProperty.Handlers.Length)
            {
                var newHandlers = new EventHandler<ObservablePropertyChangedEventArgs>[idx * 2];
                Array.Copy(observedProperty.Handlers, 0, newHandlers, 0, idx);
                observedProperty.Handlers = newHandlers;
            }

            observedProperty.Handlers[idx] = handler;
        }

        /// <summary>
        /// Unsubscribes from the PropertyChanged event for a single property.
        /// </summary>
        /// <param name="propertyName">The name of the property to watch.</param>
        /// <param name="handler">The method to no longer call when the property changes.</param>
        public void StopObservingProperty(string propertyName, EventHandler<ObservablePropertyChangedEventArgs> handler)
        {
            var propertyInfo = GetType().GetProperty(propertyName);
            if (propertyInfo == null)
                throw new ArgumentException("propertyName");

            StopObservingProperty(propertyInfo, handler);
        }

        /// <summary>
        /// Unsubscribes from the PropertyChanged event for a single property.
        /// </summary>
        /// <typeparam name="TPropertyType">The type of the property (can be inferred)</typeparam>
        /// <param name="expression">A lambda expression for the property: () => PropertyName</param>
        /// <param name="handler">The method to no longer call when the property changes.</param>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "Lambda inference at the call site doesn't work without the derived type.")]
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public void StopObservingProperty<TPropertyType>(Expression<Func<TPropertyType>> expression, EventHandler<ObservablePropertyChangedEventArgs> handler)
        {
            var propertyInfo = expression.GetMemberInfo() as PropertyInfo;
            if (propertyInfo == null)
                throw new ArgumentException("expression");

            StopObservingProperty(propertyInfo, handler);
        }

        private void StopObservingProperty(PropertyInfo propertyInfo, EventHandler<ObservablePropertyChangedEventArgs> handler)
        {
            ObservedProperty observedProperty;
            if (_observedProperties.TryGetValue(propertyInfo.Name, out observedProperty))
            {
                bool liveHandlers = false;

                int idx = 0;
                while (idx < observedProperty.Handlers.Length)
                {
                    var scan = observedProperty.Handlers[idx];
                    if (scan != null)
                    {
                        if (scan == handler)
                            observedProperty.Handlers[idx] = null;
                        else
                            liveHandlers = true;
                    }

                    idx++;
                }

                if (!liveHandlers)
                    _observedProperties = _observedProperties.Remove(propertyInfo.Name);
            }
        }

        /// <summary>
        /// Raises the PropertyChanged event.
        /// </summary>
        /// <param name="e">Information about which property changed</param>
        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            ObservedProperty observedProperty;
            if (_observedProperties.TryGetValue(e.PropertyName, out observedProperty))
            {
                bool valueChanged = true;

                var observablePropertyChangedEventArgs = e as ObservablePropertyChangedEventArgs;
                if (observablePropertyChangedEventArgs == null)
                {
                    var oldValue = observedProperty.CurrentValue;
                    var newValue = observedProperty.PropertyInfo.GetValue(this, null);

                    if (newValue == null)
                    {
                        if (oldValue == null)
                            valueChanged = false;
                    }
                    else if (newValue.GetType().IsClass)
                    {
                        if (ReferenceEquals(newValue, oldValue))
                            valueChanged = false;
                    }
                    else
                    {
                        if (Equals(newValue, oldValue))
                            valueChanged = false;
                    }

                    observablePropertyChangedEventArgs = new ObservablePropertyChangedEventArgs(e.PropertyName, newValue, oldValue);
                }

                if (valueChanged)
                {
                    observedProperty.CurrentValue = observablePropertyChangedEventArgs.NewValue;

                    foreach (var handler in observedProperty.Handlers)
                    {
                        if (handler != null)
                            handler(this, observablePropertyChangedEventArgs);
                    }

                    e = observablePropertyChangedEventArgs;
                }
            }

            base.OnPropertyChanged(e);
        }
    }
}
