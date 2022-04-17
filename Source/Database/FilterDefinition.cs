using System;
using System.Diagnostics;

namespace Jamiras.Database
{
    /// <summary>
    /// Defines a query filter.
    /// </summary>
    [DebuggerDisplay("{ColumnName,nq} {Operation} {Value}")]
    public struct FilterDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FilterDefinition"/> struct.
        /// </summary>
        /// <param name="columnName">The column to filter on.</param>
        /// <param name="operation">The filter operation.</param>
        /// <param name="value">The filter value.</param>
        /// <param name="dataType">Type of the data in the column.</param>
        public FilterDefinition(string columnName, FilterOperation operation, object value, DataType dataType)
        {
            _columnName = columnName;
            _value = value;
            _operation = operation;
            _dataType = dataType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterDefinition"/> struct.
        /// </summary>
        /// <param name="columnName">The column to filter on.</param>
        /// <param name="operation">The filter operation.</param>
        /// <param name="value">The filter value.</param>
        public FilterDefinition(string columnName, FilterOperation operation, string value)
            : this(columnName, operation, value, DataType.String)
        {
            if (!String.IsNullOrEmpty(value) && value[0] == '@')
                _dataType = DataType.BindVariable;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterDefinition"/> struct.
        /// </summary>
        /// <param name="columnName">The column to filter on.</param>
        /// <param name="operation">The filter operation.</param>
        /// <param name="value">The filter value.</param>
        public FilterDefinition(string columnName, FilterOperation operation, int value)
            : this(columnName, operation, value, DataType.Integer)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterDefinition"/> struct.
        /// </summary>
        /// <param name="columnName">The column to filter on.</param>
        /// <param name="operation">The filter operation.</param>
        /// <param name="value">The filter value.</param>
        public FilterDefinition(string columnName, FilterOperation operation, bool value)
            : this(columnName, operation, value, DataType.Boolean)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterDefinition"/> struct.
        /// </summary>
        /// <param name="columnName">The column to filter on.</param>
        /// <param name="operation">The filter operation.</param>
        /// <param name="value">The filter value.</param>
        public FilterDefinition(string columnName, FilterOperation operation, DateTime value)
            : this(columnName, operation, value, DataType.DateTime)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterDefinition"/> struct.
        /// </summary>
        /// <param name="columnName">The column to filter on.</param>
        /// <param name="operation">The filter operation.</param>
        /// <param name="value">The filter value.</param>
        public FilterDefinition(string columnName, FilterOperation operation, Enum value)
            : this(columnName, operation, value, DataType.Integer)
        {
        }

        private readonly string _columnName;
        private readonly object _value;
        private readonly FilterOperation _operation;
        private readonly DataType _dataType;

        /// <summary>
        /// Gets the column name to filter on.
        /// </summary>
        public string ColumnName
        {
            get { return _columnName; }
        }

        /// <summary>
        /// Gets the value to filter on.
        /// </summary>
        public object Value
        {
            get { return _value; }
        }

        /// <summary>
        /// Gets the operation to perform when evaluating the filter.
        /// </summary>
        public FilterOperation Operation
        {
            get { return _operation; }
        }

        /// <summary>
        /// Gets the type of data stored in <see cref="Value"/>.
        /// </summary>
        public DataType DataType
        {
            get { return _dataType; }
        }
    }

    /// <summary>
    /// Operation to perform when evaluating a filter.
    /// </summary>
    public enum FilterOperation
    {
        /// <summary>
        /// Unspecified.
        /// </summary>
        None = 0,

        /// <summary>
        /// Value equals the search criteria.
        /// </summary>
        Equals,

        /// <summary>
        /// Value doesn't equal the search criteria.
        /// </summary>
        NotEquals,

        /// <summary>
        /// Value is greater than the search criteria.
        /// </summary>
        GreaterThan,

        /// <summary>
        /// Value is less than the search criteria.
        /// </summary>
        LessThan,

        /// <summary>
        /// Value is similar to the search criteria.
        /// </summary>
        Like,
    }
}
