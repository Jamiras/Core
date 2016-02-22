using System;
using Jamiras.Components;
using Jamiras.Database;
using Jamiras.DataModels.Metadata;

namespace Jamiras.DataModels
{
    public class DatabaseDataModelSource : DataModelSourceBase
    {
        public DatabaseDataModelSource(IDataModelMetadataRepository metadataRepository, IDatabase database)
            : base(metadataRepository, Logger.GetLogger("DatabaseDataModelSource"))
        {
            _database = database;
        }

        private readonly IDatabase _database;
        
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
