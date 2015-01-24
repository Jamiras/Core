using System.Diagnostics;
using System;

namespace Jamiras.Database
{
    [DebuggerDisplay("{ColumnName,nq} {Operation} {Value}")]
    public struct FilterDefinition
    {
        public FilterDefinition(string columnName, FilterOperation operation, object value, DataType dataType)
        {
            _columnName = columnName;
            _value = value;
            _operation = operation;
            _dataType = dataType;
        }

        public FilterDefinition(string columnName, FilterOperation operation, string value)
            : this(columnName, operation, value, DataType.String)
        {
            if (!String.IsNullOrEmpty(value) && value[0] == '@')
                _dataType = DataType.BindVariable;
        }

        public FilterDefinition(string columnName, FilterOperation operation, int value)
            : this(columnName, operation, value, DataType.Integer)
        {
        }

        public FilterDefinition(string columnName, FilterOperation operation, bool value)
            : this(columnName, operation, value, DataType.Boolean)
        {
        }

        public FilterDefinition(string columnName, FilterOperation operation, DateTime value)
            : this(columnName, operation, value, DataType.DateTime)
        {
        }

        public FilterDefinition(string columnName, FilterOperation operation, DateTime value, bool isDateOnly)
            : this(columnName, operation, value, isDateOnly ? DataType.Date : DataType.DateTime)
        {
        }

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

    public enum FilterOperation
    {
        None = 0,
        Equals,
        GreaterThan,
        LessThan,
        Like,
    }
}
