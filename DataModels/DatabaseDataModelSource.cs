using System;
using System.Collections.Generic;
using System.Linq;
using Jamiras.Database;
using Jamiras.DataModels.Metadata;
using System.Collections;

namespace Jamiras.DataModels
{
    public class DatabaseDataModelSource : IDataModelSource
    {
        public DatabaseDataModelSource(IDataModelMetadataRepository metadataRepository, IDatabase database)
        {
            _metadataRepository = metadataRepository;
            _database = database;
            _items = new Dictionary<Type, List<WeakReference>>();
        }

        private readonly IDataModelMetadataRepository _metadataRepository;
        private readonly IDatabase _database;
        private readonly Dictionary<Type, List<WeakReference>> _items;

        private readonly ModelProperty IdProperty = ModelProperty.Register(typeof(DatabaseDataModelSource), null, typeof(int), 0);

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

            List<WeakReference> items;
            if (!_items.TryGetValue(typeof(T), out items))
            {
                items = new List<WeakReference>();
                _items[typeof(T)] = items;
            }

            T item;
            int idx;
            lock (items)
            {
                item = FindItem<T>(items, id, metadata, out idx);
                if (item != null)
                    return item;
            }

            var newItem = GetCopy<T>(id);
            if (newItem == null)
                return null;

            newItem.SetValueCore(IdProperty, id);

            lock (items)
            {
                item = FindItem<T>(items, id, metadata, out idx);
                if (item == null)
                {
                    item = newItem;
                    items.Insert(idx, new WeakReference(newItem));
                }
            }

            return item;
        }

        internal T TryGet<T>(int id)
            where T : DataModelBase
        {
            List<WeakReference> items;
            if (!_items.TryGetValue(typeof(T), out items))
                return null;

            var metadata = (DatabaseModelMetadata)_metadataRepository.GetModelMetadata(typeof(T));

            int idx;
            return FindItem<T>(items, id, metadata, out idx);
        }

        internal T TryCache<T>(T item)
            where T : DataModelBase
        {
            var metadata = _metadataRepository.GetModelMetadata(typeof(T)) as DatabaseModelMetadata;
            if (metadata == null)
                throw new ArgumentException("No metadata registered for " + typeof(T).FullName);

            List<WeakReference> items;
            if (!_items.TryGetValue(typeof(T), out items))
            {
                items = new List<WeakReference>();
                _items[typeof(T)] = items;
            }

            int id = metadata.GetKey(item);

            lock (items)
            {
                int idx;
                var existingItem = FindItem<T>(items, id, metadata, out idx);
                if (existingItem != null)
                    return existingItem;

                item.SetValueCore(IdProperty, id);
                items.Insert(idx, new WeakReference(item));
            }

            return item;
        }

        private T FindItem<T>(List<WeakReference> items, int id, DatabaseModelMetadata metadata, out int idx)
            where T : DataModelBase
        {
            int low = 0;
            int high = items.Count - 1;

            while (low <= high)
            {
                int mid = (low + high) / 2;
                var item = items[mid].Target as T;
                if (item == null)
                {
                    items.RemoveAt(mid);
                    high--;
                    continue;
                }

                int itemId = (int)item.GetValue(IdProperty);
                if (itemId == id)
                {
                    idx = mid;
                    return item;
                }

                if (itemId > id)
                    high = mid - 1;
                else
                    low = mid + 1;
            }

            idx = low;
            return null;
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
            model = TryCache<T>(model);
            return model;
        }

        /// <summary>
        /// Commits changes made to a data model. The shared model and any future copies will contain committed changes.
        /// </summary>
        /// <param name="dataModel">Data model to commit.</param>
        /// <returns><c>true</c> if the changes were committed, <c>false</c> if not.</returns>
        public bool Commit(DataModelBase model)
        {
            var metadata = _metadataRepository.GetModelMetadata(model.GetType()) as DatabaseModelMetadata;
            if (metadata == null)
                return false;

            var key = metadata.GetKey(model);
            if (!metadata.Commit(model, _database))
                return false;

            var newKey = metadata.GetKey(model);
            if (key != newKey)
            {
                var fieldMetadata = metadata.GetFieldMetadata(metadata.PrimaryKeyProperty);

                List<WeakReference> items;
                if (_items.TryGetValue(model.GetType(), out items))
                {
                    lock (items)
                    {
                        for (int i = items.Count - 1; i >= 0; i--)
                        {
                            if (ReferenceEquals(model, items[i].Target))
                            {
                                items.RemoveAt(i);
                                break;
                            }
                        }

                        int idx;
                        FindItem<DataModelBase>(items, newKey, metadata, out idx);
                        model.SetValueCore(IdProperty, newKey);
                        items.Insert(idx, new WeakReference(model));
                    }
                }

                ExpireCollections(model.GetType());
                UpdateKeys(fieldMetadata, key, newKey);
            }

            model.AcceptChanges();
            return true;
        }

        private void ExpireCollections(Type modelType)
        {
            foreach (var kvp in _items)
            {
                if (typeof(IDataModelCollection).IsAssignableFrom(kvp.Key))
                {
                    bool expire = false;

                    foreach (var item in kvp.Value)
                    {
                        var collection = item.Target as IDataModelCollection;
                        if (collection != null)
                        {
                            expire = collection.ModelType.IsAssignableFrom(modelType);
                            break;
                        }
                    }

                    if (expire)
                        kvp.Value.Clear();
                }
            }
        }

        private void UpdateKeys(FieldMetadata fieldMetadata, int key, int newKey)
        {
            // TODO: find all foreign keys pointing at fieldMetadata.FieldName and update the associated property
        }
    }
}
