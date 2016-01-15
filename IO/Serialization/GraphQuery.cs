using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Jamiras.Components;

namespace Jamiras.IO.Serialization
{
    /// <summary>
    /// Repesents a hierarchical query.
    /// </summary>
    public class GraphQuery
    {
        public GraphQuery(string objectType)
        {
            ObjectType = objectType;
            Filters = new TinyDictionary<string, string>();
            Fields = GraphQueryField.EmptyFieldArray;
        }

        /// <summary>
        /// Gets the name of the object being queried.
        /// </summary>
        public string ObjectType { get; private set; }

        /// <summary>
        /// Gets any filters being passed to the query.
        /// </summary>
        public IDictionary<string, string> Filters { get; private set; }

        /// <summary>
        /// Gets the fields to query.
        /// </summary>
        public IEnumerable<GraphQueryField> Fields { get; internal set; }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append(ObjectType);

            if (Filters.Count > 0)
            {
                builder.Append(" (");
                foreach (var filter in Filters)
                {
                    builder.Append(filter.Key);
                    builder.Append(": ");
                    builder.Append(filter.Value);
                    builder.Append(", ");
                }
                builder.Length -= 2;
                builder.Append(')');
            }

            AppendFields(builder, Fields);

            return builder.ToString();
        }

        private static void AppendFields(StringBuilder builder, IEnumerable<GraphQueryField> fields)
        {
            if (fields.Any())
            {
                builder.Append(" { ");
                foreach (var field in fields)
                {
                    builder.Append(field.FieldName);
                    AppendFields(builder, field.NestedFields);
                    builder.Append(", ");
                }

                builder.Length -= 2;
                builder.Append(" }");
            }
        }

        /// <summary>
        /// Constructs a <see cref="GraphQuery"/> from a GraphQL string.
        /// </summary>
        /// <example>
        ///   user (id: 6) { first_name, last_name, address { street, city, state, zip } }
        /// </example>
        public static GraphQuery Parse(string input)
        {
            return Parse(Tokenizer.CreateTokenizer(input));
        }

        /// <summary>
        /// Constructs a <see cref="GraphQuery"/> from a stream.
        /// </summary>
        public static GraphQuery Parse(Stream input)
        {
            return Parse(Tokenizer.CreateTokenizer(input));
        }

        private static GraphQuery Parse(Tokenizer tokenizer)
        {
            tokenizer.SkipWhitespace();

            var objectType = tokenizer.ReadIdentifier();
            var query = new GraphQuery(objectType.ToString());
            ReadFilters(tokenizer, query);

            var fields = new List<GraphQueryField>();
            ReadFields(tokenizer, fields);
            if (fields.Count > 0)
                query.Fields = fields.ToArray();

            return query;
        }

        private static void ReadFilters(Tokenizer tokenizer, GraphQuery query)
        {
            tokenizer.SkipWhitespace();

            if (tokenizer.NextChar != '(')
                return;
            tokenizer.Advance();

            do
            {
                tokenizer.SkipWhitespace();

                if (tokenizer.NextChar == ')')
                {
                    tokenizer.Advance();
                    return;
                }

                var parameter = tokenizer.ReadIdentifier();
                tokenizer.SkipWhitespace();

                if (tokenizer.NextChar != ':')
                    throw new InvalidOperationException("Filter " + parameter + " does not provide a value");

                tokenizer.Advance();
                tokenizer.SkipWhitespace();

                var value = tokenizer.ReadValue();
                tokenizer.SkipWhitespace();

                if (tokenizer.NextChar == ',')
                    tokenizer.Advance();
                else if (tokenizer.NextChar != ')')
                    throw new InvalidOperationException("Parse error after " + parameter + " filter, found: " + tokenizer.NextChar);

                query.Filters.Add(parameter.ToString(), value.ToString());
            } while (true);
        }

        private static void ReadFields(Tokenizer tokenizer, ICollection<GraphQueryField> fields)
        {
            tokenizer.SkipWhitespace();

            if (tokenizer.NextChar != '{')
                return;
            tokenizer.Advance();

            do
            {
                tokenizer.SkipWhitespace();

                if (tokenizer.NextChar == '}')
                {
                    tokenizer.Advance();
                    return;
                }

                var fieldName = tokenizer.ReadIdentifier();
                tokenizer.SkipWhitespace();

                var field = new GraphQueryField(fieldName.ToString());

                if (tokenizer.NextChar == '{')
                {
                    var nestedFields = new List<GraphQueryField>();
                    ReadFields(tokenizer, nestedFields);
                    if (nestedFields.Count > 0)
                        field.NestedFields = nestedFields.ToArray();

                    tokenizer.SkipWhitespace();
                }

                fields.Add(field);

                if (tokenizer.NextChar == ',')
                    tokenizer.Advance();
                else if (tokenizer.NextChar != '}')
                    throw new InvalidOperationException("Parse error after " + fieldName + " field, found: " + tokenizer.NextChar);
            } while (true);
        }
    }

    [DebuggerDisplay("FieldName")]
    public class GraphQueryField
    {
        public GraphQueryField(string fieldName)
        {
            FieldName = fieldName;
            NestedFields = EmptyFieldArray;
        }

        /// <summary>
        /// Gets the name of the field.
        /// </summary>
        public string FieldName { get; private set; }

        /// <summary>
        /// Gets any nested fields to be queried.
        /// </summary>
        public IEnumerable<GraphQueryField> NestedFields { get; internal set; }

        internal static readonly GraphQueryField[] EmptyFieldArray = new GraphQueryField[0];
    }
}
