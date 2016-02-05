using System;
using System.Collections.Generic;
using System.Text;
using Jamiras.Database;
using Jamiras.Components;

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

            AreResultsReadOnly = true;
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

        /// <summary>
        /// Populates a collection with items from a database.
        /// </summary>
        /// <param name="models">The uninitialized collection to populate.</param>
        /// <param name="maxResults">The maximum number of results to return</param>
        /// <param name="primaryKey">The primary key of the model to populate.</param>
        /// <param name="database">The database to populate from.</param>
        /// <returns><c>true</c> if the model was populated, <c>false</c> if not.</returns>
        protected override sealed bool Query(ICollection<T> models, int maxResults, object primaryKey, IDatabase database)
        {
            string primaryKeyString = primaryKey.ToString();
            if (String.IsNullOrEmpty(primaryKeyString))
                return true;

            // first pass: exact match
            if (!Query(models, maxResults, primaryKeyString, database)) 
                return false;

            if (models.Count < maxResults && primaryKeyString.IndexOf(' ') != -1)
            {
                // second pass: replace whitespace with wildcards
                var words = Tokenizer.GetLongestWords(primaryKeyString, 3);
                if (words.Length == 0)
                    return true;

                var builder = new StringBuilder();
                foreach (var word in words)
                {
                    builder.Append(word);
                    builder.Append('%');
                }
                builder.Length--;

                var wildcardPrimaryKeyString = builder.ToString();
                if (!Query(models, maxResults, wildcardPrimaryKeyString, database))
                    return false;

                if (models.Count < maxResults)
                {
                    // third pass: search on individual terms in search text
                    // if search type is startswith, check first word first
                    if (words.Length > 1 && (_searchType == SearchType.StartsWith || _searchType == SearchType.StartsWithOrContains))
                    {
                        var searchText = words[0].ToString() + '%';
                        if (!base.Query(models, maxResults, searchText, database))
                            return false;
                    }

                    // then use contains search for all words, starting with the longest
                    Array.Sort(words, (l, r) => r.Length - l.Length);
                    foreach (var word in words)
                    {
                        if (maxResults - models.Count <= 0)
                            break;

                        var searchText = '%' + word.ToString() + '%';
                        if (!base.Query(models, maxResults - models.Count, searchText, database))
                            return false;
                    }
                }
            }

            return true;
        }

        private bool Query(ICollection<T> models, int maxResults, string searchText, IDatabase database)
        {
            searchText = AddWildcards(searchText);

            if (!base.Query(models, maxResults - models.Count, searchText, database))
                return false;

            if (_searchType == SearchType.StartsWithOrContains && models.Count < maxResults)
            {
                searchText = '%' + searchText;
                if (!base.Query(models, maxResults - models.Count, searchText, database))
                    return false;
            }

            return true;
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

        /// <summary>
        /// Allows a subclass to modify the generated query before it is executed.
        /// </summary>
        protected override void CustomizeQuery(QueryBuilder query)
        {
            query.Filters.Add(new FilterDefinition(_searchField, (_searchType == SearchType.Exact) ? FilterOperation.Equals : FilterOperation.Like, FilterValueToken));
            query.OrderBy.Add(new OrderByDefinition(_searchField, SortOrder.Ascending));
            base.CustomizeQuery(query);
        }
    }
}
