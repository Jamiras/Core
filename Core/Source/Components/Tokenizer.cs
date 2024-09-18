using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Jamiras.Components
{
    /// <summary>
    /// A class for parsing text data.
    /// </summary>
    public abstract class Tokenizer
    {
        /// <summary>
        /// Creates a tokenizer from a <see cref="string"/>.
        /// </summary>
        public static Tokenizer CreateTokenizer(string input)
        {
            return new StringTokenizer(input, 0, input.Length);
        }

        /// <summary>
        /// Creates a tokenizer from part of a <see cref="string"/>.
        /// </summary>
        public static Tokenizer CreateTokenizer(string input, int start, int length)
        {
            return new StringTokenizer(input, start, length);
        }

        /// <summary>
        /// Creates a tokenizer from a <see cref="Token"/>.
        /// </summary>
        public static Tokenizer CreateTokenizer(Token token)
        {
            return new StringTokenizer(token.Source, token.Start, token.Length);
        }

        internal class StringTokenizer : Tokenizer
        {
            public StringTokenizer(string input, int start, int length)
            {
                _input = input;
                _inputIndex = start;
                _stop = start + length;
                Advance();
            }

            private readonly string _input;
            private readonly int _stop;
            private int _inputIndex;
            private int _tokenStart;

            public override string ToString()
            {
                var builder = new StringBuilder();
                builder.Append(GetType().Name);
                builder.Append(' ');
                builder.Append('"');

                if (NextChar != '\0')
                {
                    builder.Append(NextChar);

                    if (_stop - _inputIndex > 19)
                    {
                        builder.Append(_input.Substring(_inputIndex, 16));
                        builder.Append("...");
                    }
                    else
                    {
                        builder.Append(_input.Substring(_inputIndex, _stop - _inputIndex));
                    }
                }

                builder.Append('"');
                return builder.ToString();
            }

            internal override void StartToken()
            {
                _tokenStart = _inputIndex - 1;
            }

            internal override Token EndToken()
            {
                return new Token(_input, _tokenStart, _inputIndex - _tokenStart - 1);
            }

            public override void Advance()
            {
                if (_inputIndex < _stop)
                {
                    NextChar = _input[_inputIndex++];
                }
                else
                {
                    NextChar = '\0';
                    _inputIndex = _stop + 1;
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
                if (end > _stop)
                    end = _stop;
                int count = end - start;

                for (int i = 0; i < count; i++)
                {
                    if (_input[start + i] != token[i])
                        return i;
                }

                return count;
            }

            protected override object CreateState()
            {
                return _inputIndex;
            }

            protected override void RestoreState(object state)
            {
                if (state is int)
                {
                    _inputIndex = (int)state - 1;
                    Advance(); // populate NextChar
                }
            }
        }

        /// <summary>
        /// Creates a tokenizer from a <see cref="Stream"/>.
        /// </summary>
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

            internal override void StartToken()
            {
                _tokenBuilder = new StringBuilder();
            }

            internal override Token EndToken()
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
                        return (char)(((b & 0x1F) << 6) | (b2 & 0x3F));
                    }

                    if (b < 0xF0)
                    {
                        var b2 = _stream.ReadByte();
                        var b3 = _stream.ReadByte();
                        return (char)(((b & 0x1F) << 12) | ((b2 & 0x3F) << 6) | (b3 & 0x3F));
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

            private class StreamState
            {
                public long Position;
                public char NextChar;
                public char[] BufferedChars;
            }

            protected override object CreateState()
            {
                var state = new StreamState
                {
                    Position = _stream.Position,
                    NextChar = NextChar
                };

                if (_bufferedChars.Count > 0)
                    state.BufferedChars = _bufferedChars.ToArray();

                return state;
            }

            protected override void RestoreState(object state)
            {
                var streamState = state as StreamState;
                if (streamState != null)
                {
                    _stream.Position = streamState.Position;
                    NextChar = streamState.NextChar;

                    _bufferedChars.Clear();
                    if (streamState.BufferedChars != null)
                        _bufferedChars.AddRange(streamState.BufferedChars);
                }
            }
        }

        private Stack<object> _state;

        /// <summary>
        /// Captures the current state of the Tokenizer so it can be restored by <see cref="PopState"/>.
        /// </summary>
        public void PushState()
        {
            if (_state == null)
                _state = new Stack<object>();

            _state.Push(CreateState());
        }

        internal object CreateStateInternal() { return CreateState(); }

        /// <summary>
        /// Captures the current state of the tokenizer.
        /// </summary>
        /// <returns>An object that can be passed to <see cref="RestoreState"/> to return the tokenizer to the current state.</returns>
        protected abstract object CreateState();

        /// <summary>
        /// Restores the current state of the Tokenizer after a call to <see cref="PushState"/>.
        /// </summary>
        public void PopState()
        {
            if (_state != null && _state.Count > 0)
                RestoreState(_state.Pop());
        }

        internal void RestoreStateInternal(object state) { RestoreState(state); }

        /// <summary>
        /// Restores the state of the tokenizer to some previous state.
        /// </summary>
        /// <returns>An object that was captured by <see cref="CreateState"/> representing the state of the tokenizer when it was captured.</returns>
        protected abstract void RestoreState(object state);

        /// <summary>
        /// Gets the next character in the stream.
        /// </summary>
        public Char NextChar { get; protected set; }

        /// <summary>
        /// Advances to the next character in the source.
        /// </summary>
        public abstract void Advance();

        /// <summary>
        /// Advances the specified number of characters in the source.
        /// </summary>
        public void Advance(int count)
        {
            for (int i = 0; i < count; i++)
                Advance();
        }

        internal abstract void StartToken();

        internal abstract Token EndToken();

        /// <summary>
        /// Creates a <see cref="Token"/> from a <see cref="StringBuilder"/>.
        /// </summary>
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
        /// Scans the input for the requested string and creates a token of everything up to the first match.
        /// </summary>
        public Token ReadTo(string needle)
        {
            StartToken();

            do
            {
                while (NextChar != needle[0])
                {
                    if (NextChar == '\0')
                        return EndToken();

                    Advance();
                }

                int matchingChars = MatchSubstring(needle);
                if (matchingChars == needle.Length)
                    return EndToken();

                Advance();
            } while (true);
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
                        case 'u':
                            Advance();

                            string hex = "";
                            hex += NextChar;
                            Advance();
                            hex += NextChar;
                            Advance();
                            hex += NextChar;
                            Advance();
                            hex += NextChar;
                            Advance();

                            int value;
                            if (Int32.TryParse(hex, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out value))
                                builder.Append((Char)value);

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

            Advance(token.Length);
            return true;
        }

        /// <summary>
        /// Attempts to match as much of the provided token as possible.
        /// </summary>
        /// <param name="token">The token to match</param>
        /// <returns>
        /// The number of matching characters. The Tokenizer is not advanced.
        /// </returns>
        public abstract int MatchSubstring(string token);

        /// <summary>
        /// Splits the provided <paramref name="input"/> string at <paramref name="separator"/> boundaries.
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

        /// <summary>
        /// Gets whether the provided word is a definite (the) or indefinite (a,an) article.
        /// </summary>
        public static bool IsArticle(Token word)
        {
            switch (word.Length)
            {
                case 1:
                    return (word[0] == 'a' || word[0] == 'A'); // a

                case 2:
                    switch (word[0])
                    {
                        case 'a':
                        case 'A':
                            return (word[1] == 'n' || word[1] == 'N'); // an
                    }
                    break;

                case 3:
                    switch (word[0])
                    {
                        case 't':
                        case 'T':
                            return (word[1] == 'h' || word[1] == 'H') && (word[2] == 'e' || word[2] == 'E'); // the
                    }
                    break;
            }

            return false;
        }

        /// <summary>
        /// Strips an article (a, an, the) from the start of a phrase token.
        /// </summary>
        public static Token RemoveArticle(Token phrase)
        {
            switch (phrase.Length)
            {
                default:
                    if (phrase[3] == ' ' && (phrase[2] == 'e' || phrase[2] == 'E') && (phrase[1] == 'h' || phrase[1] == 'H') && (phrase[0] == 't' || phrase[0] == 'T')) // the
                        return phrase.SubToken(4).TrimLeft();
                    goto case 3;

                case 3:
                    if (phrase[2] == ' ' && (phrase[1] == 'n' || phrase[1] == 'N') && (phrase[0] == 'a' || phrase[0] == 'A')) // an
                        return phrase.SubToken(3).TrimLeft();
                    goto case 2;

                case 2:
                    if (phrase[1] == ' ' && (phrase[0] == 'a' || phrase[0] == 'A')) // a
                        return phrase.SubToken(2).TrimLeft();
                    break;

                case 1:
                case 0:
                    break;
            }

            return phrase;
        }

        /// <summary>
        /// Gets whether the provided word is a common word that has minimal importance.
        /// </summary>
        /// <remarks>matches: a, an, in, it, on, or, of, to, and, the</remarks>
        public static bool IsIgnoredWord(Token word)
        {
            switch (word.Length)
            {
                case 1:
                    return (word[0] == 'a' || word[0] == 'A'); // a

                case 2:
                    switch (word[0])
                    {
                        case 'a':
                        case 'A':
                            return (word[1] == 'n' || word[1] == 'N'); // an

                        case 'i':
                        case 'I':
                            return (word[1] == 'n' || word[1] == 't' || word[1] == 'N' || word[1] == 'T'); // in / it

                        case 'o':
                        case 'O':
                            return (word[1] == 'n' || word[1] == 'r' || word[1] == 'f' || word[1] == 'N' || word[1] == 'R' || word[1] == 'F'); // on / or / of

                        case 't':
                        case 'T':
                            return (word[1] == 'o' || word[1] == 'O'); // to
                    }
                    break;
                          
                case 3:
                    switch (word[0])
                    {
                        case 'a':
                        case 'A':
                            return (word[1] == 'n' || word[1] == 'N') && (word[2] == 'd' || word[2] == 'D'); // and

                        case 't':
                        case 'T':
                            return (word[1] == 'h' || word[1] == 'H') && (word[2] == 'e' || word[2] == 'E'); // the
                    }
                    break;
            }

            return false;
        }

        private static bool IsWordSeparator(char c)
        {
            // Using a switch statement is the most efficient way to identify if a character is in a set of other 
            // characters. The compiler will most likely turn this into a truth table or jump table, which is much 
            // faster than iterating (or even bsearching) over a collection.
            switch (c)
            {
                case '\t':
                case '\n':
                case '\r':
                case ' ':
                case '!':
                case '#':
                case '(':
                case ')':
                case ',':
                case '.':
                case ':':
                case ';':
                case '?':
                case '[':
                case ']':
                    return true;

                default:
                    return false;
            }
        }

        private static Token[] GetWordTokens(string input)
        {
            if (String.IsNullOrEmpty(input))
                return new Token[0];

            var inputToken = new Token(input, 0, input.Length);
            return inputToken.Split(IsWordSeparator, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Gets the <paramref name="count"/> longest words from <paramref name="input"/>
        /// </summary>
        /// <remarks>Excludes words matched by <see cref="IsIgnoredWord"/></remarks>
        public static Token[] GetLongestWords(string input, int count)
        {
            if (count < 1)
                throw new ArgumentException("count must be 1 or greater", "count");

            if (count == 1)
            {
                var longest = GetLongestWord(input);
                if (longest.IsEmpty)
                    return new Token[0];

                return new Token[] { longest };
            }

            var sortedTokens = new Token[count];
            var tokens = new List<Token>(count);

            foreach (var word in GetWordTokens(input))
            {
                if (IsIgnoredWord(word))
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

        /// <summary>
        /// Gets the longest word from <paramref name="input"/>
        /// </summary>
        /// <remarks>Excludes words matched by <see cref="IsIgnoredWord"/></remarks>
        public static Token GetLongestWord(string input)
        {
            var longest = new Token();

            foreach (var word in GetWordTokens(input))
            {
                if (word.Length > longest.Length && !IsIgnoredWord(word))
                    longest = word;
            }

            return longest;
        }

        /// <summary>
        /// Gets all of the words from <paramref name="input"/>
        /// </summary>
        /// <remarks>Excludes words matched by <see cref="IsIgnoredWord"/></remarks>
        public static Token[] GetAllWords(string input)
        {
            var tokens = GetWordTokens(input);

            // if none of the tokens are ignored, we can just return the tokens array
            int i = tokens.Length - 1;
            while (i >= 0)
            {
                if (IsIgnoredWord(tokens[i]))
                {
                    // found an ignored word, create a copy of the tokens array without the ignored word(s)
                    var filteredTokens = new List<Token>(tokens.Length - 1);
                    for (int j = 0; j < i; j++)
                    {
                        if (!IsIgnoredWord(tokens[j]))
                            filteredTokens.Add(tokens[j]);
                    }

                    // we already know everything after [i] is not an ignored word
                    for (int j = i + 1; j < tokens.Length; j++)
                        filteredTokens.Add(tokens[j]);

                    return filteredTokens.ToArray();
                }

                i--;
            }

            return tokens;
        }

        /// <summary>
        /// Populates <paramref name="matches"/> from placeholders in <paramref name="pattern"/> if <paramref name="pattern"/> matches <paramref name="token"/>.
        /// </summary>
        /// <returns><c>true</c> if the pattern was matched. <c>false</c> if not.</returns>
        /// <example>
        /// var input = "Carl has 3 apples.";
        /// var tokens = new Token[3];
        /// if (Tokenizer.Parse(new Token(input, 0, input.Length), "{0} has {1} {2}.", tokens) {
        ///     var name = tokens[0].ToString();               // tokens[0] = "Carl"
        ///     var count = Int32.Parse(tokens[1].ToString()); // tokens[1] = "3"
        ///     var item = tokens[2].ToString();               // tokens[2] = "apples"
        ///     AddInventory(name, item, count);
        /// }
        /// </example>
        public static bool Parse(Token token, string pattern, Token[] matches)
        {
            if (pattern.Length < 3)
                return (token == pattern);

            if (pattern[0] != '{' && pattern[0] != token[0])
                return false;

            if (pattern[pattern.Length - 1] != '}' && pattern[pattern.Length - 1] != token[token.Length - 1])
                return false;

            var patternFrontIndex = pattern.IndexOf('{');
            if (patternFrontIndex == -1)
                return (token == pattern);

            if (patternFrontIndex > 0)
            {
                var patternFront = new Token(pattern, 0, patternFrontIndex);
                if (!token.StartsWith(patternFront))
                    return false;
            }

            var patternBackIndex = pattern.LastIndexOf('}');
            var patternBack = new Token(pattern, patternBackIndex + 1, pattern.Length - patternBackIndex - 1);
            if (!token.EndsWith(patternBack))
                return false;

            var tokenFrontIndex = patternFrontIndex;
            var tokenBackIndex = token.Length - patternBack.Length;
            do
            {
                patternFrontIndex++;
                var indexEnd = pattern.IndexOf('}', patternFrontIndex);
                var indexToken = new Token(pattern, patternFrontIndex, indexEnd - patternFrontIndex);

                if (indexEnd == patternBackIndex)
                {
                    matches[Int32.Parse(indexToken.ToString())] = token.SubToken(tokenFrontIndex, tokenBackIndex - tokenFrontIndex);
                    return true;
                }

                indexEnd++;
                patternFrontIndex = pattern.IndexOf('{', indexEnd);
                if (patternFrontIndex == -1)
                    return false;

                var matchIndex = token.IndexOf(pattern.Substring(indexEnd, patternFrontIndex - indexEnd), tokenFrontIndex);
                if (matchIndex == -1)
                    return false;

                matches[Int32.Parse(indexToken.ToString())] = token.SubToken(tokenFrontIndex, matchIndex - tokenFrontIndex);
                tokenFrontIndex = matchIndex + (patternFrontIndex - indexEnd);
            } while (true);
        }
    }
}
