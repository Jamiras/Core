using Jamiras.Components;
using Jamiras.DataModels;
using Jamiras.DataModels.Metadata;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Jamiras.Database
{
    [DebuggerDisplay("{_queryBuilder.ToString()}")]
    public class FluentQueryBuilder<T>
        where T : DataModelBase, new()
    {
        public FluentQueryBuilder()
            : this(ServiceRepository.Instance.FindService<IDatabase>(),
                   (DatabaseModelMetadata)ServiceRepository.Instance.FindService<IDataModelMetadataRepository>().GetModelMetadata(typeof(T)))
        {
        }

        public FluentQueryBuilder(IDatabase database, DatabaseModelMetadata metadata)
        {
            _queryBuilder = new QueryBuilder();
            _database = database;
            _bindings = null;

            Metadata = metadata;
        }

        private readonly QueryBuilder _queryBuilder;
        private readonly IDatabase _database;
        private Dictionary<string, object> _bindings;

        internal DatabaseModelMetadata Metadata { get; private set; }

        private void Bind(string bindingName, object value)
        {
            _bindings ??= new Dictionary<string, object>();
            _bindings.Add(bindingName, value);
        }

        private string GetColumnName(ModelProperty property)
        {
            var fieldMetadata = Metadata.GetFieldMetadata(property);
            return fieldMetadata.FieldName;
        }

        /// <summary>
        /// Adds a filter to the query.
        /// </summary>
        /// <param name="property">Property to filter on.</param>
        /// <param name="value">Value to filter on.</param>
        /// <returns><see cref="FluentQueryBuilder{T}"/> for chaining.</returns>
        public FluentQueryBuilder<T> Where(ModelProperty property, int value)
        {
            _queryBuilder.Filters.Add(new FilterDefinition(GetColumnName(property), FilterOperation.Equals, value));
            return this;
        }

        /// <summary>
        /// Adds a filter to the query.
        /// </summary>
        /// <param name="property">Property to filter on.</param>
        /// <param name="value">Value to filter on.</param>
        /// <returns><see cref="FluentQueryBuilder{T}"/> for chaining.</returns>
        public FluentQueryBuilder<T> Where(ModelProperty property, string value)
        {
            _queryBuilder.Filters.Add(new FilterDefinition(GetColumnName(property), FilterOperation.Equals, value));
            return this;
        }

        /// <summary>
        /// Adds a filter to the query.
        /// </summary>
        /// <param name="property">Property to filter on.</param>
        /// <param name="value">Value to filter on.</param>
        /// <returns><see cref="FluentQueryBuilder{T}"/> for chaining.</returns>
        public FluentQueryBuilder<T> WhereLike(ModelProperty property, string value)
        {
            var columnName = GetColumnName(property);
            var bindingName = '@' + columnName;
            Bind(bindingName, value);
            _queryBuilder.Filters.Add(new FilterDefinition(GetColumnName(property), FilterOperation.Like, bindingName));
            return this;
        }

        /// <summary>
        /// Constructs a query from the <see cref="FluentQueryBuilder{T}"/>.
        /// </summary>
        public string ToSql()
        {
            return _queryBuilder.ToString();
        }

        private QueryBuilder GetBuilder()
        {
            var builder = new QueryBuilder();
            if (_queryBuilder.Fields.Count > 0)
            {
                foreach (var field in _queryBuilder.Fields)
                    builder.Fields.Add(field);
            }
            else
            {
                foreach (var metadata in Metadata.AllFieldMetadata.Values)
                    builder.Fields.Add(metadata.FieldName);
            }

            builder.Limit = _queryBuilder.Limit;
            builder.Offset = _queryBuilder.Offset;

            foreach (var filter in _queryBuilder.Filters)
                builder.Filters.Add(filter);

            return builder;
        }

        private void ApplyBindings(IDatabaseQuery query)
        {
            if (_bindings != null)
            {
                foreach (var binding in _bindings)
                    query.Bind(binding.Key, binding.Value);
            }
        }

        private void LogQuery(QueryBuilder builder)
        {
            var sql = builder.ToString();

            if (_bindings != null)
            {
                foreach (var binding in _bindings)
                    sql = sql.Replace(binding.Key, '[' + binding.Value.ToString() + ']');
            }

#if DEBUG
            if (builder.Limit != 0)
            {
                // Access database doesn't support limit in the query, but report it in the log
                var temp = new StringBuilder();
                ServiceRepository.Instance.FindService<IDatabase>().AppendQueryRange(temp, 1, 0);
                if (temp.Length == 0)
                    sql += " (LIMIT 1)";
            }
#endif

            Debug.WriteLine(">> " + sql);
        }

        /// <summary>
        /// Returns a collection of models matching the query.
        /// </summary>
        public List<T> Get()
        {
            var builder = GetBuilder();
            LogQuery(builder);

            var list = new List<T>();

#if DEBUG
            var stopwatch = Stopwatch.StartNew();
#endif

            var query = _database.PrepareQuery(builder);
            ApplyBindings(query);

            while (query.FetchRow())
            {
                T item = new T();
                Metadata.PopulateItem(item, builder, _database, query);
                list.Add(item);
            }

#if DEBUG
            stopwatch.Stop();
            Debug.WriteLine("<< {0}ms: {1} rows returned", stopwatch.Elapsed.TotalMilliseconds, list.Count);
#endif
            return list;
        }

        /// <summary>
        /// Returns the first model matching the query (<c>null</c> if not found).
        /// </summary>
        public T First()
        {
            var builder = GetBuilder();
            builder.Limit = 1;
            builder.Offset = 0;
            LogQuery(builder);

            T item = null;

#if DEBUG
            var stopwatch = Stopwatch.StartNew();
#endif

            var query = _database.PrepareQuery(builder);
            ApplyBindings(query);

            if (query.FetchRow())
            {
                item = new T();
                Metadata.PopulateItem(item, builder, _database, query);
            }

#if DEBUG
            stopwatch.Stop();
            Debug.WriteLine("<< {0}ms: {1} rows returned", stopwatch.Elapsed.TotalMilliseconds, item != null ? 1 : 0);
#endif

            return item;
        }
    }
}
