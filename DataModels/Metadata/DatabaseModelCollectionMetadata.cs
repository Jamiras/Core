using System;
using System.Collections.Generic;
using System.ComponentModel;
using Jamiras.Components;
using Jamiras.Database;

namespace Jamiras.DataModels.Metadata
{
    /// <summary>
    /// Metadata for a collection of database-based models.
    /// </summary>
    /// <typeparam name="TCollection">The type of models in the collection.</typeparam>
    /// <typeparam name="TModel">The type of models populated by the query (<typeparamref name="TCollection"/> or subclass of <typeparamref name="TCollection"/>).</typeparam>
    public class DatabaseModelCollectionMetadata<TCollection, TModel> : ModelMetadata, IDataModelCollectionMetadata, IDatabaseModelMetadata
        where TCollection : DataModelBase
        where TModel : TCollection, new()
    {
        public DatabaseModelCollectionMetadata()
        {
            var metadataRepository = ServiceRepository.Instance.FindService<IDataModelMetadataRepository>();
            RelatedMetadata = (DatabaseModelMetadata)metadataRepository.GetModelMetadata(typeof(TModel));
            PrimaryKeyProperty = CollectionFilterKeyProperty;
        }

        private string _queryString;
        private int _primaryKeyIndex;

        /// <summary>
        /// Gets the token to use when setting a filter value to the query key.
        /// </summary>
        protected const string FilterValueToken = "@filterValue";

        /// <summary>
        /// Gets the metadata for the items contained in the collection.
        /// </summary>
        protected DatabaseModelMetadata RelatedMetadata { get; private set; }

        ModelMetadata IDataModelCollectionMetadata.ModelMetadata
        {
            get { return RelatedMetadata; }
        }

        /// <summary>
        /// Gets whether or not the collection can be modified and committed after it has been fetched.
        /// </summary>
        public bool AreResultsReadOnly { get; protected set; }

        ModelProperty IDatabaseModelMetadata.PrimaryKeyProperty
        {
            get { return CollectionFilterKeyProperty; }
        }

        ModelProperty IDataModelCollectionMetadata.CollectionFilterKeyProperty
        {
            get { return CollectionFilterKeyProperty; }
        }

        private static readonly ModelProperty CollectionFilterKeyProperty = 
            ModelProperty.Register(typeof(DataModelBase), null, typeof(int), 0);

        /// <summary>
        /// Gets the primary key value of a model.
        /// </summary>
        /// <param name="model">The model to get the primary key for.</param>
        /// <returns>The primary key of the model.</returns>
        int IDatabaseModelMetadata.GetKey(ModelBase model)
        {
            return (int)model.GetValue(CollectionFilterKeyProperty);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        protected override sealed void RegisterFieldMetadata(ModelProperty property, FieldMetadata metadata)
        {
            throw new NotSupportedException();
        }

        FieldMetadata IDatabaseModelMetadata.GetFieldMetadata(ModelProperty property)
        {
            return null;
        }

        void IDatabaseModelMetadata.InitializeNewRecord(ModelBase model, IDatabase database)
        {
        }

        /// <summary>
        /// Populates a model from a database.
        /// </summary>
        /// <param name="model">The uninitialized model to populate.</param>
        /// <param name="primaryKey">The primary key of the model to populate.</param>
        /// <param name="database">The database to populate from.</param>
        /// <returns><c>true</c> if the model was populated, <c>false</c> if not.</returns>
        bool IDatabaseModelMetadata.Query(ModelBase model, object primaryKey, IDatabase database)
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

            if (!Query((ICollection<TCollection>)model, maxResults, primaryKey, database))
                return false;

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
        protected virtual bool Query(ICollection<TCollection> models, int maxResults, object primaryKey, IDatabase database)
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
                        TModel item = new TModel();
                        RelatedMetadata.PopulateItem(item, database, query);
                        models.Add(item);

                        if (--maxResults == 0)
                            break;
                    }
                }
                else
                {
                    while (query.FetchRow())
                    {
                        TModel item;
                        int id = query.GetInt32(_primaryKeyIndex);
                        if (databaseDataModelSource != null)
                        {
                            item = databaseDataModelSource.TryGet<TModel>(id);
                            if (item != null)
                            {
                                if (!models.Contains(item))
                                {
                                    models.Add(item);
                                    if (--maxResults == 0)
                                        break;                                    
                                }

                                continue;
                            }
                        }

                        item = new TModel();
                        RelatedMetadata.PopulateItem(item, database, query);

                        if (databaseDataModelSource != null)
                            item = databaseDataModelSource.TryCache<TModel>(id, item);

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
            var queryExpression = RelatedMetadata.BuildQueryExpression(database);

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
        /// Allows a subclass to modify the generated query before it is executed.
        /// </summary>
        protected virtual void CustomizeQuery(QueryBuilder query)
        {
        }

        /// <summary>
        /// Commits changes made to a model to a database.
        /// </summary>
        /// <param name="model">The model to commit.</param>
        /// <param name="database">The database to commit to.</param>
        /// <returns><c>true</c> if the model was committed, <c>false</c> if not.</returns>
        public bool Commit(ModelBase model, IDatabase database)
        {
            if (AreResultsReadOnly)
                return false;

            object value = model.GetValue(CollectionFilterKeyProperty);
            int parentRecordKey = (value != null) ? (int)value : 0;
            return UpdateRows((ICollection<TCollection>)model, parentRecordKey, database);
        }

        /// <summary>
        /// Updates rows in the database for a collection of model instances.
        /// </summary>
        /// <param name="models">The models to commit.</param>
        /// <param name="parentRecordKey">The primary key of the associated parent record.</param>
        /// <param name="database">The database to commit to.</param>
        /// <returns><c>true</c> if the models were committed, <c>false</c> if not.</returns>
        protected virtual bool UpdateRows(ICollection<TCollection> models, int parentRecordKey, IDatabase database)
        {
            return true;
        }
    }

    public class DatabaseModelCollectionMetadata<T> : DatabaseModelCollectionMetadata<T, T>
        where T : DataModelBase, new()
    {
    }
}
