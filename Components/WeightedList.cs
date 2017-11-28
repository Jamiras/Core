using System;
using System.Collections.Generic;

namespace Jamiras.Components
{
    /// <summary>
    /// Represents a collection of non-uniformly distributed items that can be randomly selected from.
    /// </summary>
    public class WeightedCollection<T> : ICollection<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WeightedCollection{T}"/> class.
        /// </summary>
        public WeightedCollection()
        {
            _items = EmptyTinyDictionary<T, int>.Instance;
            _totalWeight = 0;
        }

        private ITinyDictionary<T, int> _items;
        private int _totalWeight;
        private static Random _random = new Random();

        /// <summary>
        /// Adds the specified item to the collection.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <param name="weight">The weight of the item. Compared to the weights of other items in the collection, determines the liklyhood of this item being selected.</param>
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

        /// <summary>
        /// Removes all items from the collection.
        /// </summary>
        public void Clear()
        {
            _items = EmptyTinyDictionary<T, int>.Instance;
        }

        /// <summary>
        /// Determines whether the collection contains the specified item.
        /// </summary>
        public bool Contains(T item)
        {
            return _items.ContainsKey(item);
        }

        /// <summary>
        /// Gets the number of items in the collection.
        /// </summary>
        public int Count
        {
            get { return _items.Count; }
        }

        /// <summary>
        /// Removes an item from the collection.
        /// </summary>
        /// <returns><c>true</c> if the item was removed from the collection, <c>false</c> if it was not found.</returns>
        public bool Remove(T item)
        {
            int oldWeight;
            if (!_items.TryGetValue(item, out oldWeight))
                return false;

            _totalWeight -= oldWeight;
            _items = _items.Remove(item);
            return true;
        }

        /// <summary>
        /// Gets an enumerator for the collection.
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            return _items.Keys.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Gets the weight associated to an item in the collection.
        /// </summary>
        public int GetWeight(T item)
        {
            int weight;
            _items.TryGetValue(item, out weight);
            return weight;
        }

        /// <summary>
        /// Gets a random item from the collection. Items with higher weights are more likely to be returned.
        /// </summary>
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

        /// <summary>
        /// Populates the <paramref name="array"/>, starting at <paramref name="arrayIndex"/> with the items from the collection.
        /// </summary>
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
