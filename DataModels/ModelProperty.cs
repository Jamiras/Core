using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Jamiras.DataModels
{
    /// <summary>
    /// Represents a single property on a <see cref="ModelBase"/>.
    /// </summary>
    [DebuggerDisplay("{FullName,nq} ModelProperty")]
    public class ModelProperty
    {
        private ModelProperty()
        {
        }

        /// <summary>
        /// Gets the name of the property and the type of model that owns the property.
        /// </summary>
        public string FullName
        {
            get { return String.Format("{0}.{1}", OwnerType.Name, PropertyName); }
        }

        /// <summary>
        /// Gets the unique identifier for the property.
        /// </summary>
        internal int Key { get; private set; }

        /// <summary>
        /// Gets the type of model that owns the property.
        /// </summary>
        public Type OwnerType { get; private set; }

        /// <summary>
        /// Gets the type of data stored in the property.
        /// </summary>
        public Type PropertyType { get; private set; }

        /// <summary>
        /// Gets the name of the property on the model.
        /// </summary>
        public string PropertyName { get; private set; }

        /// <summary>
        /// Gets the default value of the property.
        /// </summary>
        public object DefaultValue { get; private set; }

        /// <summary>
        /// Gets the method to call when the value of the property changes.
        /// </summary>
        public EventHandler<ModelPropertyChangedEventArgs> PropertyChangedHandler { get; private set; }

        private static int _keyCount;
        private static ModelProperty[] _properties;

        /// <summary>
        /// Registers a <see cref="ModelProperty"/>.
        /// </summary>
        /// <param name="ownerType">The type of model that owns the property.</param>
        /// <param name="propertyName">The name of the property on the model.</param>
        /// <param name="propertyType">The type of the property.</param>
        /// <param name="defaultValue">The default value of the property.</param>
        /// <param name="propertyChangedHandler">Callback to call when the property changes.</param>
        public static ModelProperty Register(Type ownerType, string propertyName, Type propertyType, object defaultValue, EventHandler<ModelPropertyChangedEventArgs> propertyChangedHandler = null)
        {
            var property = new ModelProperty()
            {
                OwnerType = ownerType,
                PropertyName = propertyName,
                PropertyType = propertyType,
                DefaultValue = defaultValue,
                PropertyChangedHandler = propertyChangedHandler,
            };

            if (!property.IsValueValid(defaultValue))
                throw new InvalidCastException("Cannot store " + ((defaultValue != null) ? defaultValue.GetType().Name : "null") + " in " + property.FullName + " (" + property.PropertyType.Name + ")");

            lock (typeof(ModelProperty))
            {
                if (_properties == null)
                {
                    _properties = new ModelProperty[256];
                }
                else if (_keyCount == _properties.Length)
                {
                    var oldProperties = _properties;
                    _properties = new ModelProperty[_properties.Length + 256];
                    Array.Copy(_properties, oldProperties, _keyCount);
                }

                _properties[_keyCount] = property;
                property.Key = ++_keyCount;
            }

            return property;
        }

        /// <summary>
        /// Gets the <see cref="ModelProperty"/> for a given key.
        /// </summary>
        /// <param name="key">Unique identifier of the <see cref="ModelProperty"/> to locate.</param>
        /// <returns>Requested <see cref="ModelProperty"/>, <c>null</c> if not found.</returns>
        public static ModelProperty GetPropertyForKey(int key)
        {
            if (key < 1 || key > _keyCount)
                return null;

            return _properties[key - 1];
        }

        /// <summary>
        /// Gets all properties registered for a type.
        /// </summary>
        /// <param name="type">Owner type to locate properties for.</param>
        /// <returns>0 or more registered properties for the type.</returns>
        /// <remarks>Returns properties available to the type, including those from superclasses.</remarks>
        public static IEnumerable<ModelProperty> GetPropertiesForType(Type type)
        {
            for (int i = 0; i < _keyCount; i++)
            {
                if (type.IsAssignableFrom(_properties[i].OwnerType))
                    yield return _properties[i];
            }
        }

        public override bool Equals(object obj)
        {
            var that = obj as ModelProperty;
            if (that == null)
                return false;

            return (Key == that.Key);
        }

        public override int GetHashCode()
        {
            return Key;
        }

        /// <summary>
        /// Determines if a value can be assigned to the property.
        /// </summary>
        /// <param name="value">Value to test.</param>
        /// <returns><c>true</c> if the value is valid for the property type, <c>false</c> if not.</returns>
        public bool IsValueValid(object value)
        {
            // null is not value for value types
            if (value == null)
                return !PropertyType.IsValueType;

            // direct type match, or subclass
            if (PropertyType.IsAssignableFrom(value.GetType()))
                return true;

            // enums can be cast to ints
            if (PropertyType.IsEnum && value is int)
                return true;

            // ints can be cast to enums
            if (PropertyType == typeof(int) && value.GetType().IsEnum)
                return true;

            // not a valid cast
            return false;
        }
    }
}
