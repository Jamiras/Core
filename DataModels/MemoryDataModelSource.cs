using System;
using System.Collections.Generic;
using Jamiras.Components;
using Jamiras.DataModels.Metadata;

namespace Jamiras.DataModels
{
    public class MemoryDataModelSource : DataModelSourceBase
    {
        public MemoryDataModelSource(IDataModelMetadataRepository metadataRepository)
            : base(metadataRepository, Logger.GetLogger("MemoryDataModelSource"))
        {
        }

        private static int _nextKey = 100001;

        public void Cache<T>(T model) 
            where T : DataModelBase
        {
            var metadata = GetModelMetadata(typeof(T));
            var id = metadata.GetKey(model);
            Cache<T>(id, model);
        }

        public IEnumerable<int> GetKeys<T>()
        {
            return GetCacheKeys<T>();
        }

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

        protected override bool Query(Type collectionType, object searchData, ICollection<DataModelBase> results, int maxResults, Type resultType)
        {
            return true;
        }

        protected override bool Commit(DataModelBase dataModel, ModelMetadata metadata)
        {
            if (metadata.GetKey(dataModel) < 0)
                dataModel.SetValue(metadata.PrimaryKeyProperty, _nextKey++);

            return true;
        }
    }
}
