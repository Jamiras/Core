using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Jamiras.Components
{
    public static class EnumerableExtensions
    {
        private static bool TryGetCount<T>(IEnumerable<T> items, out int count)
        {
            var collectionT = items as ICollection<T>;
            if (collectionT != null)
            {
                count = collectionT.Count;
                return true;
            }

            var array = items as T[];
            if (array != null)
            {
                count = array.Length;
                return true;
            }

            var collection = items as ICollection;
            if (collection != null)
            {
                count = collection.Count;
                return true;
            }

            count = 0;
            return false;
        }

        /// <summary>
        /// Determine if another collection contains the same elements as the collection.
        /// </summary>
        /// <param name="compare">Collection to compare.</param>
        /// <returns><c>true</c> if the collection contains the same elements, <c>false</c> if not.</returns>
        public static bool Equivalent<T>(this IEnumerable<T> source, IEnumerable<T> compare)
        {
            int sourceCount, compareCount;
            if (TryGetCount(compare, out compareCount) && TryGetCount(source, out sourceCount) && sourceCount != compareCount)
                return false;

            List<T> compareItems;
            if (compareCount > 0)
                compareItems = new List<T>(compareCount);
            else
                compareItems = new List<T>();

            var compareEnumerator = compare.GetEnumerator();
            var sourceEnumerator = source.GetEnumerator();
            if (!sourceEnumerator.MoveNext())
                return (!compareEnumerator.MoveNext());

            var sourceFirst = sourceEnumerator.Current;
            do
            {
                if (!compareEnumerator.MoveNext())
                    return false;

                if (Equals(sourceFirst, compareEnumerator.Current))
                    break;

                compareItems.Add(compareEnumerator.Current);
            } while (true);

            while (compareEnumerator.MoveNext())
                compareItems.Add(compareEnumerator.Current);

            while (sourceEnumerator.MoveNext())
            {
                if (!compareItems.Remove(sourceEnumerator.Current))
                    return false;
            }

            return (compareItems.Count == 0);
        }
    }
}
