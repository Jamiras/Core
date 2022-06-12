using System.Collections.Generic;
using Jamiras.Components;
using System;

namespace Jamiras.DataModels
{
    /// <summary>
    /// The base class for models that can be modified and reverted.
    /// </summary>
    public abstract class DataModelBase : ModelBase
    {
        /// <summary>
        /// Constructs a new <see cref="DataModelBase"/>.
        /// </summary>
        protected DataModelBase()
        {
            _updatedValues = EmptyTinyDictionary<int, object>.Instance;
        }

        private ITinyDictionary<int, object> _updatedValues;

        /// <summary>
        /// Gets the value of a <see cref="ModelProperty"/> for this instance.
        /// </summary>
        /// <param name="property">The <see cref="ModelProperty"/> to query.</param>
        /// <returns>The current value of the <see cref="ModelProperty"/> for this instance.</returns>
        public override sealed object GetValue(ModelProperty property)
        {
            object value;
            if (!_updatedValues.TryGetValue(property.Key, out value))
                value = base.GetValue(property);

            return value;
        }

        internal object GetOriginalValue(ModelProperty property)
        {
            return base.GetValue(property);
        }

        /// <summary>
        /// Sets the value of a <see cref="ModelProperty"/> for this instance.
        /// </summary>
        /// <param name="property">The <see cref="ModelProperty"/> to update.</param>
        /// <param name="value">The new value for the <see cref="ModelProperty"/>.</param>
        public override sealed void SetValue(ModelProperty property, object value)
        {
            if (property == null)
                throw new ArgumentNullException("property");
            if (property.DefaultValue is ModelProperty.UnitializedValue)
                throw new ArgumentException("Cannot assign value to derived property: " + property.FullName);

            object currentValue;

            lock (_lockObject)
            {
                object originalValue = GetOriginalValue(property);
                if (!_updatedValues.TryGetValue(property.Key, out currentValue))
                    currentValue = originalValue;

                if (Object.Equals(value, currentValue))
                    return;

                if (Object.Equals(value, originalValue))
                    _updatedValues = _updatedValues.Remove(property.Key);
                else
                    _updatedValues = _updatedValues.AddOrUpdate(property.Key, value);
            }

            OnModelPropertyChanged(new ModelPropertyChangedEventArgs(property, currentValue, value));
        }

        /// <summary>
        /// Sets the value of a <see cref="ModelProperty"/> for this instance without marking the model as modified.
        /// </summary>
        /// <param name="property">The <see cref="ModelProperty"/> to update.</param>
        /// <param name="value">The new value for the <see cref="ModelProperty"/>.</param>
        internal void SetOriginalValue(ModelProperty property, object value)
        {
            object currentValue;
            lock (_lockObject)
            {
                object originalValue = GetOriginalValue(property);
                if (Object.Equals(value, originalValue))
                    return;

                SetValueCore(property, value);

                if (!_updatedValues.TryGetValue(property.Key, out currentValue))
                {
                    currentValue = originalValue;
                    // this is the only case where we want to raise a property changed event
                }
                else
                {
                    if (Object.Equals(value, currentValue))
                        _updatedValues = _updatedValues.Remove(property.Key);

                    return;
                }
            }

            OnModelPropertyChanged(new ModelPropertyChangedEventArgs(property, currentValue, value));
        }

        /// <summary>
        /// Gets the list of unique identifiers of properties that have changed from the committed record.
        /// </summary>
        internal IEnumerable<int> UpdatedPropertyKeys
        {
            get { return _updatedValues.Keys; }
        }

        /// <summary>
        /// Gets whether or not there are pending changes to the model.
        /// </summary>
        public bool IsModified
        {
            get { return (_updatedValues.Count > 0); }
        }

        /// <summary>
        /// Accepts pending changes to the model.
        /// </summary>
        public virtual void AcceptChanges()
        {
            foreach (var kvp in _updatedValues)
            {
                var value = kvp.Value;
                var property = ModelProperty.GetPropertyForKey(kvp.Key);
                SetValueCore(property, value);
            }

            _updatedValues = EmptyTinyDictionary<int, object>.Instance;
        }

        /// <summary>
        /// Discards pending changes to the model.
        /// </summary>
        public virtual void DiscardChanges()
        {
            var revertedProperties = new List<ModelPropertyChangedEventArgs>();
            foreach (var kvp in _updatedValues)
            {
                var property = ModelProperty.GetPropertyForKey(kvp.Key);
                revertedProperties.Add(new ModelPropertyChangedEventArgs(property, kvp.Value, base.GetValue(property)));
            }
        
            _updatedValues = EmptyTinyDictionary<int, object>.Instance;
        
            foreach (var args in revertedProperties)
                OnModelPropertyChanged(args);
        }
    }
}
