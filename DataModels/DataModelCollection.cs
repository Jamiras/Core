using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Jamiras.DataModels
{
    [DebuggerDisplay("Count = {Count}")]
    public class DataModelCollection<T> : DataModelBase, ICollection<T>
        where T : DataModelBase
    {
        public DataModelCollection()
        {
            _collection = new List<T>();
        }

        private List<T> _collection;

        public void Add(T item)
        {
            throw new NotImplementedException();
        }

        internal void AddCore(T item)
        {
            _collection.Add(item);
        }

        public void Clear()
        {
            throw new NotImplementedException();
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
            return _collection.Remove(item);
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
