using Jamiras.Components;
using Jamiras.DataModels.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jamiras.Database
{
    /// <summary>
    /// Class to facilitate in constructing database agnostic queries.
    /// </summary>
    public class QueryBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueryBuilder"/> class.
        /// </summary>
        public QueryBuilder(DatabaseSchema schema)
        {
            _schema = schema ?? ServiceRepository.Instance.FindService<IDatabase>().Schema;
            _fields = new List<string>();
            _filters = new List<FilterDefinition>();
            _joins = new List<JoinDefinition>();
            _orderBy = new List<OrderByDefinition>();
            _aliases = new List<AliasDefinition>();
            _aggregateFields = new List<AggregateFieldDefinition>();
            _values = new Dictionary<string, object>();
        }

        private readonly DatabaseSchema _schema;
        private readonly List<string> _fields;
        private readonly List<FilterDefinition> _filters;
        private readonly List<JoinDefinition> _joins;
        private readonly List<OrderByDefinition> _orderBy;
        private readonly List<AliasDefinition> _aliases;
        private readonly List<AggregateFieldDefinition> _aggregateFields;
        private readonly Dictionary<string, object> _values;
        private Dictionary<string, string> _bindings;
        private string _filterExpression;

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        public override string ToString()
        {
            if (_values.Count == 0)
                return BuildQueryString();

            if (_filters.Count == 0)
                return BuildInsertString();

            return BuildUpdateString();
        }

        #region BuildQueryString

        private static readonly string[] ReservedWords = { "when" };// "user", "session", "when", "size", "zone" };


        public string BuildQueryString()
        {
            var tables = GetTables();

            var builder = new StringBuilder();
            builder.Append("SELECT ");
            AppendQueryFields(builder);
            builder.Append(" FROM ");
            AppendJoinTree(builder, tables);
            builder.Append(" WHERE ");

            bool wherePresent = AppendFilters(builder);
            if (!wherePresent)
                builder.Length -= 7;

            AppendOrderBy(builder);

            return builder.ToString();
        }

        public string BuildInsertString()
        {
            if (_values.Count == 0)
                throw new NotSupportedException("No values provided");

            if (_bindings != null)
                _bindings.Clear();

            var builder = new StringBuilder();
            builder.Append("INSERT INTO ");

            var tableName = _values.First().Key;
            var index = tableName.IndexOf('.');
            if (index == -1)
                throw new NotSupportedException("Could not extract table name from value map");
            builder.Append(tableName, 0, index);
            tableName = tableName.Substring(0, index + 1);

            builder.Append(" (");

            foreach (var value in _values)
            {
                if (!value.Key.StartsWith(tableName))
                    throw new NotSupportedException("Cannot update multiple tables");

                builder.Append(value.Key, tableName.Length, value.Key.Length - tableName.Length);
                builder.Append(", ");
            }
            builder.Length -= 2;

            builder.Append(") VALUES (");

            TableSchema tableSchema = null;
            if (_schema != null)
                tableSchema = _schema.GetTableSchema(tableName.Substring(0, index));

            foreach (var value in _values)
            {
                if (tableSchema != null)
                {
                    var columnName = value.Key.Substring(index + 1);
                    var column = tableSchema.Columns.FirstOrDefault(c => c.FieldName == columnName);
                    AppendValue(builder, value.Value, column is ForeignKeyFieldMetadata);
                }
                else
                {
                    AppendValue(builder, value.Value, false);
                }
                builder.Append(", ");
            }
            builder.Length -= 2;
            builder.Append(')');

            return builder.ToString();
        }

        public string BuildUpdateString()
        {
            if (_values.Count == 0)
                throw new NotSupportedException("No values provided");

            if (_bindings != null)
                _bindings.Clear();

            var builder = new StringBuilder();
            builder.Append("UPDATE ");

            var tableName = _values.First().Key;
            var index = tableName.IndexOf('.');
            if (index == -1)
                throw new NotSupportedException("Could not extract table name from value map");
            builder.Append(tableName, 0, index);
            tableName = tableName.Substring(0, index + 1);

            builder.Append(" SET ");

            TableSchema tableSchema = null;
            if (_schema != null)
                tableSchema = _schema.GetTableSchema(tableName.Substring(0, index));

            foreach (var value in _values)
            {
                if (!value.Key.StartsWith(tableName))
                    throw new NotSupportedException("Cannot update multiple tables");

                builder.Append(value.Key, tableName.Length, value.Key.Length - tableName.Length);
                builder.Append('=');

                if (tableSchema != null)
                {
                    var column = tableSchema.Columns.FirstOrDefault(c => c.FieldName == value.Key);
                    AppendValue(builder, value.Value, column is ForeignKeyFieldMetadata);
                }
                else
                {
                    AppendValue(builder, value.Value, false);
                }
                builder.Append(", ");
            }
            builder.Length -= 2;

            builder.Append(" WHERE ");
            AppendFilters(builder);

            return builder.ToString();
        }

        private List<string> GetTables()
        {
            var tables = new List<string>();
            foreach (var field in Fields)
                AddTable(tables, field);

            foreach (var filter in Filters)
                AddTable(tables, filter.ColumnName);

            foreach (var join in Joins)
            {
                AddTable(tables, join.LocalKeyFieldName);
                AddTable(tables, join.RemoteKeyFieldName);
            }

            foreach (var orderBy in OrderBy)
                AddTable(tables, orderBy.ColumnName);

            return tables;
        }

        private static void AddTable(List<string> tables, string field)
        {
            int idx = field.IndexOf('(');
            if (idx != -1)
                field = field.Substring(idx + 1);

            idx = field.IndexOf('.');
            if (idx > 0)
            {
                string table = field.Substring(0, idx);
                if (!tables.Contains(table))
                    tables.Add(table);
            }
        }

        private void AppendQueryFields(StringBuilder builder)
        {
            foreach (var field in Fields)
            {
                AppendFieldName(builder, field);
                builder.Append(", ");
            }
            builder.Length -= 2;
        }

        private static bool IsFieldForTable(string fieldName, string tableName)
        {
            if (String.Compare(fieldName, 0, tableName, 0, tableName.Length, StringComparison.OrdinalIgnoreCase) != 0)
                return false;

            return (fieldName[tableName.Length] == '.');
        }

        private void AppendJoinTree(StringBuilder builder, List<string> tables)
        {
            string primaryTable = tables[0];
            if (tables.Count == 1)
            {
                AppendTable(builder, primaryTable);
                return;
            }

            tables.RemoveAt(0);
            for (int i = 1; i < tables.Count; i++)
                builder.Append('(');

            AppendTable(builder, primaryTable);

            var joins = new List<JoinDefinition>(Joins);
            if (_schema != null)
            {
                for (int i = 0; i < tables.Count; i++)
                {
                    var tableName = tables[i];

                    var join = joins.FirstOrDefault(j => IsFieldForTable(j.RemoteKeyFieldName, tableName));
                    if (join.JoinType == JoinType.None)
                    {
                        var alias = Aliases.FirstOrDefault(a => a.Alias == tableName);
                        if (!String.IsNullOrEmpty(alias.TableName))
                            tableName = alias.TableName;

                        join = _schema.GetJoin(primaryTable, tableName);
                        if (join.JoinType == JoinType.None)
                            throw new InvalidOperationException("No join defined between " + primaryTable + " and " + tableName);

                        joins.Add(join);
                    }
                }
            }

            foreach (var join in joins)
            {
                var fieldName = join.RemoteKeyFieldName;
                int idx = fieldName.IndexOf('.');
                if (idx > 0)
                {
                    string joinFieldName = join.LocalKeyFieldName;
                    string table = fieldName.Substring(0, idx);
                    if (table == primaryTable)
                    {
                        joinFieldName = fieldName;
                        fieldName = join.LocalKeyFieldName;
                        idx = fieldName.IndexOf('.');
                        if (idx > 0)
                            table = fieldName.Substring(0, idx);
                    }

                    idx = tables.IndexOf(table);
                    if (idx >= 0)
                    {
                        tables.RemoveAt(idx);

                        if (join.JoinType == JoinType.Outer)
                            builder.Append(" LEFT OUTER JOIN ");
                        else if (join.JoinType == JoinType.Inner)
                            builder.Append(" INNER JOIN ");
                        else
                            throw new InvalidOperationException("Unsupported join type: " + join.JoinType);

                        AppendTable(builder, table);
                        builder.Append(" ON ");
                        AppendFieldName(builder, fieldName);
                        builder.Append('=');
                        AppendFieldName(builder, joinFieldName);

                        if (tables.Count > 0)
                            builder.Append(')');
                    }
                }
            }

            if (tables.Count > 0)
                throw new InvalidOperationException("No join defined between " + primaryTable + " and " + tables[0]);
        }

        private void AppendTable(StringBuilder builder, string tableName)
        {
            foreach (var alias in Aliases)
            {
                if (alias.Alias == tableName)
                {
                    builder.Append(alias.TableName);
                    builder.Append(" AS ");
                    builder.Append(alias.Alias);
                    return;
                }
            }

            builder.Append(tableName);
        }

        private bool AppendFilters(StringBuilder builder)
        {
            if (Filters.Count == 0)
                return false;

            if (Filters.Count == 1)
            {
                foreach (var filter in Filters)
                    AppendFilter(builder, filter);

                return true;
            }

            var filterExpression = FilterExpression;

            int idx = 0;
            while (idx < filterExpression.Length)
            {
                int val = 0;
                while (idx < filterExpression.Length)
                {
                    char c = filterExpression[idx++];
                    if (c == '&')
                    {
                        builder.Append(" AND ");
                    }
                    else if (c == '|')
                    {
                        builder.Append(" OR ");
                    }
                    else if (Char.IsDigit(c))
                    {
                        val = c - '0';
                        break;
                    }
                    else
                    {
                        builder.Append(c);
                    }
                }

                while (idx < filterExpression.Length && Char.IsDigit(filterExpression[idx]))
                {
                    val *= 10;
                    val += (filterExpression[idx++] - '0');
                }

                if (val > 0)
                {
                    var filter = Filters.ElementAt(val - 1);
                    AppendFilter(builder, filter);
                }
            }

            return true;
        }

        private void AppendFilter(StringBuilder builder, FilterDefinition filter)
        {
            AppendFieldName(builder, filter.ColumnName);

            if (filter.Value == null)
            {
                switch (filter.Operation)
                {
                    case FilterOperation.Equals:
                        builder.Append(" IS NULL");
                        break;

                    case FilterOperation.NotEquals:
                        builder.Append(" IS NOT NULL");
                        break;

                    default:
                        throw new InvalidOperationException("Unsupported comparison to null: " + filter.Operation);
                }
                return;
            }

            switch (filter.Operation)
            {
                case FilterOperation.Like:
                    builder.Append(" LIKE ");
                    break;

                case FilterOperation.LessThan:
                    builder.Append('<');
                    break;

                case FilterOperation.GreaterThan:
                    builder.Append('>');
                    break;

                case FilterOperation.Equals:
                    builder.Append('=');
                    break;

                case FilterOperation.NotEquals:
                    builder.Append("<>");
                    break;

                default:
                    throw new InvalidOperationException("Unsupported filter operation: " + filter.Operation);
            }

            switch (filter.DataType)
            {
                case DataType.BindVariable:
                    builder.Append((string)filter.Value);
                    break;

                case DataType.Boolean:
                    AppendBoolean(builder, (bool)filter.Value);
                    break;

                case DataType.Date:
                    if (filter.Value is DateTime)
                    {
                        var dttm = (DateTime)filter.Value;
                        AppendDate(builder, new Date(dttm.Month, dttm.Day, dttm.Year));
                    }
                    else
                    {
                        AppendDate(builder, (Date)filter.Value);
                    }
                    break;

                case DataType.DateTime:
                    AppendDateTime(builder, (DateTime)filter.Value);
                    break;

                case DataType.Integer:
                    builder.Append((int)filter.Value);
                    break;

                case DataType.String:
                    builder.Append('\'');
                    builder.Append(ServiceRepository.Instance.FindService<IDatabase>().Escape((string)filter.Value));
                    builder.Append('\'');
                    break;

                default:
                    throw new InvalidOperationException("Unsupported data type: " + filter.DataType);
            }
        }

        private void AppendValue(StringBuilder builder, object value, bool isForeignKey)
        {
            if (value == null)
            {
                builder.Append("NULL");
            }
            else if (value is int || value.GetType().IsEnum)
            {
                var iVal = (int)value;
                if (iVal == 0 && isForeignKey)
                    builder.Append("NULL");
                else
                    builder.Append(iVal);
            }
            else if (value is string)
            {
                var sVal = (string)value;
                if (sVal.Length == 0)
                    builder.Append("NULL");
                else
                    builder.Append(AddBinding(sVal));
            }
            else if (value is double)
            {
                var dVal = (double)value;
                builder.Append(dVal);
            }
            else if (value is float)
            {
                var dVal = (float)value;
                builder.Append(dVal);
            }
            else if (value is DateTime)
            {
                AppendDateTime(builder, (DateTime)value);
            }
            else if (value is Date)
            {
                var date = (Date)value;
                if (date.IsEmpty)
                    builder.Append("NULL");
                else
                    AppendDate(builder, (Date)value);
            }
            else if (value is bool)
            {
                AppendBoolean(builder, (bool)value);
            }
            else
            {
                throw new NotSupportedException(value.GetType().Name);
            }
        }

        protected virtual void AppendBoolean(StringBuilder builder, bool value)
        {
            builder.Append(value ? "1" : "0");
        }

        protected virtual void AppendDateTime(StringBuilder builder, DateTime value)
        {
            builder.AppendFormat("'{0:D4}-{1:D2}-{2:D2} {3:D2}:{4:D2}:{5:D2}'", value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second);
        }

        protected virtual void AppendDate(StringBuilder builder, Date value)
        {
            builder.AppendFormat("'{0:D4}-{1:D2}-{2:D2}'", value.Year, value.Month, value.Day);
        }

        private string AddBinding(string value)
        {
            if (_bindings == null)
                _bindings = new Dictionary<string, string>();

            var key = "@" + (_bindings.Count + 1);
            _bindings[key] = value;
            return key;
        }

        private static void AppendFieldName(StringBuilder builder, string fieldName)
        {
            if (fieldName.IndexOf('(') != -1)
            {
                builder.Append(fieldName);
                return;
            }    

            int idx = fieldName.IndexOf('.');
            if (idx > 0)
            {
                builder.Append(fieldName, 0, idx + 1);
                fieldName = fieldName.Substring(idx + 1);
            }

            foreach (var reservedWord in ReservedWords)
            {
                if (fieldName.Equals(reservedWord, StringComparison.InvariantCultureIgnoreCase))
                {
                    builder.Append('[');
                    builder.Append(fieldName);
                    builder.Append(']');
                    return;
                }
            }

            builder.Append(fieldName);
        }

        private void AppendOrderBy(StringBuilder builder)
        {
            if (OrderBy.Count > 0)
            {
                builder.Append(" ORDER BY ");

                foreach (var orderBy in OrderBy)
                {
                    builder.Append(orderBy.ColumnName);

                    if (orderBy.Order == SortOrder.Descending)
                        builder.Append(" DESC");

                    builder.Append(", ");
                }

                builder.Length -= 2;
            }
        }

        #endregion

        #region Bind

        public void Bind(IDatabaseCommand command)
        {
            if (_bindings != null)
            {
                foreach (var binding in _bindings)
                    command.BindString(binding.Key, binding.Value);
            }
        }

        #endregion

        /// <summary>
        /// Gets the collection of fields to return from the query.
        /// </summary>
        public ICollection<string> Fields
        {
            get { return _fields; }
        }

        /// <summary>
        /// Gets the collection of filters to apply to the query.
        /// </summary>
        public ICollection<FilterDefinition> Filters
        {
            get { return _filters; }
        }

        /// <summary>
        /// Gets the collection of joins required to perform the query.
        /// </summary>
        public ICollection<JoinDefinition> Joins
        {
            get { return _joins; }
        }

        /// <summary>
        /// Gets the collection of sorts to apply to the results.
        /// </summary>
        public ICollection<OrderByDefinition> OrderBy
        {
            get { return _orderBy; }
        }

        /// <summary>
        /// Gets the collection of aliases used in the query.
        /// </summary>
        public ICollection<AliasDefinition> Aliases
        {
            get { return _aliases; }
        }

        /// <summary>
        /// Gets the collection of aggregate fields to return from the query.
        /// </summary>
        public ICollection<AggregateFieldDefinition> AggregateFields
        {
            get { return _aggregateFields; }
        }

        /// <summary>
        /// Defines the logical expression to apply to the filters. For example (1|2)&amp;3
        /// </summary>
        public string FilterExpression
        {
            get { return _filterExpression ?? BuildDefaultFilterExpression(); }
            set { _filterExpression = value; }
        }

        private string BuildDefaultFilterExpression()
        {
            if (_filters.Count == 1)
                return "1";
            if (_filters.Count == 0)
                return String.Empty;

            var builder = new StringBuilder();
            builder.Append('1');
            for (int i = 1; i < _filters.Count; i++)
            {
                builder.Append('&');
                builder.Append(i + 1);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Gets the collection of values to update.
        /// </summary>
        public IDictionary<string, object> Values
        {
            get { return _values; }
        }
    }
}
