using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Jamiras.DataModels
{
    public class ModelQueryExpression
    {
        public ModelQueryExpression()
        {
            _queryFields = new List<string>();
            _filters = new List<KeyValuePair<string, string>>();
        }

        private readonly List<string> _queryFields;
        private readonly List<KeyValuePair<string, string>> _filters;
        private List<JoinData> _joins;
        private List<string> _orderBy;
        private string _filterExpression;

        [DebuggerDisplay("{LocalKeyFieldName} => {RemoteKeyFieldName}")]
        private class JoinData
        {
            public JoinData(string localKeyFieldName, string remoteKeyFieldName, bool useOuterJoin)
            {
                LocalKeyFieldName = localKeyFieldName;
                RemoteKeyFieldName = remoteKeyFieldName;
                UseOuterJoin = useOuterJoin;
            }

            public string LocalKeyFieldName { get; private set; }
            public string RemoteKeyFieldName { get; private set; }
            public bool UseOuterJoin { get; private set; }
        }

        public void AddQueryField(string fieldName)
        {
            _queryFields.Add(fieldName);
        }

        public void AddFilter(string fieldName, string bindVariable)
        {
            _filters.Add(new KeyValuePair<string, string>(fieldName, bindVariable));
        }

        public void AddLikeFilter(string fieldName, string bindVariable)
        {
            _filters.Add(new KeyValuePair<string, string>(fieldName, '~' + bindVariable));
        }

        public void AddJoin(string localKeyFieldName, string remoteKeyFieldName, bool useOuterJoin)
        {
            if (_joins == null)
                _joins = new List<JoinData>();

            _joins.Add(new JoinData(localKeyFieldName, remoteKeyFieldName, useOuterJoin));
        }

        public void AddOrderBy(string fieldName)
        {
            if (_orderBy == null)
                _orderBy = new List<string>();

            _orderBy.Add(fieldName);
        }

        public void SetFilterExpression(string filterExpression)
        {
            _filterExpression = filterExpression;
        }

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

        private List<string> GetTables()
        {
            List<string> tables = new List<string>();
            foreach (var field in _queryFields)
            {
                int idx = field.IndexOf('.');
                if (idx > 0)
                {
                    string table = field.Substring(0, idx);
                    if (!tables.Contains(table))
                        tables.Add(table);
                }
            }

            foreach (var filter in _filters)
            {
                var field = filter.Key;
                int idx = field.IndexOf('.');
                if (idx > 0)
                {
                    string table = field.Substring(0, idx);
                    if (!tables.Contains(table))
                        tables.Add(table);
                }
            }

            return tables;
        }

        private void AppendQueryFields(StringBuilder builder)
        {
            foreach (var field in _queryFields)
            {
                builder.Append(field);
                builder.Append(", ");
            }
            builder.Length -= 2;
        }

        private void AppendJoinTree(StringBuilder builder, List<string> tables)
        {
            string primaryTable = tables[0];
            if (tables.Count == 1)
            {
                builder.Append(primaryTable);
                return;
            }

            tables.RemoveAt(0);
            for (int i = 1; i < tables.Count; i++)
                builder.Append('(');

            builder.Append(primaryTable);

            if (_joins != null)
            {
                foreach (var join in _joins)
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

                            if (join.UseOuterJoin)
                                builder.Append(" LEFT OUTER JOIN ");
                            else
                                builder.Append(" INNER JOIN ");

                            builder.Append(table);
                            builder.Append(" ON ");
                            builder.Append(fieldName);
                            builder.Append('=');
                            builder.Append(joinFieldName);

                            if (tables.Count > 0)
                                builder.Append(')');
                        }
                    }
                }
            }

            if (tables.Count > 0)
                throw new InvalidOperationException("No join defined between " + primaryTable + " and " + tables[0]);
        }

        private bool AppendFilters(StringBuilder builder)
        {
            if (_filters.Count == 0)
                return false;

            if (_filters.Count == 1)
            {
                foreach (var filter in _filters)
                    AppendFilter(builder, filter.Key, filter.Value);

                return true;
            }

            var filterExpression = _filterExpression ?? BuildFilterExpression();

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
                    var filter = _filters[val - 1];
                    AppendFilter(builder, filter.Key, filter.Value);
                }
            }

            return true;
        }

        private static void AppendFilter(StringBuilder builder, string fieldName, string bindVariable)
        {
            builder.Append(fieldName);

            if (bindVariable[0] == '~')
            {
                builder.Append(" LIKE ");
                builder.Append(bindVariable, 1, bindVariable.Length - 1);
            }
            else
            {
                builder.Append('=');
                builder.Append(bindVariable);
            }
        }

        private void AppendOrderBy(StringBuilder builder)
        {
            if (_orderBy != null)
            {
                builder.Append(" ORDER BY ");

                foreach (var orderBy in _orderBy)
                {
                    builder.Append(orderBy);
                    builder.Append(", ");
                }

                builder.Length -= 2;
            }
        }

        private string BuildFilterExpression()
        {
            if (_filters.Count == 1)
                return "1";

            var builder = new StringBuilder();
            for (int i = 0; i < _filters.Count; i++)
            {
                builder.Append(i + 1);
                builder.Append('&');
            }

            builder.Length -= 1;
            return builder.ToString();
        }
    }
}
