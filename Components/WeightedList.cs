using System;
using System.Collections.Generic;

namespace Jamiras.Components
{
    public class WeightedCollection<T> : ICollection<T>
    {
        public WeightedCollection()
        {
            _items = EmptyTinyDictionary<T, int>.Instance;
            _totalWeight = 0;
        }

        private ITinyDictionary<T, int> _items;
        private int _totalWeight;
        private static Random _random = new Random();

        public void Add(T item, int weight)
        {
            int oldWeight;
            if (_items.TryGetValue(item, out oldWeight))
            {
                _totalWeight -= oldWeight;
                _items = _items.AddOrUpdate(item, weight);
            }
            else
            {
                _items = _items.Add(item, weight);
            }

            _totalWeight += weight;
        }

        public void Clear()
        {
            _items = EmptyTinyDictionary<T, int>.Instance;
        }

        public bool Contains(T item)
        {
            return _items.ContainsKey(item);
        }

        public int Count
        {
            get { return _items.Count; }
        }

        public bool Remove(T item)
        {
            int oldWeight;
            if (!_items.TryGetValue(item, out oldWeight))
                return false;

            _totalWeight -= oldWeight;
            _items = _items.Remove(item);
            return true;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _items.Keys.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int GetWeight(T item)
        {
            int weight;
            _items.TryGetValue(item, out weight);
            return weight;
        }

        public T GetRandom()
        {
            int target = _random.Next(_totalWeight);
            foreach (var pair in _items)
            {
                target -= pair.Value;
                if (target < 0)
                    return pair.Key;
            }

            return default(T);
        }

        #region ICollection<T> Members

        void ICollection<T>.Add(T item)
        {
            throw new NotSupportedException("Weight must be specified when adding items");
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _items.Keys.CopyTo(array, arrayIndex);
        }

        bool ICollection<T>.IsReadOnly
        {
            get { return false; }
        }

        #endregion
    }
}
