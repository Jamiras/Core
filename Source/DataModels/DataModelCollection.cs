﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Collections;
using Jamiras.Components;
using Jamiras.DataModels.Metadata;

namespace Jamiras.DataModels
{
    /// <summary>
    /// A collection of DataModels
    /// </summary>
    [DebuggerDisplay("Count = {Count}")]
    public class DataModelCollection<T> : DataModelBase, ICollection<T>, IDataModelCollection
        where T : DataModelBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataModelCollection{T}"/> class.
        /// </summary>
        public DataModelCollection()
        {
            _collection = new List<T>();
        }

        /// <summary>
        /// Gets the underlying collection.
        /// </summary>
        protected List<T> Collection
        {
            get { return _collection; }
        }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly List<T> _collection;

        private static readonly ModelProperty IsReadOnlyProperty =
            ModelProperty.Register(typeof(DataModelCollection<T>), "IsReadOnly", typeof(bool), false);

        private static readonly ModelProperty RemovedItemsProperty =
            ModelProperty.Register(typeof(DataModelCollection<T>), null, typeof(List<T>), null);

        private static readonly ModelProperty AddedItemsProperty =
            ModelProperty.Register(typeof(DataModelCollection<T>), null, typeof(List<T>), null);

        /// <summary>
        /// Gets the unique identifier of this collection if managed by IDataModelSource.
        /// </summary>
        protected int GetFilterKey()
        {
            var metadata = ServiceRepository.Instance.FindService<IDataModelMetadataRepository>().GetModelMetadata(GetType());
            var modelMetadata = metadata as DatabaseModelMetadata;
            if (modelMetadata != null)
                return modelMetadata.GetKey(this);

            var collectionMetadata = metadata as IDataModelCollectionMetadata;
            if (collectionMetadata != null)
                return (int)GetValue(collectionMetadata.CollectionFilterKeyProperty);

            return 0;
        }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="Count"/>
        /// </summary>
        public static readonly ModelProperty CountProperty =
            ModelProperty.Register(typeof(DataModelCollection<T>), "Count", typeof(int), 0);

        /// <summary>
        /// Gets the number of items in the collection
        /// </summary>
        public int Count
        {
            get { return (int)GetValue(CountProperty); }
            private set { SetValue(CountProperty, value); }
        }

        /// <summary>
        /// Determines if the collection contains a specific item.
        /// </summary>
        public bool Contains(T item)
        {
            return _collection.Contains(item);
        }

        /// <summary>
        /// Adds an item to the collection.
        /// </summary>
        public void Add(T item)
        {
            if (IsReadOnly)
                throw new ReadOnlyException("Cannot modify read only collection.");

            InsertAtIndex(_collection.Count, item);
        }

        /// <summary>
        /// Inserts an item at the specified index.
        /// </summary>
        protected void InsertAtIndex(int index, T item)
        {
            _collection.Insert(index, item);

            UpdateModifications(AddedItemsProperty, RemovedItemsProperty, item);
            Count = _collection.Count;

            OnCollectionChanged();
        }

        /// <summary>
        /// Removes an item from the collection.
        /// </summary>
        public bool Remove(T item)
        {
            if (IsReadOnly)
                throw new ReadOnlyException("Cannot modify read only collection.");

            int index = _collection.IndexOf(item);
            if (index == -1)
                return false;

            RemoveAtIndex(index);
            return true;
        }

        /// <summary>
        /// Removes an item from the collection.
        /// </summary>
        protected void RemoveAtIndex(int index)
        {
            var item = _collection[index];
            _collection.RemoveAt(index);

            UpdateModifications(RemovedItemsProperty, AddedItemsProperty, item);
            Count = _collection.Count;

            OnCollectionChanged();
        }

        private void UpdateModifications(ModelProperty collectionToAddToProperty, ModelProperty collectionToRemoveFromProperty, T item)
        {
            var collectionToAddTo = (List<T>)GetValue(collectionToAddToProperty);
            var collectionToRemoveFrom = (List<T>)GetValue(collectionToRemoveFromProperty);
            if (collectionToRemoveFrom != null && collectionToRemoveFrom.Remove(item))
            {
                if (collectionToRemoveFrom.Count == 0)
                    SetValue(collectionToRemoveFromProperty, null);
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
        }

        Type IDataModelCollection.ModelType
        {
            get { return typeof(T); }
        }

        void IDataModelCollection.Add(DataModelBase item)
        {
            if (IsReadOnly)
                throw new ReadOnlyException("Cannot modify read only collection.");

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

        /// <summary>
        /// Removes all items from the collection.
        /// </summary>
        public void Clear()
        {
            if (IsReadOnly)
                throw new ReadOnlyException("Cannot modify read only collection.");

            if (_collection.Count > 0)
            {
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
                    SetValue(AddedItemsProperty, null);

                _collection.Clear();

                Count = 0;

                OnCollectionChanged();
            }
        }

        internal virtual void OnCollectionChanged()
        { 
        }

        /// <summary>
        /// Accepts pending changes to the model.
        /// </summary>
        public override void AcceptChanges()
        {
            SetValue(AddedItemsProperty, null);
            SetValue(RemovedItemsProperty, null);

            base.AcceptChanges();
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

        /// <summary>
        /// Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1"/> to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
        /// </summary>
        public void CopyTo(T[] array, int arrayIndex)
        {
            _collection.CopyTo(array, arrayIndex);
        }

        void IDataModelCollection.MakeReadOnly()
        {
            IsReadOnly = true;
        }

        /// <summary>
        /// Gets whether or not the collection is read only.
        /// </summary>
        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            private set
            {
                if (IsReadOnly)
                    throw new InvalidOperationException("Cannot modify IsReadOnly property once it's been set to true.");
    
                SetValueCore(IsReadOnlyProperty, value);
            }
        }

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            return _collection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Gets the item at the specified index.
        /// </summary>
        public T this[int index]
        {
            get { return _collection[index]; }
        }
    }
}
