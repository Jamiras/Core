using Jamiras.Components;
using Jamiras.Database;
using Jamiras.DataModels.Metadata;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Jamiras.DataModels.Database
{
    [DebuggerDisplay("{_queryBuilder.ToString()}")]
    public class QueryBuilder<T>
        where T : DataModelBase, new()
    {
        public QueryBuilder()
            : this(ServiceRepository.Instance.FindService<IDatabase>(),
                   (DatabaseModelMetadata)ServiceRepository.Instance.FindService<IDataModelMetadataRepository>().GetModelMetadata(typeof(T)))
        {
        }

        public QueryBuilder(IDatabase database, DatabaseModelMetadata metadata)
        {
            _queryBuilder = new QueryBuilder();
            _database = database;
            Metadata = metadata;
        }

        private readonly QueryBuilder _queryBuilder;
        private readonly IDatabase _database;

        internal DatabaseModelMetadata Metadata { get; private set; }

        private string GetColumnName(ModelProperty property)
        {
            var fieldMetadata = Metadata.GetFieldMetadata(property);
            return fieldMetadata.FieldName;
        }

        public QueryBuilder<T> Where(ModelProperty property, int value)
        {
            _queryBuilder.Filters.Add(new FilterDefinition(GetColumnName(property), FilterOperation.Equals, value));
            return this;
        }

        public string ToSql()
        {
            return _queryBuilder.ToString();
        }

        private QueryBuilder GetBuilder()
        {
            var builder = Metadata.BuildQueryExpression(_database);
            builder.Limit = _queryBuilder.Limit;
            builder.Offset = _queryBuilder.Offset;

            foreach (var filter in _queryBuilder.Filters)
                builder.Filters.Add(filter);

            return builder;
        }

        public List<T> Get()
        {
            var builder = GetBuilder();

            Debug.WriteLine(">> " + builder.ToString());

            var list = new List<T>();

            var query = _database.PrepareQuery(builder);
            while (query.FetchRow())
            {
                T item = new T();
                Metadata.PopulateItem(item, _database, query);
                list.Add(item);
            }

            Debug.WriteLine("<< {0} rows returned", list.Count);
            return list;
        }

        public T First()
        {
            var builder = GetBuilder();
            builder.Limit = 1;
            builder.Offset = 0;

#if DEBUG
            // Access database doesn't support limit in the query, but report it in the log
            var temp = new StringBuilder();
            ServiceRepository.Instance.FindService<IDatabase>().AppendQueryRange(temp, 1, 0);
            Debug.WriteLine(">> " + builder.ToString() + (temp.Length == 0 ? " (LIMIT 1)" : ""));
#endif

            T item = null;
            var query = _database.PrepareQuery(builder);
            if (query.FetchRow())
            {
                item = new T();
                Metadata.PopulateItem(item, _database, query);
                Debug.WriteLine("<< {0} rows returned", 1);
            }
            else
            {
                Debug.WriteLine("<< {0} rows returned", 0);
            }

            return item;
        }
    }
}
