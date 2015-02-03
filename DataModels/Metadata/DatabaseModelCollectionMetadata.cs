using System;
using System.Collections.Generic;
using System.ComponentModel;
using Jamiras.Components;
using Jamiras.Database;

namespace Jamiras.DataModels.Metadata
{
    public class DatabaseModelCollectionMetadata<T> : DatabaseModelMetadata, IDataModelCollectionMetadata
        where T : DataModelBase, new()
    {
        public DatabaseModelCollectionMetadata()
            : this(typeof(T))
        {
        }

        public DatabaseModelCollectionMetadata(Type modelType)
        {
            var metadataRepository = ServiceRepository.Instance.FindService<IDataModelMetadataRepository>();
            RelatedMetadata = (DatabaseModelMetadata)metadataRepository.GetModelMetadata(modelType);
        }

        protected DatabaseModelMetadata RelatedMetadata { get; private set; }

        ModelMetadata IDataModelCollectionMetadata.ModelMetadata
        {
            get { return RelatedMetadata; }
        }

        private string _queryString;
        private int _primaryKeyIndex;

        public bool AreResultsReadOnly { get; protected set; }

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

        /// <summary>
        /// Gets the primary key value of a model.
        /// </summary>
        /// <param name="model">The model to get the primary key for.</param>
        /// <returns>The primary key of the model.</returns>
        public override sealed int GetKey(ModelBase model)
        {
            return (int)model.GetValue(CollectionFilterKeyProperty);
        }

        /// <summary>
        /// Populates a model from a database.
        /// </summary>
        /// <param name="model">The uninitialized model to populate.</param>
        /// <param name="primaryKey">The primary key of the model to populate.</param>
        /// <param name="database">The database to populate from.</param>
        /// <returns><c>true</c> if the model was populated, <c>false</c> if not.</returns>
        public override sealed bool Query(ModelBase model, object primaryKey, IDatabase database)
        {
            return Query(model, Int32.MaxValue, primaryKey, database);
        }

        /// <summary>
        /// Populates a collection with items from a database.
        /// </summary>
        /// <param name="model">The uninitialized model to populate.</param>
        /// <param name="maxResults">The maximum number of results to return</param>
        /// <param name="primaryKey">The primary key of the model to populate.</param>
        /// <param name="database">The database to populate from.</param>
        /// <returns><c>true</c> if the model was populated, <c>false</c> if not.</returns>
        public bool Query(ModelBase model, int maxResults, object primaryKey, IDatabase database)
        {
            if (primaryKey is int)
                model.SetValueCore(CollectionFilterKeyProperty, (int)primaryKey);

            if (!Query((ICollection<T>)model, Int32.MaxValue, primaryKey, database))
                return false;

            if (AreResultsReadOnly)
            {
                var collection = model as DataModelCollection<T>;
                if (collection != null)
                    collection.IsReadOnly = true;
            }

            return true;
        }

        /// <summary>
        /// Populates a collection with items from a database.
        /// </summary>
        /// <param name="models">The uninitialized collection to populate.</param>
        /// <param name="maxResults">The maximum number of results to return</param>
        /// <param name="primaryKey">The primary key of the model to populate.</param>
        /// <param name="database">The database to populate from.</param>
        /// <returns><c>true</c> if the model was populated, <c>false</c> if not.</returns>
        protected virtual bool Query(ICollection<T> models, int maxResults, object primaryKey, IDatabase database)
        {
            if (_queryString == null)
                _queryString = BuildQueryString(database);

            var databaseDataModelSource = ServiceRepository.Instance.FindService<IDataModelSource>() as DatabaseDataModelSource;

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
                        models.Add(item);

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
                                if (!models.Contains(item))
                                    models.Add(item);
                                continue;
                            }
                        }

                        item = new T();
                        RelatedMetadata.PopulateItem(item, query);
                        InitializeExistingRecord(item);

                        if (databaseDataModelSource != null)
                            item = databaseDataModelSource.TryCache<T>(id, item);

                        models.Add(item);

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

        /// <summary>
        /// Creates rows in the database for a new model instance.
        /// </summary>
        /// <param name="model">The model to commit.</param>
        /// <param name="database">The database to commit to.</param>
        /// <returns><c>true</c> if the model was committed, <c>false</c> if not.</returns>
        protected override sealed bool CreateRows(ModelBase model, IDatabase database)
        {
            return UpdateRows(model, database);
        }

        /// <summary>
        /// Updates rows in the database for an existing model instance.
        /// </summary>
        /// <param name="model">The model to commit.</param>
        /// <param name="database">The database to commit to.</param>
        /// <returns><c>true</c> if the model was committed, <c>false</c> if not.</returns>
        protected override sealed bool UpdateRows(ModelBase model, IDatabase database)
        {
            if (AreResultsReadOnly)
                return false;

            object value = model.GetValue(CollectionFilterKeyProperty);
            int parentRecordKey = (value != null) ? (int)value : 0;
            return UpdateRows((ICollection<T>)model, parentRecordKey, database);
        }

        /// <summary>
        /// Updates rows in the database for a collection of model instances.
        /// </summary>
        /// <param name="models">The models to commit.</param>
        /// <param name="parentRecordKey">The primary key of the associated parent record.</param>
        /// <param name="database">The database to commit to.</param>
        /// <returns><c>true</c> if the models were committed, <c>false</c> if not.</returns>
        protected virtual bool UpdateRows(ICollection<T> models, int parentRecordKey, IDatabase database)
        {
            foreach (var model in models)
            {
                if (!RelatedMetadata.Commit(model, database))
                    return false;
            }                

            return true;
        }
    }
}
