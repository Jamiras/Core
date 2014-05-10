using System;

namespace Jamiras.DataModels.Metadata
{
    /// <summary>
    /// Interface for repository that stores model metadata.
    /// </summary>
    public interface IDataModelMetadataRepository
    {
        /// <summary>
        /// Gets the metadata for the provided model type.
        /// </summary>
        /// <param name="type">Type of model to get metadata for.</param>
        /// <returns>Requested metadata, <c>null</c> if not found.</returns>
        ModelMetadata GetModelMetadata(Type type);

        /// <summary>
        /// Registers metadata for a model type.
        /// </summary>
        /// <param name="type">Type of model to register metadata for.</param>
        /// <param name="metadata">Type of metadata to register.</param>
        void RegisterModelMetadata(Type type, Type metadataType);
    }
}
