using System;
using System.Collections.Generic;
using Jamiras.Components;
using Jamiras.DataModels.Metadata;

namespace Jamiras.DataModels
{
    /// <summary>
    /// <see cref="IDataModelSource"/> for models stored in memory.
    /// </summary>
    public class MemoryDataModelSource : DataModelSourceBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseDataModelSource"/> class.
        /// </summary>
        /// <param name="metadataRepository">The repository containing metadata describing the models.</param>
        public MemoryDataModelSource(IDataModelMetadataRepository metadataRepository)
            : base(metadataRepository, Logger.GetLogger("MemoryDataModelSource"))
        {
        }

        private static int _nextKey = 100001;

        /// <summary>
        /// Stores a model in the cache.
        /// </summary>
        /// <typeparam name="T">Type of data model to cache.</typeparam>
        /// <param name="model">Model to cache.</param>
        public void Cache<T>(T model) 
            where T : DataModelBase
        {
            var metadata = GetModelMetadata(typeof(T));
            var id = metadata.GetKey(model);
            Cache<T>(id, model);
        }

        /// <summary>
        /// Gets the keys of all models of the specified type stored in the cache.
        /// </summary>
        /// <typeparam name="T">Type of data model to query.</typeparam>
        public IEnumerable<int> GetKeys<T>()
        {
            return GetCacheKeys<T>();
        }

        /// <summary>
        /// Gets a non-shared instance of a data model.
        /// </summary>
        /// <typeparam name="T">Type of data model to retrieve.</typeparam>
        /// <param name="searchData">Filter data used to populate the data model.</param>
        /// <param name="metadata">Metadata about the model.</param>
        /// <returns>
        /// Populated data model, <c>null</c> if not found.
        /// </returns>
        /// <exception cref="NotImplementedException">non-int query not supported</exception>
        protected override T Query<T>(object searchData, ModelMetadata metadata)
        {
            if (!(searchData is int))
                throw new NotImplementedException("non-int query not supported");

            T model = Get<T>((int)searchData);
            if (model == null)
                return null;

            T copy = new T();
            foreach (var propertyKey in model.PropertyKeys)
            {
                var property = ModelProperty.GetPropertyForKey(propertyKey);
                var value = model.GetOriginalValue(property);
                copy.SetValueCore(property, value);
            }

            return copy;
        }

        /// <summary>
        /// Gets a non-shared instance of a collection of data models.
        /// </summary>
        /// <param name="collectionType">Type of the collection.</param>
        /// <param name="searchData">Filter data used to populate the data model.</param>
        /// <param name="results">A collection to hold the results.</param>
        /// <param name="maxResults">The maximum number of results to return.</param>
        /// <param name="resultType">Type of the result items.</param>
        /// <returns>
        ///   <c>true</c> if results were found, <c>false</c> if not.
        /// </returns>
        protected override bool Query(Type collectionType, object searchData, ICollection<DataModelBase> results, int maxResults, Type resultType)
        {
            return true;
        }

        /// <summary>
        /// Commits a single model.
        /// </summary>
        protected override bool Commit(DataModelBase dataModel, ModelMetadata metadata)
        {
            if (metadata.GetKey(dataModel) < 0)
                dataModel.SetValue(metadata.PrimaryKeyProperty, _nextKey++);

            return true;
        }
    }
}
