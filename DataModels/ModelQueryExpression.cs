using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jamiras.Components;
using Jamiras.Database;

namespace Jamiras.DataModels
{
    public class ModelQueryExpression
    {
        public ModelQueryExpression()
        {
            _queryFields = new List<string>();
            _filters = EmptyTinyDictionary<string, string>.Instance;
        }

        private List<string> _queryFields;
        private ITinyDictionary<string, string> _filters;
        private List<JoinData> _joins;
        private string _filterExpression;

        private class JoinData
        {
            public JoinData(string localKeyFieldName, string remoteKeyFieldName, bool localKeyIsPrimary)
            {
                LocalKeyFieldName = localKeyFieldName;
                RemoteKeyFieldName = remoteKeyFieldName;
                LocalKeyIsPrimary = localKeyIsPrimary;
            }

            public string LocalKeyFieldName { get; private set; }
            public string RemoteKeyFieldName { get; private set; }
            public bool LocalKeyIsPrimary { get; private set; }
        }

        public void AddQueryField(string fieldName)
        {
            _queryFields.Add(fieldName);
        }

        public void AddFilter(string fieldName, string bindVariable)
        {
            _filters = _filters.AddOrUpdate(fieldName, bindVariable);
        }

        public void AddLikeFilter(string fieldName, string bindVariable)
        {
            _filters = _filters.AddOrUpdate(fieldName, '~' + bindVariable);
        }

        public void AddJoin(string localKeyFieldName, string remoteKeyFieldName, bool localKeyIsPrimary)
        {
            if (_joins == null)
                _joins = new List<JoinData>();

            _joins.Add(new JoinData(localKeyFieldName, remoteKeyFieldName, localKeyIsPrimary));
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
            var whereJoins = AppendJoinTree(builder, tables);
            builder.Append(" WHERE ");

            bool wherePresent = AppendFilters(builder, !String.IsNullOrEmpty(whereJoins));
            wherePresent = AppendWhereJoins(builder, whereJoins, wherePresent);

            if (!wherePresent)
                builder.Length -= 7;

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

            foreach (var field in _filters.Keys)
            {
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

        private string AppendJoinTree(StringBuilder builder, List<string> tables)
        {
            if (tables.Count == 1)
            {
                builder.Append(tables[0]);
                return String.Empty;
            }

            if (_joins == null)
                throw new InvalidOperationException("No join defined between " + tables[0] + " and " + tables[1]);

            int tableCount = tables.Count;
            foreach (var join in _joins.Where(j => j.LocalKeyIsPrimary))
            {
                int idx = join.LocalKeyFieldName.IndexOf('.');
                if (idx > 0)
                {
                    string table = join.LocalKeyFieldName.Substring(0, idx);
                    idx = tables.IndexOf(table);
                    if (idx >= 0)
                    {
                        tables.RemoveAt(idx);
                        builder.Append(table);
                    }
                }

                idx = join.RemoteKeyFieldName.IndexOf('.');
                if (idx > 0)
                {
                    string table = join.RemoteKeyFieldName.Substring(0, idx);
                    idx = tables.IndexOf(table);
                    if (idx >= 0)
                    {
                        tables.RemoveAt(idx);
                        builder.Append(" LEFT OUTER JOIN ");
                        builder.Append(table);
                        builder.Append(" ON ");
                        builder.Append(join.LocalKeyFieldName);
                        builder.Append('=');
                        builder.Append(join.RemoteKeyFieldName);
                    }
                }
            }

            if (tables.Count == 0)
                return String.Empty;

            if (tables.Count < tableCount)
                builder.Append(", ");

            var whereJoins = new StringBuilder();
            foreach (var join in _joins.Where(j => !j.LocalKeyIsPrimary))
            {
                int idx = join.LocalKeyFieldName.IndexOf('.');
                if (idx > 0)
                {
                    string table = join.LocalKeyFieldName.Substring(0, idx);
                    idx = tables.IndexOf(table);
                    if (idx >= 0)
                    {
                        tables.RemoveAt(idx);
                        builder.Append(table);
                        builder.Append(", ");
                    }
                }

                idx = join.RemoteKeyFieldName.IndexOf('.');
                if (idx > 0)
                {
                    string table = join.RemoteKeyFieldName.Substring(0, idx);
                    idx = tables.IndexOf(table);
                    if (idx >= 0)
                    {
                        tables.RemoveAt(idx);
                        builder.Append(table);
                        builder.Append(", ");
                    }
                }

                whereJoins.Append(join.LocalKeyFieldName);
                whereJoins.Append('=');
                whereJoins.Append(join.RemoteKeyFieldName);
                whereJoins.Append(" AND ");
            }

            builder.Length -= 2;
            whereJoins.Length -= 5;
            return whereJoins.ToString();
        }

        private bool AppendFilters(StringBuilder builder, bool hasWhereJoins)
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

            if (hasWhereJoins)
                builder.Append('(');

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
                    var fieldName = _filters.Keys.ElementAt(val - 1);
                    string bindVariable;
                    _filters.TryGetValue(fieldName, out bindVariable);

                    AppendFilter(builder, fieldName, bindVariable);
                }
            }

            if (hasWhereJoins)
                builder.Append(')');

            return true;
        }

        private void AppendFilter(StringBuilder builder, string fieldName, string bindVariable)
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

        private bool AppendWhereJoins(StringBuilder builder, string whereJoins, bool wherePresent)
        {
            if (String.IsNullOrEmpty(whereJoins))
                return wherePresent;

            if (wherePresent)
                builder.Append(" AND ");

            builder.Append(whereJoins);
            return true;
        }
    }
}
