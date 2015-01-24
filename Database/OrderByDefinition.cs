using System.Diagnostics;

namespace Jamiras.Database
{
    [DebuggerDisplay("{ColumnName} {Order}")]
    public struct OrderByDefinition
    {
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

    public enum SortOrder
    {
        None = 0,
        Ascending,
        Descending,
    }
}
