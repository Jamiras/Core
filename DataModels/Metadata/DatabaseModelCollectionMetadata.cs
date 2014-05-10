using System;
using System.ComponentModel;
using Jamiras.Components;
using Jamiras.Database;

namespace Jamiras.DataModels.Metadata
{
    public class DatabaseModelCollectionMetadata<T> : DatabaseModelMetadata
        where T : DataModelBase, new()
    {
        public DatabaseModelCollectionMetadata()
        {
            var metadataRepository = ServiceRepository.Instance.FindService<IDataModelMetadataRepository>();
            _relatedMetadata = (DatabaseModelMetadata)metadataRepository.GetModelMetadata(typeof(T));
        }

        private DatabaseModelMetadata _relatedMetadata;
        private string _queryString;
        private int _primaryKeyIndex;

        [EditorBrowsable(EditorBrowsableState.Never)]
        protected override sealed void RegisterFieldMetadata(ModelProperty property, FieldMetadata metadata)
        {
            throw new NotSupportedException();
        }

        public override bool Query(ModelBase model, object primaryKey, IDatabase database)
        {
            if (_queryString == null)
                _queryString = BuildQueryString();

            var databaseDataModelSource = ServiceRepository.Instance.FindService<IDataModelSource>() as DatabaseDataModelSource;
            var collection = (DataModelCollection<T>)model;

            using (var query = database.PrepareQuery(_queryString))
            {
                query.Bind("@filterValue", primaryKey);

                while (query.FetchRow())
                {
                    T item;
                    int id = query.GetInt32(_primaryKeyIndex);
                    if (databaseDataModelSource != null)
                    {
                        item = databaseDataModelSource.TryGet<T>(id);
                        if (item != null)
                        {
                            collection.AddCore(item);
                            continue;
                        }
                    }

                    item = new T();
                    _relatedMetadata.PopulateItem(item, query);

                    if (databaseDataModelSource != null)
                        item = databaseDataModelSource.TryCache<T>(item);

                    collection.AddCore(item);
                }
            }

            return true;
        }

        private string BuildQueryString()
        {
            var queryExpression = _relatedMetadata.BuildQueryExpression();

            _primaryKeyIndex = 0;
            if (_relatedMetadata.PrimaryKeyProperty != null)
            {
                var primaryKeyFieldName = _relatedMetadata.GetFieldMetadata(_relatedMetadata.PrimaryKeyProperty).FieldName;

                int index = 0;
                foreach (var metadata in _relatedMetadata.AllFieldMetadata.Values)
                {
                    if (primaryKeyFieldName == metadata.FieldName)
                    {
                        _primaryKeyIndex = index;
                        break;
                    }

                    index++;
                }
            }

            CustomizeQuery(queryExpression);

            return queryExpression.BuildQueryString();
        }

        protected virtual void CustomizeQuery(ModelQueryExpression queryExpression)
        {
        }
    }
}
