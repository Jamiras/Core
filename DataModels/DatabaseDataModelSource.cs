using System;
using Jamiras.Components;
using Jamiras.Database;
using Jamiras.DataModels.Metadata;

namespace Jamiras.DataModels
{
    /// <summary>
    /// <see cref="IDataModelSource"/> for models stored in a database.
    /// </summary>
    public class DatabaseDataModelSource : DataModelSourceBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseDataModelSource"/> class.
        /// </summary>
        /// <param name="metadataRepository">The repository containing metadata describing the models.</param>
        /// <param name="database">The database where the models are stored.</param>
        public DatabaseDataModelSource(IDataModelMetadataRepository metadataRepository, IDatabase database)
            : base(metadataRepository, Logger.GetLogger("DatabaseDataModelSource"))
        {
            _database = database;
        }

        private readonly IDatabase _database;

        /// <summary>
        /// Gets a non-shared instance of a data model.
        /// </summary>
        /// <typeparam name="T">Type of data model to retrieve.</typeparam>
        /// <param name="searchData">Filter data used to populate the data model.</param>
        /// <param name="metadata">Metadata about the model.</param>
        /// <returns>
        /// Populated data model, <c>null</c> if not found.
        /// </returns>
        /// <exception cref="ArgumentException">Metadata registered for " + typeof(T).FullName + " does not implement IDatabaseModelMetadata</exception>
        protected override T Query<T>(object searchData, ModelMetadata metadata)
        {
            var databaseModelMetadata = metadata as IDatabaseModelMetadata;
            if (databaseModelMetadata == null)
                throw new ArgumentException("Metadata registered for " + typeof(T).FullName + " does not implement IDatabaseModelMetadata");

            var model = new T();
            if (!databaseModelMetadata.Query(model, searchData, _database))
                return null;

            return model;
        }

        internal override T Query<T>(object searchData, int maxResults, IDataModelCollectionMetadata collectionMetadata)
        {
            var model = new T();
            if (!collectionMetadata.Query(model, maxResults, searchData, _database))
                return null;

            return model;
        }

        /// <summary>
        /// Initializes a new record.
        /// </summary>
        /// <param name="model">The newly created model object.</param>
        /// <param name="metadata">Metadata about the model.</param>
        protected override void InitializeNewRecord(DataModelBase model, ModelMetadata metadata)
        {
            base.InitializeNewRecord(model, metadata);

            var databaseMetadata = metadata as IDatabaseModelMetadata;
            if (databaseMetadata != null)
                databaseMetadata.InitializeNewRecord(model, _database);
        }

        /// <summary>
        /// Commits a single model.
        /// </summary>
        protected override bool Commit(DataModelBase dataModel, ModelMetadata metadata)
        {
            var databaseMetadata = metadata as IDatabaseModelMetadata;
            return databaseMetadata.Commit(dataModel, _database);
        }
    }
}
