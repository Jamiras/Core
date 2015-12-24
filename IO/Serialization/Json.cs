using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Jamiras.Components;

namespace Jamiras.IO.Serialization
{
    public class Json
    {
        /// <summary>
        /// Converts a JSON string into a dictionary of fields.
        /// </summary>
        public static IDictionary<string, object> Parse(string input)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(input)))
            {
                return Parse(stream);
            }
        }

        /// <summary>
        /// Converts a stream containing a JSON string into a dictionary of fields.
        /// </summary>
        public static IDictionary<string, object> Parse(Stream input)
        {
            var tokenizer = new Tokenizer(input);
            tokenizer.SkipWhitespace();

            if (tokenizer.NextChar == '[')
            {
                var array = ParseArray("root", tokenizer);
                var dict = new TinyDictionary<string, object>();
                dict.Add("items", array);
                return dict;
            }

            return ParseObject("root", tokenizer);
        }

        private static IDictionary<string, object> ParseObject(string parentObjectName, Tokenizer tokenizer)
        {
            if (tokenizer.NextChar != '{')
                throw new InvalidOperationException("Object should start with an opening brace, found " + tokenizer.NextChar);

            var fields = new TinyDictionary<string, object>();

            tokenizer.Advance();
            tokenizer.SkipWhitespace();

            while (tokenizer.NextChar != '}')
            {
                if (tokenizer.NextChar != '"')
                {
                    if (tokenizer.NextChar == 0)
                        throw new InvalidOperationException("End of stream encountered processing fields for " + parentObjectName + " object");

                    throw new InvalidOperationException("Field name should be in quotes, found " + tokenizer.NextChar);
                }

                var fieldName = tokenizer.ReadQuotedString();
                tokenizer.SkipWhitespace();
                if (tokenizer.NextChar != ':')
                    throw new InvalidOperationException("Expecting colon following field name: " + fieldName);

                tokenizer.Advance();
                tokenizer.SkipWhitespace();

                string value;
                switch (tokenizer.NextChar)
                {
                    case '{':
                        var nestedObject = ParseObject(fieldName, tokenizer);
                        fields.Add(fieldName, nestedObject);
                        break;

                    case '"':
                        value = tokenizer.ReadQuotedString();
                        fields.Add(fieldName, value);           
                        break;

                    case '[':
                        var nestedArray = ParseArray(fieldName, tokenizer);
                        fields.Add(fieldName, nestedArray);
                        break;

                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        value = tokenizer.ReadNumber();
                        if (value.Contains('.'))
                        {
                            var dVal = Double.Parse(value);
                            fields.Add(fieldName, dVal);
                        }
                        else
                        {
                            var iVal = Int32.Parse(value);
                            fields.Add(fieldName, iVal);
                        }
                        break;

                    default:
                        value = tokenizer.ReadIdentifier();
                        if (String.Compare(value, "true", StringComparison.InvariantCultureIgnoreCase) == 0)
                            fields.Add(fieldName, true);
                        else if (String.Compare(value, "false", StringComparison.InvariantCultureIgnoreCase) == 0)
                            fields.Add(fieldName, false);
                        else if (String.Compare(value, "null", StringComparison.InvariantCultureIgnoreCase) == 0)
                            fields.Add(fieldName, null);
                        else
                            throw new NotSupportedException("Unsupported raw field value: " + value);
                        break;
                }

                tokenizer.SkipWhitespace();
                if (tokenizer.NextChar == ',')
                {
                    tokenizer.Advance();
                    tokenizer.SkipWhitespace();
                }
            }

            tokenizer.Advance();
            return fields;
        }

        private static IEnumerable<IDictionary<string, object>> ParseArray(string fieldName, Tokenizer tokenizer)
        {
            if (tokenizer.NextChar != '[')
                throw new InvalidOperationException("Array should start with an opening bracket, found " + tokenizer.NextChar);

            tokenizer.Advance();
            tokenizer.SkipWhitespace();

            var items = new List<IDictionary<string, object>>();
            while (tokenizer.NextChar != ']')
            {
                if (tokenizer.NextChar == '{')
                {
                    var itemName = String.Format("{0}[{1}]", fieldName, items.Count);
                    var item = ParseObject(itemName, tokenizer);
                    items.Add(item);
                }
                else
                {
                    throw new NotSupportedException(fieldName + " array element starting with " + tokenizer.NextChar);
                }

                tokenizer.SkipWhitespace();

                if (tokenizer.NextChar == ',')
                {
                    tokenizer.Advance();
                    tokenizer.SkipWhitespace();
                }
            }

            tokenizer.Advance();
            return items.ToArray();
        }

        /// <summary>
        /// Converts a field dictionary to a JSON string.
        /// </summary>
        /// <param name="fields">Fields of the JSON object</param>
        /// <param name="indent">Indent for each line of the output. If 0, output is all on a single line.</param>
        public static String Format(IDictionary<string, object> fields, int indent = 0)
        {
            var builder = new StringBuilder();
            AppendObject(builder, fields, 0, indent);
            return builder.ToString();
        }

        private static void AppendObject(StringBuilder builder, IEnumerable<KeyValuePair<string, object>> fields, int currentIndent, int indent)
        {
            builder.Append('{');
            currentIndent += indent;
            AppendLine(builder, currentIndent, indent);

            var first = true;
            foreach (var field in fields)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    builder.Append(",");
                    AppendLine(builder, currentIndent, indent);
                }


                builder.Append('"');
                builder.Append(field.Key);
                builder.Append("\": ");

                var strValue = field.Value as string;
                if (strValue != null)
                {
                    builder.Append('"');
                    AppendString(builder, strValue);
                    builder.Append('"');
                }
                else if (field.Value is bool)
                {
                    builder.Append((bool)field.Value ? "true" : "false");
                }
                else if (field.Value == null)
                {
                    builder.Append("null");
                }
                else if (field.Value is float)
                {
                    if ((float)field.Value == 0.0)
                        builder.Append("0.0");
                    else
                        builder.Append(field.Value);
                }
                else if (field.Value is double)
                {
                    if ((double)field.Value == 0.0)
                        builder.Append("0.0");
                    else
                        builder.Append(field.Value);
                }
                else
                {
                    var nestedObject = field.Value as IDictionary<string, object>;
                    if (nestedObject != null)
                    {
                        AppendObject(builder, nestedObject, currentIndent, indent);
                    }
                    else
                    {
                        var nestedArray = field.Value as IEnumerable<IDictionary<string, object>>;
                        if (nestedArray != null)
                        {
                            AppendArray(builder, nestedArray, currentIndent, indent);
                        }
                        else
                        {
                            builder.Append(field.Value);
                        }
                    }
                }
            }

            if (!first)
            {
                currentIndent -= indent;
                AppendLine(builder, currentIndent, indent);
            }

            builder.Append('}');
        }

        private static void AppendString(StringBuilder builder, string strValue)
        {
            for (var i = 0; i < strValue.Length; i++)
            {
                var c = strValue[i];
                switch (c)
                {
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '\"':
                        builder.Append("\\\"");
                        break;
                    case '\r':
                        if (i + 1 == strValue.Length || strValue[i + 1] != '\n')
                            builder.Append("\\\r");
                        break;
                    default:
                        builder.Append(c);
                        break;
                }
            }
        }

        private static void AppendArray(StringBuilder builder, IEnumerable<IDictionary<string, object>> nestedArray, int currentIndent, int indent)
        {
            builder.Append('[');
            currentIndent += indent;
            AppendLine(builder, currentIndent, indent);

            var first = true;
            foreach (var obj in nestedArray)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    builder.Append(',');
                    AppendLine(builder, currentIndent, indent);
                }

                AppendObject(builder, obj, currentIndent, indent);
            }

            currentIndent -= indent;
            AppendLine(builder, currentIndent, indent);

            builder.Append(']');
        }

        private static void AppendLine(StringBuilder builder, int currentIndent, int indent)
        {
            if (indent == 0)
            {
                builder.Append(' ');
            }
            else
            {
                builder.AppendLine();
                for (var i = 0; i < currentIndent; i++)
                    builder.Append(' ');
            }
        }
    }
}
