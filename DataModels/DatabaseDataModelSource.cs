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
                    item = existingItem;
                else
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

                int itemId = metadata.GetKey(item);
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
        /// Commits changes made to a data model. The shared model and any future copies will contain committed changes.
        /// </summary>
        /// <param name="dataModel">Data model to commit.</param>
        /// <returns><c>true</c> if the changes were committed, <c>false</c> if not.</returns>
        public bool Commit(DataModelBase model)
        {
            var metadata = _metadataRepository.GetModelMetadata(model.GetType()) as DatabaseModelMetadata;
            if (metadata == null)
                return false;

            return metadata.Commit(model, _database);
        }
    }
}
