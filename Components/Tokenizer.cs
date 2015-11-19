using System;
using System.IO;
using System.Text;

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
            Advance();
        }

        private readonly Stream _stream;

        /// <summary>
        /// Gets the next character in the stream.
        /// </summary>
        public Char NextChar { get; private set; }

        /// <summary>
        /// Advances to the next character in the stream.
        /// </summary>
        public void Advance()
        {
            var b = _stream.ReadByte();
            if (b < 0)
            {
                NextChar = (char)0x00;
            }
            else if (b < 0x80)
            {
                NextChar = (char)b;
            }
            else if (b < 0xC0)
            {
                NextChar = (char)0xFFFD;
            }
            else if (b < 0xE0)
            {
                var b2 = _stream.ReadByte();
                NextChar = (char)((b << 5) | (b2 & 0x1F));
            }
            else if (b < 0xF0)
            {
                var b2 = _stream.ReadByte();
                var b3 = _stream.ReadByte();
                NextChar = (char)((b << 10) | ((b2 & 0x1F) << 5) | (b3 & 0x1F));
            }
            else
            {
                NextChar = (char)0xFFFD;
            }
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
    }
}
