﻿using System;
using System.Collections.Generic;
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

        ///// <summary>
        ///// Returns a <see cref="System.String" /> that represents this instance.
        ///// </summary>
        //public override string ToString()
        //{
        //    return AccessDatabaseQuery.BuildQueryString(this, null);
        //}

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
