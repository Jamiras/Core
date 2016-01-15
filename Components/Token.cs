using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Jamiras.Components
{
    [DebuggerDisplay("{DebugString}")]
    public struct Token
    {
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
        /// Gets the string represented by the token.
        /// </summary>
        public override string ToString()
        {
            if (_start != 0 || _length != _source.Length)
            {
                _source = _source.Substring(_start, _length);
                _start = 0;
            }

            return _source;
        }

        private string DebugString
        {
            get { return _source.Substring(_start, _length); }
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
            int longer = Math.Max(_length, value.Length);
            return String.Compare(_source, _start, value, 0, longer);
        }

        /// <summary>
        /// Compares the token to a string.
        /// </summary>
        public int CompareTo(string value, StringComparison comparisonType)
        {
            int longer = Math.Max(_length, value.Length);
            return String.Compare(_source, _start, value, 0, longer, comparisonType);
        }

        /// <summary>
        /// Compares the token to another token.
        /// </summary>
        public int CompareTo(Token value)
        {
            int longer = Math.Max(_length, value.Length);
            return String.Compare(_source, _start, value._source, value._start, longer);
        }

        /// <summary>
        /// Compares the token to another token.
        /// </summary>
        public int CompareTo(Token value, StringComparison comparisonType)
        {
            int longer = Math.Max(_length, value.Length);
            return String.Compare(_source, _start, value._source, value._start, longer, comparisonType);
        }

        /// <summary>
        /// Compares the token to a string.
        /// </summary>
        public static bool operator ==(Token token, string str)
        {
            if (token._length != str.Length)
                return false;

            return (token.CompareTo(str) == 0);
        }

        /// <summary>
        /// Compares the token to a string.
        /// </summary>
        public static bool operator !=(Token token, string str)
        {
            if (token._length != str.Length)
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

        public override int GetHashCode()
        {
            return _source.GetHashCode() ^ (_start + 1) ^ ((_start + 1) * (_length + 1));
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
            if (value.Length < _length)
                return false;

            return String.Compare(_source, _start, value, 0, value.Length) == 0;
        }

        /// <summary>
        /// Determines if the token starts with a string value.
        /// </summary>
        public bool StartsWith(string value, StringComparison comparisonType)
        {
            if (value.Length < _length)
                return false;

            return String.Compare(_source, _start, value, 0, value.Length, comparisonType) == 0;
        }

        /// <summary>
        /// Determines if the token ends with a string value.
        /// </summary>
        public bool EndsWith(string value)
        {
            if (value.Length < _length)
                return false;

            return String.Compare(_source, _start + _length - value.Length, value, 0, value.Length) == 0;
        }

        /// <summary>
        /// Determines if the token ends with a string value.
        /// </summary>
        public bool EndsWith(string value, StringComparison comparisonType)
        {
            if (value.Length < _length)
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

            int index = _source.IndexOf(value, _start + startIndex, _length);
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

            int index = _source.IndexOf(value, _start + startIndex, _length);
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

            int index = _source.IndexOf(value, _start + startIndex, _length, comparisonType);
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

            return new Token(_source, _start + start, _length - _start);
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
            var tokens = new List<Token>();
            var start = _start;
            var end = _start + Length;
            var scan = start;
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
                isSeparator = c =>
                {
                    foreach (var s in separator)
                    {
                        if (c == s)
                            return true;
                    }

                    return false;
                };
            }

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
