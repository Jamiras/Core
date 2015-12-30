using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Jamiras.DataModels.Metadata;

namespace Jamiras.DataModels
{
    public abstract class DataModelSourceBase : IDataModelSource
    {
        protected DataModelSourceBase(IDataModelMetadataRepository metadataRepository)
        {
            _items = new Dictionary<Type, DataModelCache>();
            _metadataRepository = metadataRepository;
        }

        private readonly Dictionary<Type, DataModelCache> _items;
        private readonly IDataModelMetadataRepository _metadataRepository;

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

            private sealed class DataModelCacheDebugView
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

                        if (item.Value.Target != null)
                            break;
                    }

                    if (expire)
                        _cache.Clear();
                }
            }
        }

        /// <summary>
        /// Gets the metadata for a type.
        /// </summary>
        protected ModelMetadata GetModelMetadata(Type type)
        {
            var metadata = _metadataRepository.GetModelMetadata(type);
            if (metadata == null)
                throw new ArgumentException("No metadata registered for " + type.FullName);

            return metadata;
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
            var metadata = GetModelMetadata(typeof(T));
            var collectionMetadata = metadata as IDataModelCollectionMetadata;
            if (collectionMetadata != null)
                return Query<T>(searchData, Int32.MaxValue, collectionMetadata);

            return Query<T>(searchData, metadata);
        }

        protected abstract T Query<T>(object searchData, ModelMetadata metadata) where T : DataModelBase, new(); 

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
            var collectionMetadata = GetModelMetadata(typeof(T)) as IDataModelCollectionMetadata;
            if (collectionMetadata == null)
                throw new ArgumentException(typeof(T).FullName + " is not registered to a collection metadata");

            return Query<T>(searchData, maxResults, collectionMetadata);
        }

        internal virtual T Query<T>(object searchData, int maxResults, IDataModelCollectionMetadata collectionMetadata)
            where T : DataModelBase, new()
        {
            T model = new T();
            var collection = model as IDataModelCollection;
            if (collection != null)
            {
                var items = new List<DataModelBase>();
                if (!Query(typeof(T), searchData, items, maxResults, collection.ModelType))
                    return null;

                foreach (var item in items)
                    collection.Add(item);

                if (collectionMetadata.AreResultsReadOnly)
                    collection.MakeReadOnly();
            }

            return model;
        }

        protected virtual bool Query(Type collectionType, object searchData, ICollection<DataModelBase> results, int maxResults, Type resultType) 
        {
            return false;
        }

        /// <summary>
        /// Creates a new data model instance.
        /// </summary>
        /// <typeparam name="T">Type of data model to create.</typeparam>
        /// <returns>New instance initialized with default values.</returns>
        public abstract T Create<T>() where T : DataModelBase, new();

        /// <summary>
        /// Commits changes made to a data model. The shared model and any future copies will contain committed changes.
        /// </summary>
        /// <param name="dataModel">Data model to commit.</param>
        /// <returns><c>true</c> if the changes were committed, <c>false</c> if not.</returns>
        public bool Commit(DataModelBase dataModel)
        {
            if (!dataModel.IsModified && !(dataModel is IDataModelCollection))
                return true;

            var metadata = _metadataRepository.GetModelMetadata(dataModel.GetType());
            if (metadata == null)
                return false;

            var collection = dataModel as IDataModelCollection;
            if (collection != null)
            {
                var collectionMetadata = (IDataModelCollectionMetadata)metadata;
                if (!Commit(collection, collectionMetadata.ModelMetadata))
                    return false;
            }

            if (!Commit(dataModel, metadata))
                return false;

            dataModel.AcceptChanges();
            return true;
        }

        /// <summary>
        /// Commits a single model.
        /// </summary>
        protected abstract bool Commit(DataModelBase dataModel, ModelMetadata metadata);

        /// <summary>
        /// Commits a collection of models.
        /// </summary>
        protected virtual bool Commit(IEnumerable collection, ModelMetadata itemMetadata)
        {
            foreach (DataModelBase model in collection)
            {
                if (!Commit(model))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Discards any cached collection containing models of the specified type.
        /// </summary>
        protected void ExpireCollections(Type modelType)
        {
            foreach (var kvp in _items)
            {
                if (typeof(IDataModelCollection).IsAssignableFrom(kvp.Key))
                    kvp.Value.ExpireCollections(modelType);
            }
        }

        /// <summary>
        /// Changes <paramref name="key"/> to <paramref name="newKey"/> for any ForeignKeyFieldMetadata fields in any cached models.
        /// </summary>
        /// <param name="fieldMetadata">The <see cref="FieldMetadata"/> for the field being changed.</param>
        /// <param name="key">The old value.</param>
        /// <param name="newKey">The new value.</param>
        protected void UpdateKeys(FieldMetadata fieldMetadata, int key, int newKey)
        {
            foreach (var kvp in _items)
            {
                kvp.Value.UpdateKey(key, newKey);

                if (!kvp.Value.Models.Any())
                    continue;

                var modelMetadata = _metadataRepository.GetModelMetadata(kvp.Key);
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
                            foreach (var collection in kvp.Value.Models.OfType<IDataModelCollection>())
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

        private static ModelProperty GetDependantProperty(ModelMetadata modelMetadata, string fieldName)
        {
            foreach (var kvp in modelMetadata.AllFieldMetadata)
            {
                if (kvp.Value.FieldName == fieldName)
                    return ModelProperty.GetPropertyForKey(kvp.Key);

                var fkMetadata = kvp.Value as ForeignKeyFieldMetadata;
                if (fkMetadata != null && fkMetadata.RelatedFieldName == fieldName)
                    return ModelProperty.GetPropertyForKey(kvp.Key);
            }

            return null;
        }

        private static void UpdateKeys(IEnumerable collection, ModelProperty property, int key, int newKey)
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
