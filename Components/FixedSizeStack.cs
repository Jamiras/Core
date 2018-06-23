using System.Collections.Generic;
using System.Diagnostics;

namespace Jamiras.Components
{
    /// <summary>
    /// A stack implementation with a fixed capacity.
    /// </summary>
    [DebuggerDisplay("Count = {Count}")]
    public class FixedSizeStack<T> : IEnumerable<T>
    {
        /// <summary>
        /// Constructs a new FixedSizeStack
        /// </summary>
        public FixedSizeStack(int capacity)
        {
            _buffer = new T[capacity];
        }

        private readonly T[] _buffer;
        private int _firstIndex;
        private int _count;

        /// <summary>
        /// Gets the number of items in the collection.
        /// </summary>
        public int Count 
        {
            get { return _count; } 
        }

        /// <summary>
        /// Gets a value indicating whether this instance is empty.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is empty; otherwise, <c>false</c>.
        /// </value>
        public bool IsEmpty
        {
            get { return _count == 0; }
        }

        /// <summary>
        /// Gets the maximum number of items the collection will hold.
        /// </summary>
        public int Capacity 
        {
            get { return _buffer.Length; }
        }

        /// <summary>
        /// Pushes an item onto the stack.
        /// </summary>
        /// <remarks>If the collection is full (<see cref="Count"/>==<see cref="Capacity"/>), the least recent item is lost.</remarks>
        public void Push(T item)
        {
            if (_count > 0)
            {
                _firstIndex++;
                if (_firstIndex == _buffer.Length)
                    _firstIndex = 0;
            }

            _buffer[_firstIndex] = item;

            if (_count < _buffer.Length)
                _count++;
        }

        /// <summary>
        /// Peeks at the top item in the stack.
        /// </summary>
        public T Peek()
        {
            if (_count == 0)
                return default(T);

            return _buffer[_firstIndex];
        }

        /// <summary>
        /// Pops na item off the stack.
        /// </summary>
        /// <returns></returns>
        public T Pop()
        {
            if (_count == 0)
                return default(T);

            var item = _buffer[_firstIndex];
            _buffer[_firstIndex] = default(T); // allow item to be GC'd (if applicable)

            if (_firstIndex == 0)
                _firstIndex = _buffer.Length - 1;
            else
                _firstIndex--;

            _count--;

            return item;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            int index = _firstIndex;
            for (int i = 0; i < _count; i++)
            {
                yield return _buffer[index];
                if (index == 0)
                    index = _buffer.Length - 1;
                else
                    index--;
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
