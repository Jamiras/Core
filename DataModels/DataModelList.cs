using System;
using System.Collections.Generic;
using System.Data;

namespace Jamiras.DataModels
{
    /// <summary>
    /// An indexable collection of data models.
    /// </summary>
    public class DataModelList<T> : DataModelCollection<T>, IList<T>
        where T : DataModelBase
    {
        private T[] unmodifiedList;

        private static readonly ModelProperty IsListModifiedProperty =
            ModelProperty.Register(typeof(DataModelList<T>), null, typeof(bool), false);

        /// <summary>
        /// Determines if the collection contains a specific item.
        /// </summary>
        public int IndexOf(T item)
        {
            return Collection.IndexOf(item);
        }

        /// <summary>
        /// Removes an item from the collection.
        /// </summary>
        public void RemoveAt(int index)
        {
            if (IsReadOnly)
                throw new ReadOnlyException("Cannot modify read only collection.");
            if (index < 0 || index >= Collection.Count)
                throw new ArgumentOutOfRangeException("index");

            RemoveAtIndex(index);
        }

        /// <summary>
        /// Adds an item to the collection.
        /// </summary>
        public void Insert(int index, T item)
        {
            if (IsReadOnly)
                throw new ReadOnlyException("Cannot modify read only collection.");
            if (index < 0 || index > Collection.Count)
                throw new ArgumentOutOfRangeException("index");

            InsertAtIndex(index, item);
        }

        /// <summary>
        /// Gets or sets the item at the specified index.
        /// </summary>
        /// <exception cref="ReadOnlyException">The collection is read only.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is before the first item or after the last item.</exception>
        public new T this[int index]
        {
            get { return Collection[index]; }
            set
            {
                if (IsReadOnly)
                    throw new ReadOnlyException("Cannot modify read only collection.");
                if (index < 0 || index >= Collection.Count)
                    throw new ArgumentOutOfRangeException("index");

                if (!ReferenceEquals(Collection[index], value))
                {
                    Collection[index] = value;
                    OnCollectionChanged();
                }
            }
        }

        /// <summary>
        /// Moves an item within the collection.
        /// </summary>
        public void Move(int oldIndex, int newIndex)
        {
            if (IsReadOnly)
                throw new ReadOnlyException("Cannot modify read only collection.");
            if (oldIndex < 0 || oldIndex >= Collection.Count)
                throw new ArgumentOutOfRangeException("oldIndex");
            if (newIndex < 0 || newIndex >= Collection.Count)
                throw new ArgumentOutOfRangeException("newIndex");

            T item = Collection[oldIndex];
            if (oldIndex > newIndex)
            {
                for (int i = oldIndex; i > newIndex; i--)
                    Collection[i] = Collection[i - 1];
                Collection[newIndex] = item;

                OnCollectionChanged();
            }
            else if (oldIndex < newIndex)
            {
                for (int i = oldIndex; i < newIndex; i++)
                    Collection[i] = Collection[i + 1];
                Collection[newIndex] = item;

                OnCollectionChanged();
            }
        }

        internal override void OnCollectionChanged()
        {
            if (unmodifiedList != null)
            {
                if (unmodifiedList.Length != Collection.Count)
                {
                    SetValue(IsListModifiedProperty, true);
                }
                else
                {
                    bool isModified = false;
                    for (int i = 0; i < unmodifiedList.Length; i++)
                    {
                        if (!ReferenceEquals(unmodifiedList[i], Collection[i]))
                        {
                            isModified = true;
                            break;
                        }
                    }

                    SetValue(IsListModifiedProperty, isModified);
                }
            }

            base.OnCollectionChanged();
        }

        /// <summary>
        /// Accepts pending changes to the model.
        /// </summary>
        public override void AcceptChanges()
        {
            UpdateUnmodifiedList();
            SetValue(IsListModifiedProperty, false);
            base.AcceptChanges();
        }

        /// <summary>
        /// Discards pending changes to the model.
        /// </summary>
        public override void DiscardChanges()
        {
            UpdateUnmodifiedList();
            SetValue(IsListModifiedProperty, false);
            base.DiscardChanges();
        }

        private void UpdateUnmodifiedList()
        {
            unmodifiedList = Collection.ToArray();
        }
    }
}
