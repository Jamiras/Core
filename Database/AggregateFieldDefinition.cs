using System.Diagnostics;

namespace Jamiras.Database
{
    [DebuggerDisplay("{Function}({ColumnName,nq})")]
    public struct AggregateFieldDefinition
    {
        public AggregateFieldDefinition(AggregateFunction function, string columnName)
            : this(function, columnName, null)
        {
        }

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

    public enum AggregateFunction
    {
        None = 0,
        Count,
        Distinct,
    }
}
