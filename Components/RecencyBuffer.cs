using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Jamiras.Components
{
    /// <summary>
    /// A collection of items where order implies recency.
    /// </summary>
    [DebuggerDisplay("Count = {Count}")]
    public class RecencyBuffer<T> : IEnumerable<T>
    {
        /// <summary>
        /// Constructs a new RecencyBuffer
        /// </summary>
        public RecencyBuffer(int capacity)
        {
            _buffer = new T[capacity];
        }

        private readonly T[] _buffer;
        private int _firstIndex, _stopIndex;
        private int _count;

        /// <summary>
        /// Gets the number of items in the collection.
        /// </summary>
        public int Count 
        {
            get { return _count; } 
        }
        
        /// <summary>
        /// Gets the maximum number of items the collection will hold.
        /// </summary>
        public int Capacity 
        {
            get { return _buffer.Length; }
        }

        /// <summary>
        /// Adds an item to the collection as the most recent item.
        /// </summary>
        /// <remarks>If the collection is full (<see cref="Count"/>==<see cref="Capacity"/>), the least recent item is lost.</remarks>
        public void Add(T item)
        {
            if (_count == 0)
            {
                _firstIndex = Capacity - 1;
                _stopIndex = 0;
            }
            else
            {
                _firstIndex = GetPreviousIndex(_firstIndex);
            }

            _buffer[_firstIndex] = item;

            if (_count == Capacity)
                _stopIndex = _firstIndex;
            else
                _count++;
        }

        /// <summary>
        /// Removes the specified item from the collection.
        /// </summary>
        /// <returns><c>true</c> if the item was removed, <c>false</c> if not.</returns>
        public bool Remove(T item)
        {
            return Remove(i => Equals(i, item));
        }

        /// <summary>
        /// Removes an item matching the provided predicate from the collection.
        /// </summary>
        /// <returns><c>true</c> if the item was removed, <c>false</c> if not.</returns>
        public bool Remove(Predicate<T> matchPredicate)
        {
            int index = FindIndex(matchPredicate);
            if (index < 0)
                return false;

            if (index == _firstIndex)
            {
                _buffer[_firstIndex] = default(T);
                _firstIndex = GetNextIndex(_firstIndex);
            }
            else
            {
                _stopIndex = GetPreviousIndex(_stopIndex);

                while (index != _stopIndex)
                {
                    int nextIndex = GetNextIndex(index);
                    _buffer[index] = _buffer[nextIndex];
                    index = nextIndex;
                }

                _buffer[_stopIndex] = default(T);
            }

            _count--;
            return true;
        }

        /// <summary>
        /// Determines if an item matching the provided predicate exists in the collection.
        /// </summary>
        /// <returns><c>true</c> if the item was found, <c>false</c> if not.</returns>
        public bool Contains(Predicate<T> matchPredicate)
        {
            return (FindIndex(matchPredicate) >= 0);
        }

        /// <summary>
        /// Gets the item matching the provided predicate and moves it to be the most recent item.
        /// </summary>
        public T FindAndMakeRecent(Predicate<T> matchPredicate)
        {
            int index = FindIndex(matchPredicate);
            if (index < 0)
                return default(T);

            T item = _buffer[index];

            if (index != _firstIndex)
            {
                do
                {
                    int previousIndex = GetPreviousIndex(index);
                    _buffer[index] = _buffer[previousIndex];
                    index = previousIndex;
                } while (index != _firstIndex);
                
                _buffer[index] = item;
            }

            return item;
        }

        private int FindIndex(Predicate<T> matchPredicate)
        {
            if (_count > 0)
            {
                foreach (int index in GetIndices())
                {
                    if (matchPredicate(_buffer[index]))
                        return index;
                }
            }

            return -1;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            foreach (var index in GetIndices())
                yield return _buffer[index];
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private IEnumerable<int> GetIndices()
        {
            if (_count > 0)
            {
                int index = _firstIndex;
                do
                {
                    yield return index;

                    index = GetNextIndex(index);
                } while (index != _stopIndex);
            }
        }

        private int GetNextIndex(int index)
        {
            index++;
            return (index < Capacity) ? index : 0;
        }

        private int GetPreviousIndex(int index)
        {
            return (index == 0) ? Capacity - 1 : index - 1;
        }
    }
}
