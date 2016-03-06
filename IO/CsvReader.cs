using System;
using System.Collections.Generic;
using System.IO;
using Jamiras.Components;

namespace Jamiras.IO
{
    public class CsvReader
    {
        public CsvReader(Stream stream)
        {
            _tokenizer = Tokenizer.CreateTokenizer(stream);
        }

        private readonly Tokenizer _tokenizer;

        /// <summary>
        /// Gets the next line from the CSV file
        /// </summary>
        /// <returns><see cref="Token"/>s for the line, or <c>null</c> if no more lines found.</returns>
        public Token[] ReadLine()
        {
            if (_tokenizer.NextChar == '\0')
                return null;

            var line = new List<Token>();

            do
            {
                if (_tokenizer.NextChar == '\"')
                {
                    line.Add(_tokenizer.ReadQuotedString());
                }
                else
                {
                    line.Add(_tokenizer.ReadTo(',', '\n').Trim());
                }

                if (_tokenizer.NextChar != ',')
                    break;

                _tokenizer.Advance();
            } while (true);

            if (_tokenizer.NextChar != '\0')
            {
                if (_tokenizer.NextChar != '\r' && _tokenizer.NextChar != '\n')
                    throw new InvalidOperationException(String.Format("Expected comma or newline after entry '{0}', found '{1}'", line[line.Count - 1], _tokenizer.NextChar));

                while (_tokenizer.NextChar == '\r' || _tokenizer.NextChar == '\n')
                    _tokenizer.Advance();
            }

            return line.ToArray();
        }
    }
}
