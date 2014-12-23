using Jamiras.Components;

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
        }

        private ITinyDictionary<int, FieldMetadata> _fieldMetadata;

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
        /// Initializes default values for a new record.
        /// </summary>
        /// <param name="dataModel">Model to initialize.</param>
        public virtual void InitializeNewRecord(ModelBase dataModel)
        {
        }
    }
}
