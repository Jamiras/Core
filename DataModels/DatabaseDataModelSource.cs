using System;
using System.Collections.Generic;
using System.Linq;
using Jamiras.Database;
using Jamiras.DataModels.Metadata;
using System.Collections;
using System.Diagnostics;

namespace Jamiras.DataModels
{
    public class DatabaseDataModelSource : IDataModelSource
    {
        public DatabaseDataModelSource(IDataModelMetadataRepository metadataRepository, IDatabase database)
        {
            _metadataRepository = metadataRepository;
            _database = database;
            _items = new Dictionary<Type, DataModelCache>();
        }

        private readonly IDataModelMetadataRepository _metadataRepository;
        private readonly IDatabase _database;
        private readonly Dictionary<Type, DataModelCache> _items;

        [DebuggerTypeProxy(typeof(DataModelCacheDebugView))]
        private class DataModelCache
        {
            public DataModelCache()
            {
                _cache = new List<KeyValuePair<int, WeakReference>>();
            }

            public override string ToString()
            {
                return "Count = " + _cache.Count;
            }

            internal sealed class DataModelCacheDebugView
            {
                public DataModelCacheDebugView(DataModelCache cache)
                {
                    _cache = cache;
                }

                private readonly DataModelCache _cache;

                [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
                public KeyValuePair<int, DataModelBase>[] Items
                {
                    get
                    {
                        var list = new List<KeyValuePair<int, DataModelBase>>();
                        foreach (var item in _cache._cache)
                        {
                            if (item.Value.IsAlive)
                                list.Add(new KeyValuePair<int, DataModelBase>(item.Key, (DataModelBase)item.Value.Target));
                        }

                        return list.ToArray();
                    }
                }
            }

            private readonly List<KeyValuePair<int, WeakReference>> _cache;

            public IEnumerable<DataModelBase> Models
            {
                get
                {
                    foreach (var item in _cache)
                    {
                        var model = item.Value.Target as DataModelBase;
                        if (model != null && item.Value.IsAlive)
                            yield return model;
                    }
                }
            }
    
            public DataModelBase TryGet(int id)
            {
                lock (_cache)
                {
                    int idx;
                    return Find(id, out idx);
                }
            }

            public DataModelBase TryCache(int id, DataModelBase model)
            {
                lock (_cache)
                {
                    int idx;
                    DataModelBase cached = Find(id, out idx);
                    if (cached != null)
                        return cached;

                    _cache.Insert(idx, new KeyValuePair<int, WeakReference>(id, new WeakReference(model)));
                    return model;
                }
            }

            public void UpdateKey(int id, int newId)
            {
                lock (_cache)
                {
                    int idx;
                    var model = Find(id, out idx);
                    if (model != null)
                    {
                        _cache.RemoveAt(idx);

                        if (Find(newId, out idx) == null)
                            _cache.Insert(idx, new KeyValuePair<int, WeakReference>(newId, new WeakReference(model)));
                    }
                }
            }

            private DataModelBase Find(int id, out int insertIndex)
            {
                int low = 0;
                int high = _cache.Count - 1;

                while (low <= high)
                {
                    int mid = (low + high) / 2;
                    var item = _cache[mid].Value.Target as DataModelBase;
                    if (item == null)
                    {
                        _cache.RemoveAt(mid);
                        high--;
                        continue;
                    }

                    int itemId = _cache[mid].Key;
                    if (itemId == id)
                    {
                        insertIndex = mid;
                        return item;
                    }

                    if (itemId > id)
                        high = mid - 1;
                    else
                        low = mid + 1;
                }

                insertIndex = low;
                return null;
            }

            public void ExpireCollections(Type modelType)
            {
                bool expire = false;

                lock (_cache)
                {
                    foreach (var item in _cache)
                    {
                        var collection = item.Value.Target as IDataModelCollection;
                        if (collection != null)
                        {
                            expire = collection.ModelType.IsAssignableFrom(modelType);
                            break;
                        }
                        else if (item.Value.Target != null)
                        {
                            break;
                        }
                    }

                    if (expire)
                        _cache.Clear();
                }
            }
        }

        /// <summary>
        /// Gets the shared instance of a data model.
        /// </summary>
        /// <typeparam name="T">Type of data model to retrieve.</typeparam>
        /// <param name="id">Unique identifier of model to retrieve.</param>
        /// <returns>Requested model, <c>null</c> if not found.</returns>
        public T Get<T>(int id)
            where T : DataModelBase, new()
        {
            var metadata = _metadataRepository.GetModelMetadata(typeof(T)) as DatabaseModelMetadata;
            if (metadata == null)
                throw new ArgumentException("No metadata registered for " + typeof(T).FullName);

            T item;

            DataModelCache cache;
            if (_items.TryGetValue(typeof(T), out cache))
            {
                item = cache.TryGet(id) as T;
                if (item != null)
                    return item;
            }
            else
            {
                cache = new DataModelCache();
                _items[typeof(T)] = cache;
            }

            item = GetCopy<T>(id);
            if (item == null)
                return null;

            item = (T)cache.TryCache(id, item);
            return item;
        }

        internal T TryGet<T>(int id)
            where T : DataModelBase
        {
            DataModelCache cache;
            if (!_items.TryGetValue(typeof(T), out cache))
                return null;

            return cache.TryGet(id) as T;
        }

        internal T TryCache<T>(int id, T item)
            where T : DataModelBase
        {
            DataModelCache cache;
            if (!_items.TryGetValue(typeof(T), out cache))
            {
                cache = new DataModelCache();
                _items[typeof(T)] = cache;
            }

            return (T)cache.TryCache(id, item);
        }

        /// <summary>
        /// Gets a non-shared instance of a data model.
        /// </summary>
        /// <typeparam name="T">Type of data model to retrieve.</typeparam>
        /// <param name="id">Unique identifier of model to retrieve.</param>
        /// <returns>Copy of requested model, <c>null</c> if not found.</returns>
        public T GetCopy<T>(int id)
            where T : DataModelBase, new()
        {
            return Query<T>(id);
        }

        /// <summary>
        /// Gets a non-shared instance of a data model.
        /// </summary>
        /// <typeparam name="T">Type of data model to retrieve.</typeparam>
        /// <param name="searchData">Filter data used to populate the data model.</param>
        /// <returns>Populated data model, <c>null</c> if not found.</returns>
        public T Query<T>(object searchData)
            where T : DataModelBase, new()
        {
            var metadata = _metadataRepository.GetModelMetadata(typeof(T)) as DatabaseModelMetadata;
            if (metadata == null)
                throw new ArgumentException("No metadata registered for " + typeof(T).FullName);

            var model = new T();
            if (!metadata.Query(model, searchData, _database))
                return null;

            return model;
        }

        /// <summary>
        /// Gets a non-shared instance of a data model.
        /// </summary>
        /// <typeparam name="T">Type of data model to retrieve.</typeparam>
        /// <param name="searchData">Filter data used to populate the data model.</param>
        /// <param name="maxResults">The maximum number of results to return.</param>
        /// <returns>Populated data model, <c>null</c> if not found.</returns>
        public T Query<T>(object searchData, int maxResults)
            where T : DataModelBase, new()
        {
            var metadata = _metadataRepository.GetModelMetadata(typeof(T)) as DatabaseModelMetadata;
            if (metadata == null)
                throw new ArgumentException("No metadata registered for " + typeof(T).FullName);

            var collectionMetadata = metadata as IDataModelCollectionMetadata;
            if (collectionMetadata == null)
                throw new ArgumentException(typeof(T).FullName + " is not registered to a collection metadata");

            var model = new T();
            if (!collectionMetadata.Query(model, maxResults, searchData, _database))
                return null;

            return model;
        }

        /// <summary>
        /// Creates a new data model instance.
        /// </summary>
        /// <typeparam name="T">Type of data model to create.</typeparam>
        /// <returns>New instance initialized with default values.</returns>
        public T Create<T>()
            where T : DataModelBase, new()
        {
            var metadata = _metadataRepository.GetModelMetadata(typeof(T)) as DatabaseModelMetadata;
            if (metadata == null)
                throw new ArgumentException("No metadata registered for " + typeof(T).FullName);

            var model = new T();
            metadata.InitializeNewRecord(model);
            int id = metadata.GetKey(model);
            if (id != 0)
                model = TryCache<T>(id, model);
            return model;
        }

        /// <summary>
        /// Commits changes made to a data model. The shared model and any future copies will contain committed changes.
        /// </summary>
        /// <param name="dataModel">Data model to commit.</param>
        /// <returns><c>true</c> if the changes were committed, <c>false</c> if not.</returns>
        public bool Commit(DataModelBase dataModel)
        {
            if (!dataModel.IsModified && !(dataModel is IDataModelCollection))
                return true;

            var metadata = _metadataRepository.GetModelMetadata(dataModel.GetType()) as DatabaseModelMetadata;
            if (metadata == null)
                return false;

            return Commit(dataModel, metadata);
        }

        private bool Commit(DataModelBase dataModel, DatabaseModelMetadata metadata)
        {
            var collection = dataModel as IDataModelCollection;
            if (collection != null && !Commit(collection, metadata as IDataModelCollectionMetadata)) 
                return false;

            var key = metadata.GetKey(dataModel);
            if (!metadata.Commit(dataModel, _database))
                return false;

            var newKey = metadata.GetKey(dataModel);
            if (key != newKey)
            {
                ExpireCollections(dataModel.GetType());

                var fieldMetadata = metadata.GetFieldMetadata(metadata.PrimaryKeyProperty);
                UpdateKeys(fieldMetadata, key, newKey);
            }

            dataModel.AcceptChanges();
            return true;
        }

        private bool Commit(IDataModelCollection collection, IDataModelCollectionMetadata collectionMetadata)
        {
            var modelMetadata = (collectionMetadata != null) ? collectionMetadata.ModelMetadata as DatabaseModelMetadata : null;
            if (modelMetadata != null)
            {
                if (modelMetadata.PrimaryKeyProperty == null)
                    return true;

                foreach (DataModelBase model in collection)
                {
                    if (model.IsModified && !Commit(model, modelMetadata))
                        return false;
                }
            }
            else
            {
                foreach (DataModelBase model in collection)
                {
                    if (!Commit(model))
                        return false;
                }
            }

            return true;
        }

        private void ExpireCollections(Type modelType)
        {
            foreach (var kvp in _items)
            {
                if (typeof(IDataModelCollection).IsAssignableFrom(kvp.Key))
                    kvp.Value.ExpireCollections(modelType);
            }
        }

        private void UpdateKeys(FieldMetadata fieldMetadata, int key, int newKey)
        {
            foreach (var kvp in _items)
            {
                kvp.Value.UpdateKey(key, newKey);

                if (!kvp.Value.Models.Any())
                    continue;

                var modelMetadata = _metadataRepository.GetModelMetadata(kvp.Key) as DatabaseModelMetadata;
                var collectionMetadata = modelMetadata as IDataModelCollectionMetadata;
                if (collectionMetadata != null)
                {
                    modelMetadata = collectionMetadata.ModelMetadata as DatabaseModelMetadata;
                    UpdateKeys(kvp.Value.Models, collectionMetadata.CollectionFilterKeyProperty, key, newKey);
                }

                if (modelMetadata == null)
                    continue;

                var dependantProperty = GetDependantProperty(modelMetadata, fieldMetadata.FieldName);
                if (dependantProperty != null && dependantProperty.PropertyType == typeof(int))
                {
                    if (collectionMetadata != null)
                    {
                        var firstModel = kvp.Value.Models.First();
                        if (firstModel is IDataModelCollection)
                        {
                            foreach (IDataModelCollection collection in kvp.Value.Models)
                                UpdateKeys(collection, dependantProperty, key, newKey);
                        }
                        else
                        {
                            UpdateKeys(kvp.Value.Models, dependantProperty, key, newKey);
                        }
                    }
                    else
                    {
                        UpdateKeys(kvp.Value.Models, dependantProperty, key, newKey);
                    }
                }
            }
        }

        private ModelProperty GetDependantProperty(ModelMetadata modelMetadata, string fieldName)
        {
            foreach (var kvp in modelMetadata.AllFieldMetadata)
            {
                if (kvp.Value.FieldName == fieldName)
                    return ModelProperty.GetPropertyForKey(kvp.Key);

                var fkMetadata = kvp.Value as ForeignKeyFieldMetadata ;
                if (fkMetadata != null && fkMetadata.RelatedFieldName == fieldName)
                    return ModelProperty.GetPropertyForKey(kvp.Key);
            }

            return null;
        }

        private void UpdateKeys(IEnumerable collection, ModelProperty property, int key, int newKey)
        {
            foreach (ModelBase model in collection)
            {
                var currentValue = (int)model.GetValue(property);
                if (currentValue == key)
                {
                    // ASSERT: if a temporary key exists in the record, it must already be modified, so changing 
                    // the temporary key to a permanent one will also mark the record as modified, which is ok.
                    model.SetValue(property, newKey);
                }
            }
        }
    }
}
