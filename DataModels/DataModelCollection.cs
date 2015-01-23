using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Collections;

namespace Jamiras.DataModels
{
    [DebuggerDisplay("Count = {Count}")]
    public class DataModelCollection<T> : DataModelBase, ICollection<T>, IDataModelCollection
        where T : DataModelBase
    {
        public DataModelCollection()
        {
            _collection = new List<T>();
        }

        private readonly List<T> _collection;

        private static readonly ModelProperty RemovedItemsProperty =
            ModelProperty.Register(typeof(DataModelCollection<T>), null, typeof(List<T>), null);

        private static readonly ModelProperty AddedItemsProperty =
            ModelProperty.Register(typeof(DataModelCollection<T>), null, typeof(List<T>), null);

        private static readonly ModelProperty IsCollectionChangedProperty =
            ModelProperty.Register(typeof(DataModelCollection<T>), null, typeof(bool), false);

        public bool IsCollectionChanged
        {
            get { return (bool)GetValue(IsCollectionChangedProperty); }
            private set { SetValue(IsCollectionChangedProperty, value); }
        }

        public static readonly ModelProperty CountProperty =
            ModelProperty.Register(typeof(DataModelCollection<T>), "Count", typeof(int), 0);

        public int Count
        {
            get { return (int)GetValue(CountProperty); }
            private set { SetValue(CountProperty, value); }
        }

        public bool Contains(T item)
        {
            return _collection.Contains(item);
        }

        public void Add(T item)
        {
            _collection.Add(item);

            IsCollectionChanged = UpdateModifications(AddedItemsProperty, RemovedItemsProperty, item);
            Count = _collection.Count;
        }

        public bool Remove(T item)
        {
            if (!_collection.Remove(item))
                return false;

            IsCollectionChanged = UpdateModifications(RemovedItemsProperty, AddedItemsProperty, item);
            Count = _collection.Count;
            return true;
        }

        private bool UpdateModifications(ModelProperty collectionToAddToProperty, ModelProperty collectionToRemoveFromProperty, T item)
        {
            bool isCollectionChanged = true;

            var collectionToAddTo = (List<T>)GetValue(collectionToAddToProperty);
            var collectionToRemoveFrom = (List<T>)GetValue(collectionToRemoveFromProperty);
            if (collectionToRemoveFrom != null && collectionToRemoveFrom.Remove(item))
            {
                if (collectionToRemoveFrom.Count == 0)
                {
                    SetValue(collectionToRemoveFromProperty, null);
                    isCollectionChanged = (collectionToAddTo != null);
                }
            }
            else
            {
                if (collectionToAddTo == null)
                {
                    collectionToAddTo = new List<T>();
                    SetValue(collectionToAddToProperty, collectionToAddTo);
                }

                collectionToAddTo.Add(item);
            }

            return isCollectionChanged;
        }

        Type IDataModelCollection.ModelType
        {
            get { return typeof(T); }
        }

        void IDataModelCollection.Add(DataModelBase item)
        {
            if (IsModified)
            {
                Add((T)item);
            }
            else
            {
                _collection.Add((T)item);
                SetValueCore(CountProperty, _collection.Count);                
            }
        }

        bool IDataModelCollection.Contains(DataModelBase item)
        {
            return _collection.Contains((T)item);
        }

        public void Clear()
        {
            if (_collection.Count > 0)
            {
                bool isCollectionChanged = true;
                var removedItems = (List<T>)GetValue(RemovedItemsProperty);
                var addedItems = (List<T>)GetValue(AddedItemsProperty);
                foreach (var item in _collection)
                {
                    if (addedItems == null || !addedItems.Remove(item))
                    {
                        if (removedItems == null)
                        {
                            removedItems = new List<T>();
                            SetValue(RemovedItemsProperty, removedItems);
                        }

                        removedItems.Add(item);
                    }                    
                }

                if (addedItems != null && addedItems.Count == 0)
                {
                    SetValue(AddedItemsProperty, null);
                    isCollectionChanged = (removedItems != null);
                }

                _collection.Clear();

                IsCollectionChanged = isCollectionChanged;
                Count = 0;
            }
        }

        /// <summary>
        /// Accepts pending changes to the model.
        /// </summary>
        public override void AcceptChanges()
        {
            SetValue(AddedItemsProperty, null);
            SetValue(RemovedItemsProperty, null);

            base.AcceptChanges();

            IsCollectionChanged = false;
        }

        /// <summary>
        /// Discards pending changes to the model.
        /// </summary>
        public override void DiscardChanges()
        {
            var addedItems = (List<T>)GetValue(AddedItemsProperty);
            if (addedItems != null)
            {
                foreach (var item in addedItems)
                    _collection.Remove(item);
            }

            var removedItems = (List<T>)GetValue(RemovedItemsProperty);
            if (removedItems != null)
                _collection.AddRange(removedItems);

            base.DiscardChanges();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _collection.CopyTo(array, arrayIndex);
        }

        bool ICollection<T>.IsReadOnly
        {
            get { return false; }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _collection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public T this[int index]
        {
            get { return _collection[index]; }
        }
    }
}
