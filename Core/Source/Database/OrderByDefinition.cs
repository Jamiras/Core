using System.Diagnostics;

namespace Jamiras.Database
{
    /// <summary>
    /// Specifies the order that queried data should be returned in.
    /// </summary>
    [DebuggerDisplay("{ColumnName} {Order}")]
    public struct OrderByDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OrderByDefinition"/> struct.
        /// </summary>
        /// <param name="columnName">Name of the column to order by.</param>
        /// <param name="order">How the data should be ordered.</param>
        public OrderByDefinition(string columnName, SortOrder order)
        {
            _columnName = columnName;
            _order = order;
        }

        private readonly string _columnName;
        private readonly SortOrder _order;

        /// <summary>
        /// Gets the column to sort on.
        /// </summary>
        public string ColumnName
        {
            get { return _columnName; }
        }

        /// <summary>
        /// Gets the type of sort to perform.
        /// </summary>
        public SortOrder Order
        {
            get { return _order; }
        }
    }

    /// <summary>
    /// How data should be ordered.
    /// </summary>
    public enum SortOrder
    {
        /// <summary>
        /// Unspecified.
        /// </summary>
        None = 0,

        /// <summary>
        /// Ascending order. (A->Z, 0->99)
        /// </summary>
        Ascending,

        /// <summary>
        /// Descending order. (Z->A, 99->0)
        /// </summary>
        Descending,
    }
}
