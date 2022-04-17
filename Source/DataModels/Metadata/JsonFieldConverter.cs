﻿using Jamiras.IO.Serialization;
using Jamiras.ViewModels.Converters;
using System;

namespace Jamiras.DataModels.Metadata
{
    /// <summary>
    /// Converter for mapping a <see cref="ModelProperty"/> to a JSON field.
    /// </summary>
    public class JsonFieldConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonFieldConverter"/> class.
        /// </summary>
        /// <param name="jsonFieldName">Name of the json field.</param>
        /// <param name="property">The property to map.</param>
        public JsonFieldConverter(string jsonFieldName, ModelProperty property)
            : this(jsonFieldName, property, null, JsonField.GetFieldType(property.PropertyType))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonFieldConverter"/> class.
        /// </summary>
        /// <param name="jsonFieldName">Name of the json field.</param>
        /// <param name="property">The property to map.</param>
        /// <param name="converter">Converter to change the data type when mapping.</param>
        /// <param name="type">The target JSON type.</param>
        public JsonFieldConverter(string jsonFieldName, ModelProperty property, IConverter converter, JsonFieldType type)
        {
            JsonFieldName = jsonFieldName;
            Property = property;
            Type = type;
            _converter = converter;
        }

        private readonly IConverter _converter;
        private object _ignoreValue;
        private bool _hasIgnoreValue;

        /// <summary>
        /// Gets the JSON field name.
        /// </summary>
        public string JsonFieldName { get; private set; }

        /// <summary>
        /// Gets the model property.
        /// </summary>
        public ModelProperty Property { get; private set; }

        /// <summary>
        /// Gets the JSON field type.
        /// </summary>
        public JsonFieldType Type { get; private set; }

        /// <summary>
        /// Gets or sets a value that if the field is set to will cause the field to not be serialized.
        /// </summary>
        public object IgnoreValue 
        {
            get { return _ignoreValue; }
            set
            {
                _ignoreValue = value;
                _hasIgnoreValue = true;
            }
        }

        /// <summary>
        /// Serializes the value from <paramref name="model"/> into <paramref name="jsonObject"/>.
        /// </summary>
        public void Serialize(JsonObject jsonObject, ModelBase model)
        {
            var value = GetValue(model);
            if (_hasIgnoreValue && Object.Equals(value, _ignoreValue))
                return;

            if (_converter != null)
            {
                var result = _converter.Convert(ref value);
                if (!String.IsNullOrEmpty(result))
                    throw new InvalidOperationException("Cannot convert " + Property.PropertyName);
            }

            jsonObject.AddField(JsonFieldName, Type, value);
        }

        /// <summary>
        /// Gets the value from the model.
        /// </summary>
        protected virtual object GetValue(ModelBase model)
        {
            return model.GetValue(Property);
        }
    }
}
