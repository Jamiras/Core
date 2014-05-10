using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Jamiras.Components
{
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(TinyDictionary<,>.TinyDictionaryDebugView))]
    internal sealed class OneItemTinyDictionary<TKey, TValue> : ITinyDictionary<TKey, TValue>
    {
        private readonly TKey _key;
        private TValue _value;

        public OneItemTinyDictionary(TKey key, TValue value)
        {
            _key = key;
            _value = value;
        }

        /// <summary>
        /// Adds an element with the provided key and value to the ITinyDictionary.
        /// </summary>
        /// <param name="key">The object to use as the key of the element to add.</param>
        /// <param name="value">The object to use as the value of the element to add.</param>
        /// <exception cref="T:System.ArgumentException">An element with the same key already exists in the ITinyDictionary.</exception>
        public ITinyDictionary<TKey, TValue> Add(TKey key, TValue value)
        {
            if (Equals(key, _key))
                throw new ArgumentException("An element with the provided key already exists in this dictionary.", "key");

            return new TwoItemsTinyDictionary<TKey, TValue>(_key, _value, key, value);
        }

        /// <summary>
        /// Adds an element with the provided key and value to the ITinyDictionary, or replaces the value for the provided key if the key already exists.
        /// </summary>
        /// <param name="key">The object to use as the key of the element to add.</param>
        /// <param name="value">The object to use as the value of the element to add.</param>
        public ITinyDictionary<TKey, TValue> AddOrUpdate(TKey key, TValue value)
        {
            if (!Equals(key, _key))
                return new TwoItemsTinyDictionary<TKey, TValue>(_key, _value, key, value);

            _value = value;
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
            if (Equals(key, _key))
                return EmptyTinyDictionary<TKey, TValue>.Instance;

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
            return Equals(key, _key);
        }

        /// <summary>
        /// Gets the number of elements contained in the ITinyDictionary.
        /// </summary>
        public int Count
        {
            get { return 1; }
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the keys of the ITinyDictionary.
        /// </summary>
        public ICollection<TKey> Keys
        {
            get { return new TKey[] { _key }; }
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the values in the ITinyDictionary.
        /// </summary>
        public ICollection<TValue> Values
        {
            get { return new TValue[] { _value }; }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            yield return new KeyValuePair<TKey, TValue>(_key, _value);
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
            if (Equals(_key, key))
            {
                value = _value;
                return true;
            }

            value = default(TValue);
            return false;
        }
    }
}
