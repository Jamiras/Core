using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Jamiras.Components
{
    public abstract class Tokenizer
    {
        public static Tokenizer CreateTokenizer(string input)
        {
            return new StringTokenizer(input);
        }

        internal class StringTokenizer : Tokenizer
        {
            public StringTokenizer(string input)
            {
                _input = input;
                Advance();
            }

            private readonly string _input;
            private int _inputIndex;
            private int _tokenStart;

            protected override void StartToken()
            {
                _tokenStart = _inputIndex - 1;
            }

            protected override Token EndToken()
            {
                return new Token(_input, _tokenStart, _inputIndex - _tokenStart - 1);
            }

            public override void Advance()
            {
                if (_inputIndex < _input.Length)
                {
                    NextChar = _input[_inputIndex++];
                }
                else
                {
                    NextChar = '\0';
                    _inputIndex = _input.Length + 1;
                }
            }

            public override Token ReadQuotedString()
            {
                if (NextChar != '"')
                    throw new InvalidOperationException("expecting quote, found " + NextChar);

                Advance();
                StartToken();
                while (NextChar != '"')
                {
                    if (NextChar == '\\')
                    {
                        // need to process the string, reset to opening quote and let the base class handle it.
                        _inputIndex = _tokenStart;
                        NextChar = '\"';
                        return base.ReadQuotedString();
                    }
                    else if (NextChar == 0)
                    {
                        throw new InvalidOperationException("closing quote not found for quoted string");
                    }

                    Advance();
                }

                var token = EndToken();
                Advance();
                return token;
            }

            public override int MatchSubstring(string token)
            {
                int start = _inputIndex - 1;
                int end = start + token.Length;
                if (end > _input.Length)
                    end = _input.Length;
                int count = end - start;

                for (int i = 0; i < count; i++)
                {
                    if (_input[start + i] != token[i])
                        return i;
                }

                return count;
            }
        }

        public static Tokenizer CreateTokenizer(Stream input)
        {
            return new StreamTokenizer(input);
        }

        internal class StreamTokenizer : Tokenizer
        {
            public StreamTokenizer(Stream input)
            {
                _stream = input;
                _bufferedChars = new List<char>();
                Advance();
            }

            private readonly Stream _stream;
            private readonly List<char> _bufferedChars;
            private StringBuilder _tokenBuilder;

            protected override void StartToken()
            {
                _tokenBuilder = new StringBuilder();
            }

            protected override Token EndToken()
            {
                if (_tokenBuilder == null || _tokenBuilder.Length == 0)
                    return new Token();

                var str = _tokenBuilder.ToString();
                var token = new Token(str, 0, str.Length);
                _tokenBuilder = null;
                return token;
            }

            /// <summary>
            /// Advances to the next character in the stream.
            /// </summary>
            public override void Advance()
            {
                if (_tokenBuilder != null)
                    _tokenBuilder.Append(NextChar);

                if (_bufferedChars.Count > 0)
                {
                    NextChar = _bufferedChars[0];
                    _bufferedChars.RemoveAt(0);
                    return;
                }

                NextChar = ReadChar();
            }

            private char ReadChar()
            {
                var b = _stream.ReadByte();
                if (b < 0)
                    return (char)0x00;

                if (b < 0x80)
                    return (char)b;

                if (b >= 0xC0)
                {
                    if (b < 0xE0)
                    {
                        var b2 = _stream.ReadByte();
                        return (char)((b << 5) | (b2 & 0x1F));
                    }

                    if (b < 0xF0)
                    {
                        var b2 = _stream.ReadByte();
                        var b3 = _stream.ReadByte();
                        return (char)((b << 10) | ((b2 & 0x1F) << 5) | (b3 & 0x1F));
                    }
                }

                return (char)0xFFFD;
            }

            /// <summary>
            /// Attempts to match as much of the provided token as possible.
            /// </summary>
            /// <param name="token">token to match</param>
            /// <returns>number of matching characters. tokenizer is not advanced.</returns>
            public override int MatchSubstring(string token)
            {
                if (token.Length == 0 || NextChar != token[0])
                    return 0;

                int bufferIndex = 0;
                int tokenIndex = 1;

                while (bufferIndex < _bufferedChars.Count)
                {
                    if (tokenIndex == token.Length)
                        return tokenIndex;

                    if (_bufferedChars[bufferIndex] != token[tokenIndex])
                        return tokenIndex;

                    bufferIndex++;
                    tokenIndex++;
                }

                while (tokenIndex < token.Length)
                {
                    var c = ReadChar();
                    _bufferedChars.Add(c);
                    if (token[tokenIndex] != c)
                        return tokenIndex;

                    tokenIndex++;
                }

                return tokenIndex;
            }
        }

        /// <summary>
        /// Gets the next character in the stream.
        /// </summary>
        public Char NextChar { get; protected set; }

        /// <summary>
        /// Advances to the next character in the source.
        /// </summary>
        public abstract void Advance();

        protected abstract void StartToken();

        protected abstract Token EndToken();

        protected static Token CreateToken(StringBuilder builder)
        {
            if (builder.Length == 0)
                return new Token();

            var str = builder.ToString();
            return new Token(str, 0, str.Length);
        }

        /// <summary>
        /// Advances to the next non-whitespace character in the source.
        /// </summary>
        public void SkipWhitespace()
        {
            while (Char.IsWhiteSpace(NextChar))
                Advance();
        }
        
        /// <summary>
        /// Matches a token containing alphanumeric characters and/or underscores, or a quoted string.
        /// </summary>
        public Token ReadValue()
        {
            if (NextChar == '"') 
                return ReadQuotedString();
            
            if (Char.IsDigit(NextChar))
                return ReadNumber();

            return ReadIdentifier();
        }

        /// <summary>
        /// Matches a token containing alphanumeric characters and/or underscores.
        /// </summary>
        public Token ReadIdentifier()
        {
            StartToken();
            if (Char.IsLetter(NextChar) || NextChar == '_')
            {
                while (Char.IsLetterOrDigit(NextChar) || NextChar == '_')
                    Advance();
            }

            return EndToken();
        }

        /// <summary>
        /// Matches a token containing numeric characters, possibly with a single decimal separator.
        /// </summary>
        public Token ReadNumber()
        {
            if (!Char.IsDigit(NextChar))
                return new Token();

            StartToken();
            while (Char.IsDigit(NextChar))
                Advance();

            if (NextChar == '.')
            {
                Advance();

                while (Char.IsDigit(NextChar))
                    Advance();
            }

            return EndToken();
        }

        /// <summary>
        /// Scans the input for the requested characters and creates a token of everything up to the first match.
        /// </summary>
        public Token ReadTo(params char[] chars)
        {
            StartToken();

            if (chars.Length == 1)
            {
                var c = chars[0];
                while (NextChar != c && NextChar != '\0')
                    Advance();
            }
            else if (chars.Length == 2)
            {
                var c1 = chars[0];
                var c2 = chars[1];
                while (NextChar != c1 && NextChar != c2 && NextChar != '\0')
                    Advance();
            }
            else
            {
                while (!chars.Contains(NextChar) && NextChar != '\0')
                    Advance();
            }

            return EndToken();
        }

        /// <summary>
        /// Matches a quoted string.
        /// </summary>
        public virtual Token ReadQuotedString()
        {
            if (NextChar != '"')
                throw new InvalidOperationException("expecting quote, found " + NextChar);

            Advance();
            var builder = new StringBuilder();
            while (NextChar != '"')
            {
                if (NextChar == '\\')
                {
                    Advance();
                    switch (NextChar)
                    {
                        case 't':
                            builder.Append("\t");
                            Advance();
                            continue;
                        case 'n':
                            builder.AppendLine();
                            Advance();
                            continue;
                        case 'r':
                            Advance();
                            continue;
                    }
                }
                else if (NextChar == 0)
                {
                    throw new InvalidOperationException("closing quote not found for quoted string");
                }

                builder.Append(NextChar);
                Advance();
            }

            Advance();
            return CreateToken(builder);
        }

        /// <summary>
        /// Attempts to match a string against the next few characters of the input.
        /// </summary>
        /// <param name="token">token to match</param>
        /// <returns><c>true</c> if the token matches (tokenizer will advance over the token), <c>false</c> if not.</returns>
        public bool Match(string token)
        {
            int matchingChars = MatchSubstring(token);
            if (matchingChars != token.Length)
                return false;

            for (int i = 0; i < token.Length; i++)
                Advance();

            return true;
        }

        /// <summary>
        /// Attempts to match as much of the provided token as possible.
        /// </summary>
        /// <param name="token">token to match</param>
        /// <returns>number of matching characters. tokenizer is not advanced.</returns>
        public abstract int MatchSubstring(string token);

        /// <summary>
        /// Splits the provided <param name="input"/> string at <paramref name="separator"/> boundaries.
        /// </summary>
        public static Token[] Split(string input, params char[] separator)
        {
            return Split(input, separator, StringSplitOptions.None);
        }

        /// <summary>
        /// Splits the provided <paramref name="input"/> string at <paramref name="separator"/> boundaries.
        /// </summary>
        public static Token[] Split(string input, char[] separator, StringSplitOptions options)
        {
            var token = new Token(input, 0, input.Length);
            return token.Split(separator, options);
        }

        public static char[] WordSeparators = new[] { ' ', '\n', '\t', '\r', '(', ')', ',', '.', '!', ':', ';', '[', ']' };

        /// <summary>
        /// Gets the <paramref name="count"/> longest words from <paramref name="input"/>
        /// </summary>
        public static Token[] GetLongestWords(string input, int count)
        {
            var sortedTokens = new Token[count];
            var tokens = new List<Token>(count);

            string[] ignoreWords = { "a", "an", "in", "it", "of", "on", "or", "the", "to" };

            foreach (var word in Tokenizer.Split(input, WordSeparators, StringSplitOptions.RemoveEmptyEntries))
            {
                if (word.Length <= 3 && ignoreWords.Any(w => word.CompareTo(w, StringComparison.OrdinalIgnoreCase) == 0))
                    continue;

                for (int i = 0; i < count; i++)
                {
                    if (sortedTokens[i].Length == 0)
                    {
                        sortedTokens[i] = word;
                        tokens.Add(word);
                        break;
                    }
                    else if (sortedTokens[i].Length < word.Length)
                    {
                        if (tokens.Count == count)
                            tokens.Remove(sortedTokens[count - 1]);

                        Array.Copy(sortedTokens, i, sortedTokens, i + 1, count - i - 1);
                        sortedTokens[i] = word;
                        tokens.Add(word);
                        break;
                    }
                    else if (sortedTokens[i].CompareTo(word, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        break;
                    }
                }
            }

            return tokens.ToArray();
        }
    }
}
