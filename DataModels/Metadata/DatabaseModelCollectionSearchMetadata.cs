using System;
using System.Text;
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
            Contains,
            StartsWithOrContains
        }

        private readonly string _searchField;
        private readonly SearchType _searchType;

        public override sealed bool Query(ModelBase model, int maxResults, object primaryKey, IDatabase database)
        {
            string primaryKeyString = primaryKey.ToString();
            if (String.IsNullOrEmpty(primaryKeyString))
                return true;

            // first pass: exact match
            if (!Query(model, maxResults, primaryKeyString, database)) 
                return false;

            var collection = (IDataModelCollection)model;
            if (collection.Count < maxResults && primaryKeyString.IndexOf(' ') != -1)
            {
                // second pass: replace whitespace with wildcards
                var wildcardPrimaryKeyString = ConvertWhitespaceToWildcards(primaryKeyString);
                if (!Query(model, maxResults, wildcardPrimaryKeyString, database))
                    return false;

                if (collection.Count < maxResults)
                {
                    // third pass: search on individual terms in search text
                    var words = wildcardPrimaryKeyString.Split('%');
                    if (!Query(model, maxResults, words[0], database))
                        return false;

                    // always do Contains search for secondary words
                    for (int i = 1; i < words.Length && collection.Count < maxResults; i++)
                    {
                        var searchText = '%' + words[i] + '%';
                        if (!base.Query(model, maxResults - collection.Count, searchText, database))
                            return false;
                    }
                }
            }

            return true;
        }

        private bool Query(ModelBase model, int maxResults, string searchText, IDatabase database)
        {
            var collection = (IDataModelCollection)model;
            searchText = AddWildcards(searchText);

            if (!base.Query(model, maxResults - collection.Count, searchText, database))
                return false;

            if (_searchType == SearchType.StartsWithOrContains && collection.Count < maxResults)
            {
                searchText = '%' + searchText;
                if (!base.Query(model, maxResults - collection.Count, searchText, database))
                    return false;
            }

            return true;
        }

        private static string ConvertWhitespaceToWildcards(string primaryKeyString)
        {
            var builder = new StringBuilder(primaryKeyString.Length + 3);
            bool atWhitespace = true;

            foreach (var c in primaryKeyString)
            {
                if (Char.IsWhiteSpace(c))
                {
                    if (!atWhitespace)
                    {
                        atWhitespace = true;
                        builder.Append('%');
                    }
                }
                else
                {
                    builder.Append(c);
                    atWhitespace = false;
                }
            }

            if (atWhitespace)
                builder.Length--;

            return builder.ToString();
        }

        private string AddWildcards(string text)
        {
            switch (_searchType)
            {
                case SearchType.Contains:
                    return '%' + text + '%';

                case SearchType.StartsWith:
                case SearchType.StartsWithOrContains:
                    return text + '%';

                case SearchType.EndsWith:
                    return '%' + text;

                default:
                    return text;
            }
        }

        protected override void CustomizeQuery(QueryBuilder query)
        {
            query.Filters.Add(new FilterDefinition(_searchField, (_searchType == SearchType.Exact) ? FilterOperation.Equals : FilterOperation.Like, FilterValueToken));
            query.OrderBy.Add(new OrderByDefinition(_searchField, SortOrder.Ascending));
            base.CustomizeQuery(query);
        }
    }
}
