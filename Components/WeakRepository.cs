using System;
using System.Collections.Generic;

namespace Jamiras.Components
{
    /// <summary>
    /// Provides a base class for storing reusable data that can be regenerated on demand.
    /// </summary>
    /// <typeparam name="T">The type of object being stored.</typeparam>
    public abstract class WeakRepository<T>
        where T : class
    {
        /// <summary>
        /// Constructs a new WeakRepository.
        /// </summary>
        protected WeakRepository()
        {
            _items = new Dictionary<int, WeakReference>();
        }

        private readonly Dictionary<int, WeakReference> _items;

        /// <summary>
        /// Gets an item from the repository.
        /// </summary>
        /// <param name="id">Unique identifier of the item.</param>
        /// <returns>Requested item, <c>null</c> if item does not exist.</returns>
        public T Get(int id)
        {
            T item = GetOrAddItem(id, null);
            if (item == null)
            {
                item = BuildItem(id);
                if (item != null)
                    item = GetOrAddItem(id, item);
            }

            return item;
        }

        /// <summary>
        /// Inserts an item into the repository or returns the existing item if one is present.
        /// </summary>
        /// <param name="id">Unique identifier of the item.</param>
        /// <param name="item">The item to insert, or <c>null</c> if not inserting an item.</param>
        /// <returns>A reference to the item in the repository.</returns>
        protected T GetOrAddItem(int id, T item)
        {
            lock (_items)
            {
                WeakReference wr;
                if (_items.TryGetValue(id, out wr))
                {
                    var item2 = wr.Target as T;
                    if (item2 != null)
                        return item2;

                    wr.Target = item;
                }
                else if (item != null)
                {
                    _items[id] = new WeakReference(item);
                }
            }

            return item;
        }

        /// <summary>
        /// Generates an item for a unique identifier.
        /// </summary>
        /// <param name="id">Unique identifier of the item.</param>
        /// <returns>Generated item, or <c>null</c> if the item could not be generated.</returns>
        protected abstract T BuildItem(int id);
    }
}
