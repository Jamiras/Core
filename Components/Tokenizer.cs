using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace Jamiras.Components
{
    public class Tokenizer
    {
        public Tokenizer(string input)
            : this(new MemoryStream(Encoding.UTF8.GetBytes(input)))
        {
        }

        public Tokenizer(Stream input)
        {
            _stream = input;
            _bufferedChars = new List<char>();

            Advance();
        }

        private readonly Stream _stream;
        private readonly List<char> _bufferedChars;

        /// <summary>
        /// Gets the next character in the stream.
        /// </summary>
        public Char NextChar { get; private set; }

        /// <summary>
        /// Advances to the next character in the stream.
        /// </summary>
        public void Advance()
        {
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
        /// Advances to the next non-whitespace character in the stream.
        /// </summary>
        public void SkipWhitespace()
        {
            while (Char.IsWhiteSpace(NextChar))
                Advance();
        }

        /// <summary>
        /// Matches a token containing alphanumeric characters and/or underscores, or a quoted string.
        /// </summary>
        public string ReadValue()
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
        public string ReadIdentifier()
        {
            var builder = new StringBuilder();
            if (!Char.IsDigit(NextChar))
            {
                while (Char.IsLetterOrDigit(NextChar) || NextChar == '_')
                {
                    builder.Append(NextChar);
                    Advance();
                }
            }

            return (builder.Length == 0) ? String.Empty : builder.ToString();
        }

        /// <summary>
        /// Matches a token containing numeric characters, possibly with a single decimal separator.
        /// </summary>
        public string ReadNumber()
        {
            if (!Char.IsDigit(NextChar))
                return String.Empty;

            var builder = new StringBuilder();
            while (Char.IsDigit(NextChar))
            {
                builder.Append(NextChar);
                Advance();
            }

            if (NextChar == '.')
            {
                builder.Append('.');
                Advance();

                while (Char.IsDigit(NextChar))
                {
                    builder.Append(NextChar);
                    Advance();
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Matches a quoted string.
        /// </summary>
        public String ReadQuotedString()
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
            return (builder.Length == 0) ? String.Empty : builder.ToString();
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
        public int MatchSubstring(string token)
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
}
