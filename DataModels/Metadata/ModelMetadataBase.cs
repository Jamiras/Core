using Jamiras.Components;
using System.Collections.Generic;
using Jamiras.ViewModels.Converters;
using Jamiras.IO.Serialization;

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
            _jsonFields = new List<JsonFieldConverter>();
        }

        private ITinyDictionary<int, FieldMetadata> _fieldMetadata;
        private readonly List<JsonFieldConverter> _jsonFields;

        internal ITinyDictionary<int, FieldMetadata> AllFieldMetadata
        {
            get { return _fieldMetadata; }
        }

        /// <summary>
        /// Registers metadata for a <see cref="ModelProperty"/>.
        /// </summary>
        /// <param name="property">Property to register metadata for.</param>
        /// <param name="metadata">Metadata for the field.</param>
        protected virtual void RegisterFieldMetadata(ModelProperty property, FieldMetadata metadata)
        {
            _fieldMetadata = _fieldMetadata.AddOrUpdate(property.Key, metadata);
        }

        /// <summary>
        /// Gets the metadata registered for a <see cref="ModelProperty"/>.
        /// </summary>
        /// <param name="property">The property to get the metadata for.</param>
        /// <returns>Requested metadata, <c>null</c> if not found.</returns>
        public FieldMetadata GetFieldMetadata(ModelProperty property)
        {
            FieldMetadata metadata;
            _fieldMetadata.TryGetValue(property.Key, out metadata);
            return metadata;
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
