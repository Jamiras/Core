using System.Diagnostics;

namespace Jamiras.Database
{
    /// <summary>
    /// Defines a query field that aggregates the results.
    /// </summary>
    [DebuggerDisplay("{Function}({ColumnName,nq})")]
    public struct AggregateFieldDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateFieldDefinition"/> struct.
        /// </summary>
        /// <param name="function">The function to use to aggregate the data.</param>
        /// <param name="columnName">Name of the column to aggregate on.</param>
        public AggregateFieldDefinition(AggregateFunction function, string columnName)
            : this(function, columnName, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateFieldDefinition"/> struct.
        /// </summary>
        /// <param name="function">The function to use to aggregate the data.</param>
        /// <param name="columnName">Name of the column to aggregate on.</param>
        /// <param name="groupByColumnNames">List of columns to group data by before aggregating.</param>
        public AggregateFieldDefinition(AggregateFunction function, string columnName, string[] groupByColumnNames)
        {
            _function = function;
            _columnName = columnName;
            _groupByColumnNames = groupByColumnNames;
        }

        private readonly AggregateFunction _function;
        private readonly string _columnName;
        private readonly string[] _groupByColumnNames;

        /// <summary>
        /// Gets the function to apply to the column.
        /// </summary>
        public AggregateFunction Function
        {
            get { return _function; }
        }

        /// <summary>
        /// Gets the column to apply the function to.
        /// </summary>
        public string ColumnName
        {
            get { return _columnName; }
        }

        /// <summary>
        /// Gets the column to group results by.
        /// </summary>
        public string[] GroupByColumnName
        {
            get { return _groupByColumnNames; }
        }
    }

    /// <summary>
    /// Method to use when aggregating data.
    /// </summary>
    public enum AggregateFunction
    {
        /// <summary>
        /// Unspecified
        /// </summary>
        None = 0,

        /// <summary>
        /// Counts values matching the group by criteria.
        /// </summary>
        Count,

        /// <summary>
        /// Only returns unique rows.
        /// </summary>
        Distinct,
    }
}
