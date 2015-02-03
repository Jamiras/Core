using System;
using System.ComponentModel;
using Jamiras.Components;
using Jamiras.Database;

namespace Jamiras.DataModels.Metadata
{
    public class DatabaseModelCollectionMetadata<T> : DatabaseModelMetadata, IDataModelCollectionMetadata
        where T : DataModelBase, new()
    {
        public DatabaseModelCollectionMetadata()
        {
            var metadataRepository = ServiceRepository.Instance.FindService<IDataModelMetadataRepository>();
            RelatedMetadata = (DatabaseModelMetadata)metadataRepository.GetModelMetadata(typeof(T));
        }

        protected DatabaseModelMetadata RelatedMetadata { get; private set; }

        ModelMetadata IDataModelCollectionMetadata.ModelMetadata
        {
            get { return RelatedMetadata; }
        }

        private string _queryString;
        private int _primaryKeyIndex;

        ModelProperty IDataModelCollectionMetadata.CollectionFilterKeyProperty
        {
            get { return CollectionFilterKeyProperty; }
        }

        private static readonly ModelProperty CollectionFilterKeyProperty = 
            ModelProperty.Register(typeof(DataModelBase), null, typeof(int), 0);

        [EditorBrowsable(EditorBrowsableState.Never)]
        protected override sealed void RegisterFieldMetadata(ModelProperty property, FieldMetadata metadata)
        {
            throw new NotSupportedException();
        }

        public override int GetKey(ModelBase model)
        {
            return (int)model.GetValue(CollectionFilterKeyProperty);
        }

        public override sealed bool Query(ModelBase model, object primaryKey, IDatabase database)
        {
            return Query(model, Int32.MaxValue, primaryKey, database);
        }

        public virtual bool Query(ModelBase model, int maxResults, object primaryKey, IDatabase database)
        {
            if (_queryString == null)
                _queryString = BuildQueryString(database);

            var databaseDataModelSource = ServiceRepository.Instance.FindService<IDataModelSource>() as DatabaseDataModelSource;
            var collection = (IDataModelCollection)model;

            if (primaryKey is int)
                model.SetValue(CollectionFilterKeyProperty, (int)primaryKey);

            using (var query = database.PrepareQuery(_queryString))
            {
                query.Bind(FilterValueToken, primaryKey);

                if (_primaryKeyIndex == -1)
                {
                    while (query.FetchRow())
                    {
                        T item = new T();
                        RelatedMetadata.PopulateItem(item, query);
                        InitializeExistingRecord(item);
                        collection.Add(item);

                        if (--maxResults == 0)
                            break;
                    }
                }
                else
                {
                    while (query.FetchRow())
                    {
                        T item;
                        int id = query.GetInt32(_primaryKeyIndex);
                        if (databaseDataModelSource != null)
                        {
                            item = databaseDataModelSource.TryGet<T>(id);
                            if (item != null)
                            {
                                if (!collection.Contains(item))
                                    collection.Add(item);
                                continue;
                            }
                        }

                        item = new T();
                        RelatedMetadata.PopulateItem(item, query);
                        InitializeExistingRecord(item);

                        if (databaseDataModelSource != null)
                            item = databaseDataModelSource.TryCache<T>(id, item);

                        collection.Add(item);

                        if (--maxResults == 0)
                            break;
                    }
                }
            }

            return true;
        }

        private string BuildQueryString(IDatabase database)
        {
            var queryExpression = RelatedMetadata.BuildQueryExpression();

            _primaryKeyIndex = -1;
            if (RelatedMetadata.PrimaryKeyProperty != null)
            {
                var primaryKeyFieldName = RelatedMetadata.GetFieldMetadata(RelatedMetadata.PrimaryKeyProperty).FieldName;

                int index = 0;
                foreach (var metadata in RelatedMetadata.AllFieldMetadata.Values)
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

            return database.BuildQueryString(queryExpression);
        }

        protected override bool UpdateRows(ModelBase model, IDatabase database)
        {
            var collection = (IDataModelCollection)model;
            var enumerator = collection.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (!RelatedMetadata.Commit((ModelBase)enumerator.Current, database))
                    return false;
            }

            return true;
        }
    }
}
