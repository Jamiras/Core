using System;
using Jamiras.Components;
using Jamiras.Database;
using Jamiras.DataModels.Metadata;
using System.Collections;
using System.Collections.Generic;

namespace Jamiras.DataModels
{
    public class DatabaseDataModelSource : DataModelSourceBase
    {
        public DatabaseDataModelSource(IDataModelMetadataRepository metadataRepository, IDatabase database)
            : base(metadataRepository)
        {
            _database = database;
        }

        private readonly IDatabase _database;
        private readonly ILogger _logger = Logger.GetLogger("DatabaseDataModelSource");

        
        protected override T Query<T>(object searchData, ModelMetadata metadata)
        {
            var databaseModelMetadata = metadata as IDatabaseModelMetadata;
            if (databaseModelMetadata == null)
                throw new ArgumentException("Metadata registered for " + typeof(T).FullName + " does not implement IDatabaseModelMetadata");

            _logger.Write("Querying {0}({1})", typeof(T).Name, searchData);

            var model = new T();
            if (!databaseModelMetadata.Query(model, searchData, _database))
            {
                _logger.WriteVerbose("{0}({1}) not found", typeof(T).Name, searchData);
                return null;
            }

            return model;
        }

        internal override T Query<T>(object searchData, int maxResults, IDataModelCollectionMetadata collectionMetadata)
        {
            _logger.Write("Querying {0}({1}) limit {2}", typeof(T).Name, searchData, maxResults);

            var model = new T();
            if (!collectionMetadata.Query(model, maxResults, searchData, _database))
            {
                _logger.WriteVerbose("{0}({1}) not found", typeof(T).Name, searchData);
                return null;
            }

            var collection = model as IDataModelCollection;
            if (collection != null)
                _logger.Write("Returning {0} {1}", collection.Count, typeof(T).Name);

            return model;
        }

        /// <summary>
        /// Creates a new data model instance.
        /// </summary>
        /// <typeparam name="T">Type of data model to create.</typeparam>
        /// <returns>New instance initialized with default values.</returns>
        public override T Create<T>()
        {
            var metadata = GetModelMetadata(typeof(T)) as IDatabaseModelMetadata;
            if (metadata == null)
                throw new ArgumentException("No metadata registered for " + typeof(T).FullName);

            var collectionMetadata = metadata as IDataModelCollectionMetadata;
            if (collectionMetadata != null)
                throw new ArgumentException("Cannot create new collection for " + typeof(T).FullName + ". Use Query<> method.");

            _logger.Write("Creating {0}", typeof(T).Name);

            var model = new T();
            metadata.InitializeNewRecord(model, _database);
            int id = metadata.GetKey(model);
            if (id != 0)
                model = TryCache(id, model);
            return model;
        }

        /// <summary>
        /// Commits a single model.
        /// </summary>
        protected override bool Commit(DataModelBase dataModel, ModelMetadata metadata)
        {
            var databaseMetadata = metadata as IDatabaseModelMetadata;

            var key = databaseMetadata.GetKey(dataModel);
            _logger.Write("Committing {0}({1})", dataModel.GetType().Name, key);
            if (!databaseMetadata.Commit(dataModel, _database))
            {
                _logger.WriteWarning("Commit failed {0}({1})", dataModel.GetType().Name, key);
                return false;
            }

            var newKey = databaseMetadata.GetKey(dataModel);
            if (key != newKey)
            {
                _logger.WriteVerbose("New key for {0}:{1}", dataModel.GetType().Name, newKey);

                ExpireCollections(dataModel.GetType());

                var fieldMetadata = metadata.GetFieldMetadata(databaseMetadata.PrimaryKeyProperty);
                UpdateKeys(fieldMetadata, key, newKey);
            }

            return true;
        }

        /// <summary>
        /// Commits a collection of models.
        /// </summary>
        protected override bool Commit(IEnumerable collection, ModelMetadata itemMetadata)
        {
            var modelMetadata = itemMetadata as IDatabaseModelMetadata;
            if (modelMetadata != null)
            {
                if (modelMetadata.PrimaryKeyProperty == null)
                {
                    _logger.Write("Commit aborted, no primary key for collection");
                    return true;
                }

                _logger.Write("Committing {0}", collection.GetType().Name);

                foreach (DataModelBase model in collection)
                {
                    if (model.IsModified && !Commit(model, itemMetadata))
                        return false;

                    model.AcceptChanges();
                }
            }
            else
            {
                _logger.Write("Committing {0}", collection.GetType().Name);

                return base.Commit(collection, itemMetadata);
            }

            return true;
        }
    }
}
