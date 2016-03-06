using System;
using System.Collections.Generic;
using Jamiras.Components;
using Jamiras.IO.Serialization;
using Jamiras.ViewModels.Converters;

namespace Jamiras.DataModels.Metadata
{
    /// <summary>
    /// The base class for model metadata.
    /// </summary>
    public abstract class ModelMetadata
    {
        protected ModelMetadata()
        {
            _fieldMetadata = EmptyTinyDictionary<int, FieldMetadata>.Instance;
            _fieldConverters = EmptyTinyDictionary<int, IConverter>.Instance;
            _jsonFields = new List<JsonFieldConverter>();
        }

        private ITinyDictionary<int, FieldMetadata> _fieldMetadata;
        private ITinyDictionary<int, IConverter> _fieldConverters;
        private readonly List<JsonFieldConverter> _jsonFields;
        private static int _nextKey = -100;

        internal ITinyDictionary<int, FieldMetadata> AllFieldMetadata
        {
            get { return _fieldMetadata; }
        }

        /// <summary>
        /// Registers metadata for a <see cref="ModelProperty"/>.
        /// </summary>
        /// <param name="property">Property to register metadata for.</param>
        /// <param name="metadata">Metadata for the field.</param>
        protected virtual void RegisterFieldMetadata(ModelProperty property, FieldMetadata metadata, IConverter converter)
        {
            _fieldMetadata = _fieldMetadata.AddOrUpdate(property.Key, metadata);

            if (converter != null)
                _fieldConverters = _fieldConverters.AddOrUpdate(property.Key, converter);

            if ((metadata.Attributes & InternalFieldAttributes.PrimaryKey) != 0 && PrimaryKeyProperty == null)
                PrimaryKeyProperty = property;
        }

        /// <summary>
        /// Gets the property for the primary key of the record.
        /// </summary>
        public ModelProperty PrimaryKeyProperty { get; protected set; }

        /// <summary>
        /// Gets the primary key value of a model.
        /// </summary>
        /// <param name="model">The model to get the primary key for.</param>
        /// <returns>The primary key of the model.</returns>
        public virtual int GetKey(ModelBase model)
        {
            if (PrimaryKeyProperty == null)
                throw new InvalidOperationException("Could not determine primary key for " + GetType().Name);

            return (int)model.GetValue(PrimaryKeyProperty);
        }

        internal void InitializePrimaryKey(ModelBase model)
        {
            if (PrimaryKeyProperty != null)
                model.SetValue(PrimaryKeyProperty, _nextKey--);
        }

        /// <summary>
        /// Gets the metadata registered for a <see cref="ModelProperty"/>.
        /// </summary>
        /// <param name="property">The property to get the metadata for.</param>
        /// <returns>Requested metadata, <c>null</c> if not found.</returns>
        internal FieldMetadata GetFieldMetadata(ModelProperty property)
        {
            FieldMetadata metadata;
            _fieldMetadata.TryGetValue(property.Key, out metadata);
            return metadata;
        }

        /// <summary>
        /// Gets the converter registered for a <see cref="ModelProperty"/>.
        /// </summary>
        /// <param name="property">The property to get the metadata for.</param>
        /// <returns>Requested converter, <c>null</c> if not found.</returns>
        internal IConverter GetConverter(ModelProperty property)
        {
            IConverter converter;
            _fieldConverters.TryGetValue(property.Key, out converter);
            return converter;
        }

        /// <summary>
        /// Gets the JSON object name associated to the model.
        /// </summary>
        public string JsonObjectName { get; protected set; }

        /// <summary>
        /// Gets the JSON fields associated to the model.
        /// </summary>
        public IEnumerable<JsonFieldConverter> JsonFields
        {
            get { return _jsonFields; }
        }

        /// <summary>
        /// Registers a JSON field for the model.
        /// </summary>
        protected JsonFieldConverter AddJsonField(string jsonFieldName, ModelProperty modelProperty)
        {
            var field = new JsonFieldConverter(jsonFieldName, modelProperty);
            _jsonFields.Add(field);
            return field;
        }

        /// <summary>
        /// Registers a JSON field for the model.
        /// </summary>
        protected JsonFieldConverter AddJsonField(string jsonFieldName, ModelProperty modelProperty, IConverter converter, JsonFieldType type)
        {
            var field = new JsonFieldConverter(jsonFieldName, modelProperty, converter, type);
            _jsonFields.Add(field);
            return field;
        }

        /// <summary>
        /// Registers a JSON field for the model.
        /// </summary>
        protected void AddJsonField(JsonFieldConverter field)
        {
            _jsonFields.Add(field);
        }
    }
}
