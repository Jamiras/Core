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
        internal ModelProperty()
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

        /// <summary>
        /// Gets the properties that are dependant on this property.
        /// </summary>
        internal int[] DependantProperties { get; private set; }

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

            if (!property.IsValueValid(defaultValue) && !(defaultValue is UnitializedValue))
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
                    Array.Copy(oldProperties, _properties, _keyCount);
                }

                _properties[_keyCount] = property;
                property.Key = ++_keyCount;
            }

            return property;
        }

        /// <summary>
        /// Registers a <see cref="ModelProperty"/>.
        /// </summary>
        /// <param name="ownerType">The type of model that owns the property.</param>
        /// <param name="propertyName">The name of the property on the model.</param>
        /// <param name="propertyType">The type of the property.</param>
        /// <param name="dependancies">The properties this property is dependant on.</param>
        /// <param name="getValueFunction">The method that constructs the property value from the dependant properties.</param>
        public static ModelProperty RegisterDependant(Type ownerType, string propertyName, Type propertyType,
                                                      ModelProperty[] dependancies, Func<ModelBase, object> getValueFunction)
        {
            foreach (var dependancy in dependancies)
            {
                if (!dependancy.OwnerType.IsAssignableFrom(ownerType))
                    throw new ArgumentException("Dependant properties must be on same model. " + dependancy.FullName + " not on " + ownerType.FullName, "dependancies");
            }            

            var property = Register(ownerType, propertyName, propertyType, new UnitializedValue(getValueFunction));

            foreach (var dependancy in dependancies)
            {
                var dependantProperties = dependancy.DependantProperties;
                if (dependantProperties == null)
                {
                    dependantProperties = new[] { property.Key };
                }
                else
                {
                    dependantProperties = new int[dependantProperties.Length + 1];
                    Array.Copy(dependancy.DependantProperties, dependantProperties, dependancy.DependantProperties.Length);
                    dependantProperties[dependancy.DependantProperties.Length] = property.Key;                    
                }

                dependancy.DependantProperties = dependantProperties;
            }

            return property;
        }

        /// <summary>
        /// ModelProperty that is constructed from other ModelProperties, and therefore can be lazy populated.
        /// </summary>
        internal class UnitializedValue
        {
            internal UnitializedValue(Func<ModelBase, object> getValueFunction)
            {
                _getValueFunction = getValueFunction;
            }

            private readonly Func<ModelBase, object> _getValueFunction;

            /// <summary>
            /// Gets the value of the property for a given model.
            /// </summary>
            public object GetValue(ModelBase model)
            {
                return _getValueFunction(model);
            }
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
                if (_properties[i].OwnerType.IsAssignableFrom(type))
                    yield return _properties[i];
            }
        }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// true if the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>; otherwise, false.
        /// </returns>
        /// <param name="obj">The <see cref="T:System.Object"/> to compare with the current <see cref="T:System.Object"/>. </param><filterpriority>2</filterpriority>
        public override bool Equals(object obj)
        {
            var that = obj as ModelProperty;
            if (that == null)
                return false;

            return (Key == that.Key);
        }

        /// <summary>
        /// Serves as a hash function for a particular type. 
        /// </summary>
        /// <filterpriority>2</filterpriority>
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
            // UninitializedValue can only be assigned to a property registered using RegisterDependent, which sets the DefaultValue to an UninitializedValue
            if (value is UnitializedValue)
                return (DefaultValue is UnitializedValue);

            // null is not valid for value types (except Nullable<ValueType>)
            if (value == null)
                return !PropertyType.IsValueType || PropertyType.Name == "Nullable`1";

            // direct type match, or subclass
            if (PropertyType.IsAssignableFrom(value.GetType()))
                return true;

            // enums can be cast to ints
            if (PropertyType.IsEnum && value is int)
                return true;

            // ints can be cast to enums
            if (PropertyType == typeof(int) && value.GetType().IsEnum)
                return true;

            // doubles
            if (value is Single)
                return (PropertyType == typeof(double) || PropertyType == typeof(float));

            // not a valid cast
            return false;
        }
    }
}
