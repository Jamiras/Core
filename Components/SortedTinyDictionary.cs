using System;
using System.Collections.Generic;

namespace Jamiras.Components
{
    internal sealed class SortedTinyDictionary<TKey, TValue> : ITinyDictionary<TKey, TValue>
    {
        private TKey[] _keys;
        private TValue[] _values;
        private int _count;

        public SortedTinyDictionary()
        {
            _keys = new TKey[4];
            _values = new TValue[4];
            _count = 0;
        }

        /// <summary>
        /// Gets the index of the provided key.
        /// </summary>
        /// <param name="key">Key to find index of.</param>
        /// <returns>Index of provided key, or bitwise invert of index where key should be inserted if not present.</returns>
        private int IndexOf(TKey key)
        {
            IComparable comparableKey = ((IComparable)key);
            int low = 0;
            int high = _count;

            while (low < high)
            {
                int mid = (low + high) / 2;

                int diff = comparableKey.CompareTo(_keys[mid]);
                if (diff == 0)
                    return mid;

                if (diff > 0)
                    low = mid + 1;
                else
                    high = mid;
            }

            return ~low;
        }

        /// <summary>
        /// Adds an element with the provided key and value to the ITinyDictionary.
        /// </summary>
        /// <param name="key">The object to use as the key of the element to add.</param>
        /// <param name="value">The object to use as the value of the element to add.</param>
        /// <exception cref="T:System.ArgumentException">An element with the same key already exists in the ITinyDictionary.</exception>
        public ITinyDictionary<TKey, TValue> Add(TKey key, TValue value)
        {
            int index = IndexOf(key);
            if (index >= 0)
                throw new ArgumentException("An element with the provided key already exists in this dictionary.", "key");

            return Insert(~index, key, value);
        }

        /// <summary>
        /// Adds an element with the provided key and value to the ITinyDictionary, or replaces the value for the provided key if the key already exists.
        /// </summary>
        /// <param name="key">The object to use as the key of the element to add.</param>
        /// <param name="value">The object to use as the value of the element to add.</param>
        public ITinyDictionary<TKey, TValue> AddOrUpdate(TKey key, TValue value)
        {
            int index = IndexOf(key);
            if (index >= 0)
            {
                _values[index] = value;
                return this;
            }

            return Insert(~index, key, value);
        }

        private ITinyDictionary<TKey, TValue> Insert(int index, TKey key, TValue value)
        {
            if (_count < _keys.Length)
            {
                if (index < _count)
                {
                    Array.Copy(_keys, index, _keys, index + 1, _count - index);
                    Array.Copy(_values, index, _values, index + 1, _count - index);
                }
            }
            else 
            {
                var newKeys = new TKey[_keys.Length + 4];
                Array.Copy(_keys, newKeys, index);
                Array.Copy(_keys, index, newKeys, index + 1, _count - index);
                _keys = newKeys;

                var newValues = new TValue[_values.Length + 4];
                Array.Copy(_values, newValues, index);
                Array.Copy(_values, index, newValues, index + 1, _count - index);
                _values = newValues;
            }

            _keys[index] = key;
            _values[index] = value;
            _count++;

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
            int index = IndexOf(key);
            if (index < 0)
                return this;

            if (_count == 3)
            {
                int index1 = 0, index2 = 1;
                if (index == 0)
                    index1 = 2;
                else if (index == 1)
                    index2 = 2;

                return new TwoItemsTinyDictionary<TKey, TValue>(_keys[index1], _values[index2], _keys[index2], _values[index2]);
            }

            if (index + 1 < _count)
            {
                Array.Copy(_keys, index + 1, _keys, index, _count - index - 1);
                Array.Copy(_values, index + 1, _values, index, _count - index - 1);
            }

            _count--;
            _keys[_count] = default(TKey);
            _values[_count] = default(TValue);
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
            return (IndexOf(key) >= 0);
        }

        /// <summary>
        /// Gets the number of elements contained in the ITinyDictionary.
        /// </summary>
        public int Count
        {
            get { return _count; }
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the keys of the ITinyDictionary.
        /// </summary>
        public ICollection<TKey> Keys
        {
            get
            {
                TKey[] keys = new TKey[_count];
                Array.Copy(keys, _keys, _count);
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
                TValue[] values = new TValue[_count];
                Array.Copy(values, _values, _count);
                return values;
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            for (int i = 0; i < _count; i++)
                yield return new KeyValuePair<TKey, TValue>(_keys[i], _values[i]);
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
            int index = IndexOf(key);
            if (index >= 0)
            {
                value = _values[index];
                return true;
            }

            value = default(TValue);
            return false;
        }
    }
}
