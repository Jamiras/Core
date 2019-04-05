using System.Collections.Generic;

namespace Jamiras.Components
{
    internal sealed class BucketTinyDictionary<TKey, TValue> : ITinyDictionary<TKey, TValue>
    {
        public BucketTinyDictionary(int buckets)
        {
            _buckets = new ITinyDictionary<TKey, TValue>[buckets];
            for (int i = 0; i < buckets; i++)
                _buckets[i] = EmptyTinyDictionary<TKey, TValue>.Instance;
        }

        private readonly ITinyDictionary<TKey, TValue>[] _buckets;

        private int GetBucket(TKey key)
        {
            var hash = key.GetHashCode();
            return hash % _buckets.Length;
        }

        /// <summary>
        /// Adds an element with the provided key and value to the ITinyDictionary.
        /// </summary>
        /// <param name="key">The object to use as the key of the element to add.</param>
        /// <param name="value">The object to use as the value of the element to add.</param>
        /// <exception cref="T:System.ArgumentException">An element with the same key already exists in the ITinyDictionary.</exception>
        public ITinyDictionary<TKey, TValue> Add(TKey key, TValue value)
        {
            var bucketIndex = GetBucket(key);
            _buckets[bucketIndex] = _buckets[bucketIndex].Add(key, value);
            return this;
        }

        /// <summary>
        /// Adds an element with the provided key and value to the ITinyDictionary, or replaces the value for the provided key if the key already exists.
        /// </summary>
        /// <param name="key">The object to use as the key of the element to add.</param>
        /// <param name="value">The object to use as the value of the element to add.</param>
        public ITinyDictionary<TKey, TValue> AddOrUpdate(TKey key, TValue value)
        {
            var bucketIndex = GetBucket(key);
            _buckets[bucketIndex] = _buckets[bucketIndex].AddOrUpdate(key, value);
            return this;
        }

        /// <summary>
        /// Removes the element with the specified key from the ITinyDictionary.
        /// </summary>
        /// <returns>
        /// true if the element is successfully removed; otherwise, false.  This method also returns false if <paramref name="key"/> was not found in the original ITinyDictionary.
        /// </returns>
        /// <param name="key">The key of the element to remove.</param>
        public ITinyDictionary<TKey, TValue> Remove(TKey key)
        {
            var bucketIndex = GetBucket(key);
            _buckets[bucketIndex] = _buckets[bucketIndex].Remove(key);
            return this;
        }

        /// <summary>
        /// Determines whether the ITinyDictionary contains an element with the specified key.
        /// </summary>
        /// <returns>
        /// true if the ITinyDictionary contains an element with the key; otherwise, false.
        /// </returns>
        /// <param name="key">The key to locate in the ITinyDictionary.</param>
        public bool ContainsKey(TKey key)
        {
            var bucketIndex = GetBucket(key);
            return _buckets[bucketIndex].ContainsKey(key);
        }

        /// <summary>
        /// Gets the number of elements contained in the ITinyDictionary.
        /// </summary>
        public int Count
        {
            get 
            {
                int count = 0;
                foreach (var bucket in _buckets)
                    count += bucket.Count;

                return count;
            }
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the keys of the ITinyDictionary.
        /// </summary>
        public ICollection<TKey> Keys
        {
            get 
            {
                var keys = new List<TKey>();
                foreach (var bucket in _buckets)
                {
                    foreach (var key in bucket.Keys)
                        keys.Add(key);
                }

                return keys;
            }
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the values in the ITinyDictionary.
        /// </summary>
        public ICollection<TValue> Values
        {
            get
            {
                var values = new List<TValue>();
                foreach (var bucket in _buckets)
                {
                    foreach (var value in bucket.Values)
                        values.Add(value);
                }

                return values;
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (var bucket in _buckets)
            {
                foreach (var kvp in bucket)
                    yield return kvp;
            }
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <returns>
        /// true if the object that implements ITinyDictionary contains an element with the specified key; otherwise, false.
        /// </returns>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="value">When this method returns, the value associated with the specified key, if the key is found; otherwise, the default value for the type of the <paramref name="value"/> parameter. This parameter is passed uninitialized.</param>
        public bool TryGetValue(TKey key, out TValue value)
        {
            var bucketIndex = GetBucket(key);
            return _buckets[bucketIndex].TryGetValue(key, out value);
        }
    }
}
