using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Jamiras.Components
{
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(TinyDictionary<,>.TinyDictionaryDebugView))]
    internal sealed class TwoItemsTinyDictionary<TKey, TValue> : ITinyDictionary<TKey, TValue>
    {
        private readonly TKey _key1, _key2;
        private TValue _value1, _value2;

        public TwoItemsTinyDictionary(TKey key1, TValue value1, TKey key2, TValue value2)
        {
            _key1 = key1;
            _value1 = value1;
            _key2 = key2;
            _value2 = value2;
        }

        /// <summary>
        /// Adds an element with the provided key and value to the ITinyDictionary.
        /// </summary>
        /// <param name="key">The object to use as the key of the element to add.</param>
        /// <param name="value">The object to use as the value of the element to add.</param>
        /// <exception cref="T:System.ArgumentException">An element with the same key already exists in the ITinyDictionary.</exception>
        public ITinyDictionary<TKey, TValue> Add(TKey key, TValue value)
        {
            if (Equals(key, _key1) || Equals(key, _key2))
                throw new ArgumentException("An element with the provided key already exists in this dictionary.", "key");

            ITinyDictionary<TKey, TValue> newDictionary;
            if (key is IComparable)
                newDictionary = new SortedTinyDictionary<TKey, TValue>();
            else
                newDictionary = new UnsortedTinyDictionary<TKey, TValue>();

            newDictionary.Add(_key1, _value1);
            newDictionary.Add(_key2, _value2);
            newDictionary.Add(key, value);
            return newDictionary;
        }

        /// <summary>
        /// Adds an element with the provided key and value to the ITinyDictionary, or replaces the value for the provided key if the key already exists.
        /// </summary>
        /// <param name="key">The object to use as the key of the element to add.</param>
        /// <param name="value">The object to use as the value of the element to add.</param>
        public ITinyDictionary<TKey, TValue> AddOrUpdate(TKey key, TValue value)
        {
            if (Equals(key, _key1))
            {
                _value1 = value;
                return this;
            }

            if (Equals(key, _key2))
            {
                _value2 = value;
                return this;
            }

            return Add(key, value);
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
            if (Equals(key, _key1))
                return new OneItemTinyDictionary<TKey, TValue>(_key2, _value2);
            if (Equals(key, _key2))
                return new OneItemTinyDictionary<TKey, TValue>(_key1, _value1);

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
            return Equals(key, _key1) || Equals(key, _key2);
        }

        /// <summary>
        /// Gets the number of elements contained in the ITinyDictionary.
        /// </summary>
        public int Count
        {
            get { return 2; }
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the keys of the ITinyDictionary.
        /// </summary>
        public ICollection<TKey> Keys
        {
            get { return new TKey[] { _key1, _key2 }; }
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the values in the ITinyDictionary.
        /// </summary>
        public ICollection<TValue> Values
        {
            get { return new TValue[] { _value1, _value2 }; }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            yield return new KeyValuePair<TKey, TValue>(_key1, _value1);
            yield return new KeyValuePair<TKey, TValue>(_key2, _value2);
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
            if (Equals(_key1, key))
            {
                value = _value1;
                return true;
            }

            if (Equals(_key2, key))
            {
                value = _value2;
                return true;
            }

            value = default(TValue);
            return false;
        }
    }
}
