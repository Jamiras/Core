﻿using Jamiras.Components;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Jamiras.IO.Serialization
{
    /// <summary>
    /// Represents a single JSON object.
    /// </summary>
    [DebuggerTypeProxy(typeof(JsonObjectDebugView))]
    public class JsonObject : IEnumerable<JsonField>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonObject"/> class from a JSON string.
        /// </summary>
        public JsonObject(string json)
            : this()
        {
            Parse(Tokenizer.CreateTokenizer(json));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonObject"/> class from a stream containing JSON.
        /// </summary>
        public JsonObject(Stream stream)
            : this()
        {
            Parse(Tokenizer.CreateTokenizer(stream));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonObject"/> class from a Token containing JSON.
        /// </summary>
        public JsonObject(Token json)
            : this()
        {
            Parse(Tokenizer.CreateTokenizer(json));
        }

        /// <summary>
        /// Initializes a new empty instance of the <see cref="JsonObject"/> class.
        /// </summary>
        public JsonObject()
        {
            _fields = EmptyTinyDictionary<string, JsonField>.Instance; 
        }

        internal sealed class JsonObjectDebugView
        {
            public JsonObjectDebugView(JsonObject jsonObject)
            {
                _jsonObject = jsonObject;
            }

            private readonly JsonObject _jsonObject;

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public KeyValuePair<string, object>[] Items
            {
                get
                {
                    var array = new KeyValuePair<string, object>[_jsonObject._fields.Count];

                    int index = 0;
                    foreach (var kvp in _jsonObject._fields)
                        array[index++] = new KeyValuePair<string,object>(kvp.Key, kvp.Value.Value);

                    return array;
                }
            }
        }

        private ITinyDictionary<string, JsonField> _fields;

        /// <summary>
        /// Gets whether this instance is empty (has no fields).
        /// </summary>
        public bool IsEmpty
        {
            get { return _fields.Count == 0; }
        }

        #region IEnumerable<JsonField> Members

        IEnumerator<JsonField> IEnumerable<JsonField>.GetEnumerator()
        {
            return _fields.Values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _fields.Values.GetEnumerator();
        }

        #endregion

        #region ToString

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        public override string ToString()
        {
            return ToString(0);
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <param name="indent">The amount to indent each nested value.</param>
        public string ToString(int indent)
        {
            var builder = new StringBuilder();
            AppendObject(builder, 0, indent);
            return builder.ToString();
        }

        private void AppendObject(StringBuilder builder, int currentIndent, int indent)
        {
            builder.Append('{');
            currentIndent += indent;
            AppendLine(builder, currentIndent, indent);

            var first = true;
            foreach (var field in _fields.Values)
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
                builder.Append(field.FieldName);
                builder.Append("\": ");

                AppendValue(builder, field.Value, field.Type, currentIndent, indent);
            }

            if (!first)
            {
                currentIndent -= indent;
                AppendLine(builder, currentIndent, indent);
            }

            builder.Append('}');
        }

        private static void AppendValue(StringBuilder builder, object value, JsonFieldType type, int currentIndent, int indent)
        {
            if (value == null)
            {
                builder.Append("null");
                return;
            }

            switch (type)
            {
                case JsonFieldType.Boolean:
                    builder.Append((bool)value ? "true" : "false");
                    break;

                case JsonFieldType.Date:
                case JsonFieldType.DateTime:
                case JsonFieldType.String:
                    builder.Append('"');
                    AppendString(builder, (string)value);
                    builder.Append('"');
                    break;

                case JsonFieldType.Double:
                    var scan = builder.Length;
                    builder.Append(value);

                    bool decimalFound = false;
                    while (scan < builder.Length)
                    {
                        if (builder[scan] == '.')
                        {
                            decimalFound = true;
                            break;
                        }

                        scan++;
                    }

                    if (!decimalFound)
                        builder.Append(".0");
                    break;

                case JsonFieldType.Integer:
                    builder.Append(value);
                    break;

                case JsonFieldType.IntegerArray:
                    builder.Append('[');
                    var ints = (int[])value;
                    for (int i = 0; i < ints.Length; i++)
                    {
                        if (i > 0)
                            builder.Append(',');

                        builder.Append(' ');
                        builder.Append(ints[i]);
                    }
                    builder.Append(" ]");
                    break;

                case JsonFieldType.Object:
                    ((JsonObject)value).AppendObject(builder, currentIndent, indent);
                    break;

                case JsonFieldType.ObjectArray:
                    builder.Append('[');
                    if (indent > 0)
                    {
                        currentIndent += indent;
                        AppendLine(builder, currentIndent, indent);
                    }
                    bool first = true;
                    foreach (var nestedObject in ((IEnumerable<JsonObject>)value))
                    {
                        if (first)
                        {
                            first = false;
                        }
                        else
                        {
                            builder.Append(',');
                            if (indent > 0)
                                AppendLine(builder, currentIndent, indent);
                        }

                        if (indent == 0)
                            builder.Append(' ');

                        nestedObject.AppendObject(builder, currentIndent, indent);
                    }

                    if (indent > 0)
                    {
                        currentIndent -= indent;
                        AppendLine(builder, currentIndent, indent);
                    }
                    else
                    {
                        builder.Append(' ');
                    }

                    builder.Append(']');
                    break;

                default:
                    throw new NotImplementedException("AppendValue(" + type + ")");
            }
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

        #endregion

        #region Parse

        private void Parse(Tokenizer tokenizer)
        {
            tokenizer.SkipWhitespace();

            if (tokenizer.NextChar == '[')
            {
                ParseArray("items", tokenizer);
            }
            else if (tokenizer.NextChar != '\0')
            {
                ParseObject("root", tokenizer);
            }
        }

        private void ParseObject(string parentObjectName, Tokenizer tokenizer)
        {
            if (tokenizer.NextChar != '{')
                throw new InvalidOperationException("Object should start with an opening brace, found " + tokenizer.NextChar);

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

                Token value;
                switch (tokenizer.NextChar)
                {
                    case '{':
                        var nestedObject = new JsonObject();
                        nestedObject.ParseObject(fieldName.ToString(), tokenizer);
                        AddField(fieldName.ToString(), nestedObject);
                        break;

                    case '"':
                        value = tokenizer.ReadQuotedString();

                        if (value.Length == 11 && value[10] == 'Z' && value[4] == '-' && value[7] == '-')
                        {
                            int year, month, day;
                            if (Int32.TryParse(value.Substring(0, 4), out year) &&
                                Int32.TryParse(value.Substring(5, 2), out month) &&
                                Int32.TryParse(value.Substring(8, 2), out day))
                            {
                                AddField(fieldName.ToString(), JsonFieldType.Date, value.ToString());
                                break;
                            }
                        }
                        else if (value.Length == 17 && value[16] == 'Z' && value[4] == '-' && value[7] == '-' && value[10] == 'T' && value[13] == ':')
                        {
                            int year, month, day, hour, minute;
                            if (Int32.TryParse(value.Substring(0, 4), out year) &&
                                Int32.TryParse(value.Substring(5, 2), out month) &&
                                Int32.TryParse(value.Substring(8, 2), out day) &&
                                Int32.TryParse(value.Substring(11, 2), out hour) &&
                                Int32.TryParse(value.Substring(14, 2), out minute))
                            {
                                AddField(fieldName.ToString(), JsonFieldType.DateTime, value.Substring(0, 16) + ":00.000Z");
                                break;
                            }
                        }
                        else if (value.Length == 24 && value[23] == 'Z' && value[4] == '-' && value[7] == '-' && value[10] == 'T' && value[13] == ':' && value[16] == ':' && value[19] == '.')
                        {
                            int year, month, day, hour, minute, second, millisecond;
                            if (Int32.TryParse(value.Substring(0, 4), out year) &&
                                Int32.TryParse(value.Substring(5, 2), out month) &&
                                Int32.TryParse(value.Substring(8, 2), out day) &&
                                Int32.TryParse(value.Substring(11, 2), out hour) &&
                                Int32.TryParse(value.Substring(14, 2), out minute) &&
                                Int32.TryParse(value.Substring(17, 2), out second) &&
                                Int32.TryParse(value.Substring(20, 3), out millisecond))
                            {
                                AddField(fieldName.ToString(), JsonFieldType.DateTime, value.ToString());
                                break;
                            }
                        }

                        AddField(fieldName.ToString(), value.ToString());
                        break;

                    case '[':
                        ParseArray(fieldName.ToString(), tokenizer);
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
                            var dVal = Double.Parse(value.ToString());
                            AddField(fieldName.ToString(), dVal);
                        }
                        else
                        {
                            var iVal = Int32.Parse(value.ToString());
                            AddField(fieldName.ToString(), iVal);
                        }
                        break;

                    default:
                        value = tokenizer.ReadIdentifier();
                        if (value.CompareTo("true", StringComparison.InvariantCultureIgnoreCase) == 0)
                            AddField(fieldName.ToString(), true);
                        else if (value.CompareTo("false", StringComparison.InvariantCultureIgnoreCase) == 0)
                            AddField(fieldName.ToString(), false);
                        else if (value.CompareTo("null", StringComparison.InvariantCultureIgnoreCase) == 0)
                            AddField(fieldName.ToString(), JsonFieldType.Null, null);
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
        }

        private void ParseArray(string fieldName, Tokenizer tokenizer)
        {
            if (tokenizer.NextChar != '[')
                throw new InvalidOperationException("Array should start with an opening bracket, found " + tokenizer.NextChar);

            tokenizer.Advance();
            tokenizer.SkipWhitespace();

            var items = new List<JsonObject>();
            var strings = new List<string>();
            var ints = new List<int>();
            var dbls = new List<double>();

            while (tokenizer.NextChar != ']')
            {
                if (tokenizer.NextChar == '{')
                {
                    var itemName = String.Format("{0}[{1}]", fieldName, items.Count);
                    var item = new JsonObject();
                    item.ParseObject(itemName, tokenizer);
                    items.Add(item);
                }
                else if (tokenizer.NextChar == '"')
                {
                    var str = tokenizer.ReadQuotedString();
                    strings.Add(str.ToString());
                }
                else if (Char.IsDigit(tokenizer.NextChar))
                {
                    var value = tokenizer.ReadNumber();
                    if (value.Contains('.'))
                    {
                        var dVal = Double.Parse(value.ToString());
                        dbls.Add(dVal);
                    }
                    else
                    {
                        var iVal = Int32.Parse(value.ToString());
                        ints.Add(iVal);
                    }
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

            if (strings.Count > 0)
                AddField(fieldName, JsonFieldType.StringArray, strings.ToArray());
            else if (ints.Count > 0)
                AddField(fieldName, JsonFieldType.IntegerArray, ints.ToArray());
            else if (dbls.Count > 0)
                AddField(fieldName, JsonFieldType.DoubleArray, dbls.ToArray());
            else
                AddField(fieldName, JsonFieldType.ObjectArray, items.ToArray());
        }

        #endregion

        /// <summary>
        /// Gets the specified field from the object.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>Requested field, <c>null</c> if not found.</returns>
        public JsonField GetField(string fieldName)
        {
            JsonField field;
            _fields.TryGetValue(fieldName.ToLower(), out field);
            return field;
        }

        /// <summary>
        /// Adds a field to the object.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="type">The type of the field.</param>
        /// <param name="value">The value of the field.</param>
        public void AddField(string fieldName, JsonFieldType type, object value)
        {
            var field = new JsonField(fieldName, type, value);
            _fields = _fields.AddOrUpdate(fieldName.ToLower(), field);
        }

        /// <summary>
        /// Adds a string field to the object.
        /// </summary>
        public void AddField(string fieldName, string value)
        {
            AddField(fieldName, JsonFieldType.String, value);
        }

        /// <summary>
        /// Adds an integer field to the object.
        /// </summary>
        public void AddField(string fieldName, int? value)
        {
            AddField(fieldName, JsonFieldType.Integer, value);
        }

        /// <summary>
        /// Adds an integer array field to the object.
        /// </summary>
        public void AddField(string fieldName, int[] value)
        {
            AddField(fieldName, JsonFieldType.IntegerArray, value);
        }

        /// <summary>
        /// Adds a boolean field to the object.
        /// </summary>
        public void AddField(string fieldName, bool value)
        {
            AddField(fieldName, JsonFieldType.Boolean, value);
        }

        /// <summary>
        /// Adds a double field to the object.
        /// </summary>
        public void AddField(string fieldName, double? value)
        {
            AddField(fieldName, JsonFieldType.Double, value);
        }

        /// <summary>
        /// Adds a float field to the object.
        /// </summary>
        public void AddField(string fieldName, float? value)
        {
            if (value == null)
                AddField(fieldName, JsonFieldType.Double, null);
            else
                AddField(fieldName, JsonFieldType.Double, (double)value.GetValueOrDefault());
        }

        /// <summary>
        /// Adds a date field to the object.
        /// </summary>
        public void AddField(string fieldName, Date value)
        {
            if (value.IsEmpty)
                AddField(fieldName, JsonFieldType.Date, "0000-00-00Z");
            else
                AddField(fieldName, JsonFieldType.Date, value.ToString("yyyy-MM-dd") + 'Z');
        }

        /// <summary>
        /// Adds a date/time field to the object.
        /// </summary>
        public void AddField(string fieldName, DateTime? value)
        {
            if (value == null)
            {
                AddField(fieldName, JsonFieldType.DateTime, null);
            }
            else
            {
                var utc = value.GetValueOrDefault().ToUniversalTime();
                var asString = utc.ToString("yyyy-MM-dd") + 'T' + utc.ToString("HH:mm:ss.fff") + 'Z';
                AddField(fieldName, JsonFieldType.DateTime, asString);
            }
        }

        /// <summary>
        /// Adds an object field to the object.
        /// </summary>
        public void AddField(string fieldName, JsonObject value)
        {
            AddField(fieldName, JsonFieldType.Object, value);
        }

        /// <summary>
        /// Adds an object array field to the object.
        /// </summary>
        public void AddField(string fieldName, IEnumerable<JsonObject> value)
        {
            AddField(fieldName, JsonFieldType.ObjectArray, value);
        }
    }

    /// <summary>
    /// A single field of a <see cref="JsonObject"/>.
    /// </summary>
    [DebuggerDisplay("{FieldName,nq} = {Value} ({Type})")]
    public struct JsonField
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonField"/> struct.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="type">The type.</param>
        /// <param name="value">The value.</param>
        public JsonField(string fieldName, JsonFieldType type, object value)
        {
            _fieldName = fieldName;
            _type = type;
            _value = value;
        }

        private readonly string _fieldName;
        private readonly JsonFieldType _type;
        private readonly object _value;

        /// <summary>
        /// Gets the name of the field.
        /// </summary>
        public string FieldName { get { return _fieldName; } }

        /// <summary>
        /// Gets the type of the field.
        /// </summary>
        public JsonFieldType Type { get { return _type; } }

        internal object Value { get { return _value; } }

        /// <summary>
        /// Gets the value of the field if the type is <see cref="JsonFieldType.Object"/>.
        /// </summary>
        public JsonObject ObjectValue
        {
            get { return _value as JsonObject; }
        }

        /// <summary>
        /// Gets the value of the field if the type is <see cref="JsonFieldType.ObjectArray"/>.
        /// </summary>
        public IEnumerable<JsonObject> ObjectArrayValue
        {
            get { return _value as IEnumerable<JsonObject>; }
        }

        /// <summary>
        /// Gets the value of the field if the type is <see cref="JsonFieldType.String"/>.
        /// </summary>
        public string StringValue
        {
            get
            {
                if (_value == null)
                    return null;

                return _value.ToString();
            }
        }

        /// <summary>
        /// Gets the value of the field if the type is <see cref="JsonFieldType.Boolean"/>.
        /// </summary>
        public bool BooleanValue
        {
            get
            {
                if (_value is bool)
                    return (bool)_value;

                return false;
            }
        }

        /// <summary>
        /// Gets the value of the field if the type is <see cref="JsonFieldType.Integer"/>.
        /// </summary>
        public int? IntegerValue
        {
            get
            {
                if (_value is int)
                    return (int)_value;

                if (_value is string)
                {
                    int intValue;
                    if (Int32.TryParse((string)_value, out intValue))
                        return intValue;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the value of the field if the type is <see cref="JsonFieldType.IntegerArray"/>.
        /// </summary>
        public int[] IntegerArrayValue
        {
            get { return _value as int[]; }
        }

        /// <summary>
        /// Gets the value of the field if the type is <see cref="JsonFieldType.Double"/>.
        /// </summary>
        public double? DoubleValue
        {
            get
            {
                if (_value is double)
                    return (double)_value;

                if (_value is int)
                    return (double)_value;

                return null;
            }
        }

        /// <summary>
        /// Gets the value of the field if the type is <see cref="JsonFieldType.Date"/>.
        /// </summary>
        public Date DateValue
        {
            get
            {
                if (_value is Date)
                    return (Date)_value;

                var strValue = _value as string;
                if (strValue != null && strValue.Length == 11 && strValue[10] == 'Z' && strValue[4] == '-' && strValue[7] == '-')
                {
                    int year, month, day;
                    if (Int32.TryParse(strValue.Substring(0, 4), out year) &&
                        Int32.TryParse(strValue.Substring(5, 2), out month) &&
                        Int32.TryParse(strValue.Substring(8, 2), out day))
                    {
                        return new Date(month, day, year);
                    }
                }

                return Date.Empty;
            }
        }

        /// <summary>
        /// Gets the value of the field if the type is <see cref="JsonFieldType.DateTime"/>.
        /// </summary>
        public DateTime? DateTimeValue
        {
            get
            {
                if (_value is DateTime)
                    return (DateTime)_value;

                var strValue = _value as string;
                if (strValue == null)
                    return null;

                if (strValue.Length == 24 && strValue[23] == 'Z' && strValue[4] == '-' && strValue[7] == '-' &&
                    strValue[10] == 'T' && strValue[13] == ':' && strValue[16] == ':' && strValue[19] == '.')
                {
                    int year, month, day, hour, minute, second, millisecond;
                    if (Int32.TryParse(strValue.Substring(0, 4), out year) &&
                        Int32.TryParse(strValue.Substring(5, 2), out month) &&
                        Int32.TryParse(strValue.Substring(8, 2), out day) &&
                        Int32.TryParse(strValue.Substring(11, 2), out hour) &&
                        Int32.TryParse(strValue.Substring(14, 2), out minute) &&
                        Int32.TryParse(strValue.Substring(17, 2), out second) &&
                        Int32.TryParse(strValue.Substring(20, 3), out millisecond))
                    {
                        return new DateTime(year, month, day, hour, minute, second, millisecond, DateTimeKind.Utc);
                    }
                }

                DateTime value;
                if (DateTime.TryParse(strValue, out value))
                    return value;

                return null;
            }
        }

        /// <summary>
        /// Infers the <see cref="JsonFieldType"/> from a <see cref="Type"/>.
        /// </summary>
        /// <exception cref="NotSupportedException">The provided <paramref name="t"/> doesn't map to a <see cref="JsonFieldType"/>.</exception>
        public static JsonFieldType GetFieldType(Type t)
        {
            if (t == typeof(int) || t == typeof(int?))
                return JsonFieldType.Integer;

            if (t == typeof(string))
                return JsonFieldType.String;

            if (t == typeof(double) || t == typeof(double?) || t == typeof(float) || t == typeof(float?))
                return JsonFieldType.Double;

            if (t == typeof(bool))
                return JsonFieldType.Boolean;

            if (t == typeof(int[]))
                return JsonFieldType.IntegerArray;

            if (t == typeof(Date))
                return JsonFieldType.Date;

            if (t == typeof(DateTime) || t == typeof(DateTime?))
                return JsonFieldType.DateTime;

            if (t.IsEnum)
                return JsonFieldType.Integer;

            throw new NotSupportedException("No JSON type mapping for " + t.Name);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public enum JsonFieldType
    {
        /// <summary>
        /// Undefined.
        /// </summary>
        None = 0,

        /// <summary>
        /// No value.
        /// </summary>
        Null,

        /// <summary>
        /// An object.
        /// </summary>
        Object,

        /// <summary>
        /// An array of objects.
        /// </summary>
        ObjectArray,

        /// <summary>
        /// A string.
        /// </summary>
        String,

        /// <summary>
        /// An array of strings.
        /// </summary>
        StringArray,

        /// <summary>
        /// A true/false value.
        /// </summary>
        Boolean,

        /// <summary>
        /// An integral numbers.
        /// </summary>
        Integer,

        /// <summary>
        /// An array of integral numbers.
        /// </summary>
        IntegerArray,

        /// <summary>
        /// A non-integral numbers.
        /// </summary>
        Double,

        /// <summary>
        /// An array of non-integral numbers.
        /// </summary>
        DoubleArray,

        /// <summary>
        /// A date.
        /// </summary>
        Date,

        /// <summary>
        /// A date and time.
        /// </summary>
        DateTime,
    }
}
