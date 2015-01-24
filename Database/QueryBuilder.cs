using System;
using System.Collections.Generic;
using System.Text;

namespace Jamiras.Database
{
    /// <summary>
    /// Class to facilitate in constructing database agnostic queries.
    /// </summary>
    public class QueryBuilder
    {
        public QueryBuilder()
        {
            _fields = new List<string>();
            _filters = new List<FilterDefinition>();
            _joins = new List<JoinDefinition>();
            _orderBy = new List<OrderByDefinition>();
        }

        private readonly List<string> _fields;
        private readonly List<FilterDefinition> _filters;
        private readonly List<JoinDefinition> _joins;
        private readonly List<OrderByDefinition> _orderBy;
        private string _filterExpression;

        public override string ToString()
        {
            return AccessDatabaseQuery.BuildQueryString(this);
        }

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
        /// Defines the logical expression to apply to the filters. For example (1|2)&3
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
