using System;
using Jamiras.Database;

namespace Jamiras.DataModels.Metadata
{
    public abstract class DatabaseModelCollectionSearchMetadata<T> : DatabaseModelCollectionMetadata<T>
        where T : DataModelBase, new()
    {
        protected DatabaseModelCollectionSearchMetadata(ModelProperty searchProperty, SearchType searchType)
        {
            if (searchType == SearchType.None)
                throw new ArgumentException("searchType");

            var metadata = RelatedMetadata.GetFieldMetadata(searchProperty) as StringFieldMetadata;
            if (metadata == null)
                throw new ArgumentException("searchProperty does not map to a string field", "searchProperty");

            _searchField = metadata.FieldName;
            _searchType = searchType;
        }

        protected enum SearchType
        {
            None = 0,
            Exact,
            StartsWith,
            EndsWith,
            Contains
        }

        private readonly string _searchField;
        private readonly SearchType _searchType;

        public override sealed bool Query(ModelBase model, object primaryKey, IDatabase database)
        {
            string searchText = primaryKey.ToString();
            switch (_searchType)
            {
                case SearchType.Contains:
                    searchText = '%' + searchText + '%';
                    break;
                case SearchType.StartsWith:
                    searchText = searchText + '%';
                    break;
                case SearchType.EndsWith:
                    searchText = '%' + searchText;
                    break;
            }

            return base.Query(model, searchText, database);
        }

        protected override void CustomizeQuery(QueryBuilder query)
        {
            query.Filters.Add(new FilterDefinition(_searchField, (_searchType == SearchType.Exact) ? FilterOperation.Equals : FilterOperation.Like, FilterValueToken));
            base.CustomizeQuery(query);
        }
    }
}
