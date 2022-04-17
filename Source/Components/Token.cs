using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Jamiras.Components
{
    /// <summary>
    /// Represents a section of a larger <see cref="string"/> without creating a new <see cref="string"/> instance.
    /// </summary>
    [DebuggerDisplay("{DebugString}")]
    public struct Token
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Token"/> struct.
        /// </summary>
        /// <param name="source">The source string.</param>
        /// <param name="start">The index of the first character of the Token within the source string.</param>
        /// <param name="length">The length of the Token.</param>
        /// <exception cref="ArgumentNullException">source</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="start"/> or <paramref name="length"/> do not evaluate to valid positions within the source string.
        /// </exception>
        public Token(string source, int start, int length)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (start < 0 || start > source.Length)
                throw new ArgumentOutOfRangeException("start");
            if (length < 0 || start + length > source.Length)
                throw new ArgumentOutOfRangeException("length");

            _source = source;
            _start = start;
            _length = length;
        }

        private string _source;
        private int _start;
        private readonly int _length;

        /// <summary>
        /// Gets a token representing an empty string.
        /// </summary>
        public static Token Empty
        {
            get { return _empty; }
        }
        private static Token _empty = new Token(String.Empty, 0, 0);

        /// <summary>
        /// Gets the string represented by the token.
        /// </summary>
        public override string ToString()
        {
            if (_length == 0)
                return String.Empty;

            if (_start != 0 || _length != _source.Length)
            {
                _source = _source.Substring(_start, _length);
                _start = 0;
            }

            return _source;
        }

        // internal for unit tests
        internal string DebugString
        {
            get { return (_length > 0) ? _source.Substring(_start, _length) : String.Empty; }
        }

        internal string Source
        {
            get { return _source; }
        }

        internal int Start
        {
            get { return _start; }
        }

        /// <summary>
        /// Gets the length of the token.
        /// </summary>
        public int Length
        {
            get { return _length; }
        }

        /// <summary>
        /// Gets whether the string is empty.
        /// </summary>
        public bool IsEmpty
        {
            get { return _length == 0; }
        }

        /// <summary>
        /// Gets whether the string is empty or made entirely of whitespace characters.
        /// </summary>
        public bool IsEmptyOrWhitespace
        {
            get
            {
                for (int i = 0; i < _length; i++)
                {
                    if (!Char.IsWhiteSpace(_source[_start + i]))
                        return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Compares the token to a string.
        /// </summary>
        public int CompareTo(string value)
        {
            // can only compare the part of the string that is contained in the token
            var result = String.Compare(_source, _start, value, 0, _length);

            // if it's an exact match and value is longer, need to return -1
            if (result == 0 && value.Length > _length)
                result = -1;

            return result;
        }

        /// <summary>
        /// Compares the token to a string.
        /// </summary>
        public int CompareTo(string value, StringComparison comparisonType)
        {
            // can only compare the part of the string that is contained in the token
            var result = String.Compare(_source, _start, value, 0, _length, comparisonType);

            // if it's an exact match and value is longer, need to return -1
            if (result == 0 && value.Length > _length)
                result = -1;

            return result;
        }

        /// <summary>
        /// Compares the token to another token.
        /// </summary>
        public int CompareTo(Token value)
        {
            // can only compare the parts of the string that are contained in the tokens
            int shorter = Math.Min(_length, value.Length);
            var result = String.Compare(_source, _start, value._source, value._start, shorter);

            // if it's an exact match and one is longer, need to return size difference
            if (result == 0)
            {
                if (_length < value.Length)
                    result = -1;
                else if (_length > value.Length)
                    result = 1;
            }

            return result;
        }

        /// <summary>
        /// Compares the token to another token.
        /// </summary>
        public int CompareTo(Token value, StringComparison comparisonType)
        {
            // can only compare the parts of the string that are contained in the tokens
            int shorter = Math.Min(_length, value.Length);
            var result = String.Compare(_source, _start, value._source, value._start, shorter, comparisonType);

            // if it's an exact match and one is longer, need to return size difference
            if (result == 0)
            {
                if (_length < value.Length)
                    result = -1;
                else if (_length > value.Length)
                    result = 1;
            }

            return result;
        }

        /// <summary>
        /// Compares the token to a string.
        /// </summary>
        public static bool operator ==(Token token, string str)
        {
            if (str == null || token._length != str.Length)
                return false;

            return (token.CompareTo(str) == 0);
        }

        /// <summary>
        /// Compares the token to a string.
        /// </summary>
        public static bool operator !=(Token token, string str)
        {
            if (str == null || token._length != str.Length)
                return true;

            return (token.CompareTo(str) != 0);
        }

        /// <summary>
        /// Compares the token to another token.
        /// </summary>
        public static bool operator ==(Token token, Token token2)
        {
            if (token._length != token2._length)
                return false;

            return (token.CompareTo(token2) == 0);
        }

        /// <summary>
        /// Compares the token to another token.
        /// </summary>
        public static bool operator !=(Token token, Token token2)
        {
            if (token._length != token2._length)
                return true;

            return (token.CompareTo(token2) != 0);
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is Token)
                return this == (Token)obj;
            else if (obj is string)
                return this == (string)obj;

            return false;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        public override int GetHashCode()
        {
            // we want the output of GetHashCode to match String.GetHashCode.
            // unfortunately, this means we have to generate the String.
            return ToString().GetHashCode();
        }

        /// <summary>
        /// Gets the character at the specified index.
        /// </summary>
        public char this[int index]
        {
            get
            {
                if (index < 0 || index >= _length)
                    throw new ArgumentOutOfRangeException("index");

                return _source[_start + index];
            }
        }

        /// <summary>
        /// Determines if the token starts with a string value.
        /// </summary>
        public bool StartsWith(string value)
        {
            if (value.Length > _length)
                return false;

            return String.Compare(_source, _start, value, 0, value.Length) == 0;
        }

        /// <summary>
        /// Determines if the token starts with a token value.
        /// </summary>
        public bool StartsWith(Token value)
        {
            if (value.Length > _length)
                return false;

            return String.Compare(_source, _start, value._source, value._start, value._length) == 0;
        }

        /// <summary>
        /// Determines if the token starts with a string value.
        /// </summary>
        public bool StartsWith(string value, StringComparison comparisonType)
        {
            if (value.Length > _length)
                return false;

            return String.Compare(_source, _start, value, 0, value.Length, comparisonType) == 0;
        }

        /// <summary>
        /// Determines if the token ends with a string value.
        /// </summary>
        public bool EndsWith(string value)
        {
            if (value.Length > _length)
                return false;

            return String.Compare(_source, _start + _length - value.Length, value, 0, value.Length) == 0;
        }

        /// <summary>
        /// Determines if the token ends with a token value.
        /// </summary>
        public bool EndsWith(Token value)
        {
            if (value.Length > _length)
                return false;

            return String.Compare(_source, _start + _length - value.Length, value._source, value._start, value._length) == 0;
        }

        /// <summary>
        /// Determines if the token ends with a string value.
        /// </summary>
        public bool EndsWith(string value, StringComparison comparisonType)
        {
            if (value.Length > _length)
                return false;

            return String.Compare(_source, _start + _length - value.Length, value, 0, value.Length, comparisonType) == 0;
        }

        /// <summary>
        /// Gets the first index of the requested character.
        /// </summary>
        public int IndexOf(char value)
        {
            int index = _source.IndexOf(value, _start, _length);
            return (index >= 0) ? (index - _start) : -1;
        }

        /// <summary>
        /// Gets the first index of the requested character, starting at the specified position.
        /// </summary>
        public int IndexOf(char value, int startIndex)
        {
            if (startIndex < 0 || startIndex > _length)
                throw new ArgumentOutOfRangeException("startIndex");

            int index = _source.IndexOf(value, _start + startIndex, _length - startIndex);
            return (index >= 0) ? (index - _start) : -1;
        }

        /// <summary>
        /// Gets the first index of the requested string.
        /// </summary>
        public int IndexOf(string value)
        {
            int index = _source.IndexOf(value, _start, _length);
            return (index >= 0) ? (index - _start) : -1;
        }

        /// <summary>
        /// Gets the first index of the requested string, starting at the specified position.
        /// </summary>
        public int IndexOf(string value, int startIndex)
        {
            if (startIndex < 0 || startIndex > _length)
                throw new ArgumentOutOfRangeException("startIndex");

            int index = _source.IndexOf(value, _start + startIndex, _length - startIndex);
            return (index >= 0) ? (index - _start) : -1;
        }

        /// <summary>
        /// Gets the first index of the requested string.
        /// </summary>
        public int IndexOf(string value, StringComparison comparisonType)
        {
            int index = _source.IndexOf(value, _start, _length, comparisonType);
            return (index >= 0) ? (index - _start) : -1;
        }

        /// <summary>
        /// Gets the first index of the requested string, starting at the specified position.
        /// </summary>
        public int IndexOf(string value, int startIndex, StringComparison comparisonType)
        {
            if (startIndex < 0 || startIndex > _length)
                throw new ArgumentOutOfRangeException("startIndex");

            int index = _source.IndexOf(value, _start + startIndex, _length - startIndex, comparisonType);
            return (index >= 0) ? (index - _start) : -1;
        }

        /// <summary>
        /// Determines if the specified character exists within the token.
        /// </summary>
        public bool Contains(char value)
        {
            return (IndexOf(value) >= 0);
        }

        /// <summary>
        /// Determines if the specified string exists within the token.
        /// </summary>
        public bool Contains(string value)
        {
            return (IndexOf(value) >= 0);
        }

        /// <summary>
        /// Determines if the specified string exists within the token.
        /// </summary>
        public bool Contains(string value, StringComparison comparisonType)
        {
            return (IndexOf(value, comparisonType) >= 0);
        }

        /// <summary>
        /// Creates a second token from the contents of a token.
        /// </summary>
        public Token SubToken(int start)
        {
            if (start < 0 || start > _length)
                throw new ArgumentOutOfRangeException("start");

            return new Token(_source, _start + start, _length - start);
        }

        /// <summary>
        /// Creates a second token from the contents of a token.
        /// </summary>
        public Token SubToken(int start, int length)
        {
            if (start < 0 || start > _length)
                throw new ArgumentOutOfRangeException("start");
            if (length < 0 || start + length > _length)
                throw new ArgumentOutOfRangeException("length");

            return new Token(_source, _start + start, length);
        }

        /// <summary>
        /// Creates a string from the contents of a token.
        /// </summary>
        public string Substring(int start)
        {
            return SubToken(start).ToString();
        }

        /// <summary>
        /// Creates a string from the contents of a token.
        /// </summary>
        public string Substring(int start, int length)
        {
            return SubToken(start, length).ToString();
        }

        /// <summary>
        /// Returns a token representing the portion of the current token that does not include initial whitespace characters.
        /// </summary>
        public Token TrimLeft()
        {
            var start = _start;
            var end = _start + Length;
            while (start < end && Char.IsWhiteSpace(_source[start]))
                start++;

            return new Token(_source, start, end - start);
        }

        /// <summary>
        /// Returns a token representing the portion of the current token that does not include trailing whitespace characters.
        /// </summary>
        public Token TrimRight()
        {
            var start = _start;
            var end = _start + Length;
            while (end > start && Char.IsWhiteSpace(_source[end - 1]))
                end--;

            return new Token(_source, start, end - start);
        }

        /// <summary>
        /// Returns a token representing the portion of the current token that does not include initial or trailing whitespace characters.
        /// </summary>
        public Token Trim()
        {
            var start = _start;
            var end = _start + Length;
            while (start < end && Char.IsWhiteSpace(_source[start]))
                start++;
            while (end > start && Char.IsWhiteSpace(_source[end - 1]))
                end--;

            return new Token(_source, start, end - start);
        }

        /// <summary>
        /// Returns a collection of subtokens representing the portions of the token separated by the specified separator characters.
        /// </summary>
        public Token[] Split(params char[] separator)
        {
            return Split(separator, StringSplitOptions.None);
        }

        /// <summary>
        /// Returns a collection of subtokens representing the portions of the token separated by the specified separator characters.
        /// </summary>
        public Token[] Split(char[] separator, StringSplitOptions options)
        {
            Predicate<char> isSeparator;

            if (separator.Length == 1)
            {
                char c1 = separator[0];
                isSeparator = c => (c == c1);
            }
            else if (separator.Length == 2)
            {
                char c1 = separator[0];
                char c2 = separator[1];
                isSeparator = c => (c == c1 || c == c2);
            }
            else
            {
                char[] sorted_separators = new char[separator.Length];
                Array.Copy(separator, sorted_separators, separator.Length);
                Array.Sort(sorted_separators);

                isSeparator = c => (Array.BinarySearch(sorted_separators, c) >= 0);
            }

            return Split(isSeparator, options);
        }

        internal Token[] Split(Predicate<char> isSeparator, StringSplitOptions options)
        {
            // Estimate the initial capacity of the token array to avoid as much resizing as possible.
            // Assume the average word length is 3 characters, and one character for whitespace between 
            // words (division by 4 is also fast because it's a bit shift). The list may still grow if
            // the input string is mostly small words, or it may be slightly large if the input string
            // is mostly large words.
            var capacity = (Length + 7) / 4; // +7 to round up and add 1, ensuring the smallest capacity is 2.
            var tokens = new List<Token>(capacity);

            var start = _start;
            var scan = start;
            var end = _start + Length;

            while (scan < end)
            {
                if (isSeparator(_source[scan]))
                {
                    if (scan > start || (options & StringSplitOptions.RemoveEmptyEntries) == 0)
                        tokens.Add(new Token(_source, start, scan - start));

                    start = scan + 1;
                }

                scan++;
            }

            if (start < end)
                tokens.Add(new Token(_source, start, end - start));

            return tokens.ToArray();
        }
    }
}
