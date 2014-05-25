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

        private List<T> _collection;

        private static readonly ModelProperty IsCollectionChangedProperty =
            ModelProperty.Register(typeof(DataModelCollection<T>), null, typeof(bool), false);

        public bool IsCollectionChanged
        {
            get { return (bool)GetValue(IsCollectionChangedProperty); }
            private set { SetValue(IsCollectionChangedProperty, value); }
        }

        Type IDataModelCollection.ModelType
        {
            get { return typeof(T); }
        }

        public void Add(T item)
        {
            _collection.Add(item);
            IsCollectionChanged = true;
        }

        void IDataModelCollection.Add(DataModelBase item)
        {
            _collection.Add((T)item);
        }

        public void Clear()
        {
            if (_collection.Count > 0)
            {
                _collection.Clear();
                IsCollectionChanged = true;
            }
        }

        public bool Contains(T item)
        {
            return _collection.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _collection.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _collection.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T item)
        {
            if (!_collection.Remove(item))
                return false;

            IsCollectionChanged = true;
            return true;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _collection.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public T this[int index]
        {
            get { return _collection[index]; }
        }
    }
}
