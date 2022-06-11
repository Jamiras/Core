﻿using Jamiras.Components;
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
        public QueryBuilder()
        {
            _fields = new List<string>();
            _filters = new List<FilterDefinition>();
            _joins = new List<JoinDefinition>();
            _orderBy = new List<OrderByDefinition>();
            _aliases = new List<AliasDefinition>();
            _aggregateFields = new List<AggregateFieldDefinition>();
        }

        private readonly List<string> _fields;
        private readonly List<FilterDefinition> _filters;
        private readonly List<JoinDefinition> _joins;
        private readonly List<OrderByDefinition> _orderBy;
        private readonly List<AliasDefinition> _aliases;
        private readonly List<AggregateFieldDefinition> _aggregateFields;
        private string _filterExpression;

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        public override string ToString()
        {
            return BuildQueryString(this, null);
        }

        #region BuildQueryString

        private static readonly string[] ReservedWords = { };// "user", "session", "when", "size", "zone" };


        public static string BuildQueryString(QueryBuilder query, DatabaseSchema schema)
        {
            if (schema == null)
                schema = ServiceRepository.Instance.FindService<IDatabase>().Schema;

            var tables = GetTables(query);

            var builder = new StringBuilder();
            builder.Append("SELECT ");
            AppendQueryFields(builder, query);
            builder.Append(" FROM ");
            AppendJoinTree(builder, query, tables, schema);
            builder.Append(" WHERE ");

            bool wherePresent = AppendFilters(builder, query);
            if (!wherePresent)
                builder.Length -= 7;

            AppendOrderBy(builder, query);

            return builder.ToString();
        }

        private static List<string> GetTables(QueryBuilder query)
        {
            var tables = new List<string>();
            foreach (var field in query.Fields)
                AddTable(tables, field);

            foreach (var filter in query.Filters)
                AddTable(tables, filter.ColumnName);

            foreach (var join in query.Joins)
            {
                AddTable(tables, join.LocalKeyFieldName);
                AddTable(tables, join.RemoteKeyFieldName);
            }

            foreach (var orderBy in query.OrderBy)
                AddTable(tables, orderBy.ColumnName);

            return tables;
        }

        private static void AddTable(List<string> tables, string field)
        {
            int idx = field.IndexOf('.');
            if (idx > 0)
            {
                string table = field.Substring(0, idx);
                if (!tables.Contains(table))
                    tables.Add(table);
            }
        }

        private static void AppendQueryFields(StringBuilder builder, QueryBuilder query)
        {
            foreach (var field in query.Fields)
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

        private static void AppendJoinTree(StringBuilder builder, QueryBuilder query, List<string> tables, DatabaseSchema schema)
        {
            string primaryTable = tables[0];
            if (tables.Count == 1)
            {
                AppendTable(builder, query, primaryTable);
                return;
            }

            tables.RemoveAt(0);
            for (int i = 1; i < tables.Count; i++)
                builder.Append('(');

            AppendTable(builder, query, primaryTable);

            var joins = new List<JoinDefinition>(query.Joins);
            if (schema != null)
            {
                for (int i = 0; i < tables.Count; i++)
                {
                    var tableName = tables[i];

                    var join = joins.FirstOrDefault(j => IsFieldForTable(j.RemoteKeyFieldName, tableName));
                    if (join.JoinType == JoinType.None)
                    {
                        var alias = query.Aliases.FirstOrDefault(a => a.Alias == tableName);
                        if (!String.IsNullOrEmpty(alias.TableName))
                            tableName = alias.TableName;

                        join = schema.GetJoin(primaryTable, tableName);
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

                        AppendTable(builder, query, table);
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

        private static void AppendTable(StringBuilder builder, QueryBuilder query, string tableName)
        {
            foreach (var alias in query.Aliases)
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

        private static bool AppendFilters(StringBuilder builder, QueryBuilder query)
        {
            if (query.Filters.Count == 0)
                return false;

            if (query.Filters.Count == 1)
            {
                foreach (var filter in query.Filters)
                    AppendFilter(builder, filter);

                return true;
            }

            var filterExpression = query.FilterExpression;

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
                    var filter = query.Filters.ElementAt(val - 1);
                    AppendFilter(builder, filter);
                }
            }

            return true;
        }

        private static void AppendFilter(StringBuilder builder, FilterDefinition filter)
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
                    if ((bool)filter.Value)
                        builder.Append("YES");
                    else
                        builder.Append("NO");
                    break;

                case DataType.Date:
                    builder.AppendFormat("#{0}#", ((DateTime)filter.Value).ToShortDateString());
                    break;

                case DataType.DateTime:
                    builder.AppendFormat("#{0}#", (DateTime)filter.Value);
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

        private static void AppendFieldName(StringBuilder builder, string fieldName)
        {
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

        private static void AppendOrderBy(StringBuilder builder, QueryBuilder query)
        {
            if (query.OrderBy.Count > 0)
            {
                builder.Append(" ORDER BY ");

                foreach (var orderBy in query.OrderBy)
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
    }
}
