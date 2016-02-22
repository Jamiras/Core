using System;
using Jamiras.Components;

namespace Jamiras.DataModels.Metadata
{
    [Export(typeof(IDataModelMetadataRepository))]
    public class DataModelMetadataRepository : IDataModelMetadataRepository
    {
        public DataModelMetadataRepository()
        {
            _repository = EmptyTinyDictionary<Type, ModelMetadata>.Instance;
            _mapping = EmptyTinyDictionary<Type, Type>.Instance;
            _modelNames = EmptyTinyDictionary<string, Type>.Instance;
        }

        private ITinyDictionary<Type, ModelMetadata> _repository;
        private ITinyDictionary<Type, Type> _mapping;
        private ITinyDictionary<string, Type> _modelNames;

        /// <summary>
        /// Gets the metadata for the provided model type.
        /// </summary>
        /// <param name="type">Type of model to get metadata for.</param>
        /// <returns>Requested metadata, <c>null</c> if not found.</returns>
        public ModelMetadata GetModelMetadata(Type type)
        {
            ModelMetadata metadata;
            if (!_repository.TryGetValue(type, out metadata))
            {
                Type metadataType;
                if (_mapping.TryGetValue(type, out metadataType))
                {
                    metadata = (ModelMetadata)Activator.CreateInstance(metadataType);
                    _mapping = _mapping.Remove(type);
                    _repository = _repository.AddOrUpdate(type, metadata);
                }
            }
            return metadata;
        }

        /// <summary>
        /// Resolves a model type to a <see cref="Type"/>.
        /// </summary>
        /// <param name="modelName">Type of model to resolve to a <see cref="Type"/></param>
        /// <returns><see cref="Type"/> for the model, or <c>null</c> if not found.</returns>
        public Type GetModelType(string modelName)
        {
            Type type;
            _modelNames.TryGetValue(modelName.ToLower(), out type);
            return type;
        }

        /// <summary>
        /// Registers metadata for a model type.
        /// </summary>
        /// <param name="type">Type of model to register metadata for.</param>
        /// <param name="metadata">Type of metadata to register.</param>
        public void RegisterModelMetadata(Type type, Type metadataType)
        {
            if (!typeof(ModelMetadata).IsAssignableFrom(metadataType))
                throw new InvalidOperationException(metadataType.Name + " does not inherit from ModelMetadata");

            _mapping = _mapping.AddOrUpdate(type, metadataType);
            _modelNames = _modelNames.AddOrUpdate(type.Name.ToLower(), type);
        }
    }
}
