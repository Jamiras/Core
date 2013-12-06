using System.Collections.Generic;

namespace Jamiras.Components
{
    internal interface ITinyDictionary<TKey, TValue>
    {
        /// <summary>
        /// Adds an element with the provided key and value to the ITinyDictionary.
        /// </summary>
        /// <param name="key">The object to use as the key of the element to add.</param>
        /// <param name="value">The object to use as the value of the element to add.</param>
        /// <exception cref="T:System.ArgumentException">An element with the same key already exists in the ITinyDictionary.</exception>
        ITinyDictionary<TKey, TValue> Add(TKey key, TValue value);

        /// <summary>
        /// Adds an element with the provided key and value to the ITinyDictionary, or replaces the value for the provided key if the key already exists.
        /// </summary>
        /// <param name="key">The object to use as the key of the element to add.</param>
        /// <param name="value">The object to use as the value of the element to add.</param>
        ITinyDictionary<TKey, TValue> AddOrUpdate(TKey key, TValue value);

        /// <summary>
        /// Removes the element with the specified key from the ITinyDictionary.
        /// </summary>
        /// <returns>
        /// true if the element is successfully removed; otherwise, false.  This method also returns false if <paramref name="key"/> was not found in the original ITinyDictionary.
        /// </returns>
        /// <param name="key">The key of the element to remove.</param>
        ITinyDictionary<TKey, TValue> Remove(TKey key);

        /// <summary>
        /// Determines whether the ITinyDictionary contains an element with the specified key.
        /// </summary>
        /// <returns>
        /// true if the ITinyDictionary contains an element with the key; otherwise, false.
        /// </returns>
        /// <param name="key">The key to locate in the ITinyDictionary.</param>
        bool ContainsKey(TKey key);

        /// <summary>
        /// Gets the number of elements contained in the ITinyDictionary.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the keys of the ITinyDictionary.
        /// </summary>
        ICollection<TKey> Keys { get; }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the values in the ITinyDictionary.
        /// </summary>
        ICollection<TValue> Values { get; }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator();

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <returns>
        /// true if the object that implements ITinyDictionary contains an element with the specified key; otherwise, false.
        /// </returns>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="value">When this method returns, the value associated with the specified key, if the key is found; otherwise, the default value for the type of the <paramref name="value"/> parameter. This parameter is passed uninitialized.</param>
        bool TryGetValue(TKey key, out TValue value);
    }
}
