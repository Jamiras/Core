using Jamiras.Components;

namespace Jamiras.IO.Serialization
{
    /// <summary>
    /// Parser for processing XML documents.
    /// </summary>
    public class XmlParser
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="XmlParser"/> class.
        /// </summary>
        public XmlParser(string xml)
        {
            _tokenizer = Tokenizer.CreateTokenizer(xml);
            Advance();
        }

        private Tokenizer _tokenizer;

        /// <summary>
        /// Gets the type of the next token.
        /// </summary>
        public XmlTokenType NextTokenType { get; private set; }

        /// <summary>
        /// Gets the next token.
        /// </summary>
        public Token NextToken { get; private set; }

        /// <summary>
        /// Advances over the next token.
        /// </summary>
        public XmlTokenType Advance()
        {
            // tag
            if (_tokenizer.NextChar == '<')
            {
                _tokenizer.StartToken();
                _tokenizer.Advance();

                bool isClosing = (_tokenizer.NextChar == '/');

                while (_tokenizer.NextChar != '>' && _tokenizer.NextChar != '\0')
                {
                    if (_tokenizer.NextChar == '"')
                    {
                        _tokenizer.Advance();
                        while (_tokenizer.NextChar != '"' && _tokenizer.NextChar != '\0')
                        {
                            if (_tokenizer.NextChar == '\\')
                                _tokenizer.Advance();
                            _tokenizer.Advance();
                        }
                    }

                    _tokenizer.Advance();
                }

                _tokenizer.Advance();
                NextToken = _tokenizer.EndToken();
                NextTokenType = isClosing ? XmlTokenType.CloseTag : XmlTokenType.OpenTag;
                return NextTokenType;
            }

            // end of input
            if (_tokenizer.NextChar == '\0')
            {
                if (NextTokenType != XmlTokenType.None)
                {
                    NextToken = new Token();
                    NextTokenType = XmlTokenType.None;
                }

                return XmlTokenType.None;
            }

            // content
            _tokenizer.StartToken();
            _tokenizer.SkipWhitespace();
            if (_tokenizer.NextChar == '<')
            {
                // whitespace-only content. ignore
                _tokenizer.EndToken();
                return Advance();
            }

            do
            {
                _tokenizer.Advance();
            } while (_tokenizer.NextChar != '<' && _tokenizer.NextChar != '\0');

            NextToken = _tokenizer.EndToken();
            NextTokenType = XmlTokenType.Content;
            return XmlTokenType.Content;
        }
    }

    /// <summary>
    /// The type of the XML token.
    /// </summary>
    public enum XmlTokenType
    {
        /// <summary>
        /// Unspecified.
        /// </summary>
        None = 0,

        /// <summary>
        /// An opening tag (i.e. "&lt;a&gt;").
        /// </summary>
        OpenTag,

        /// <summary>
        /// A closing tag (i.e. "&lt;/a&gt;").
        /// </summary>
        CloseTag,

        /// <summary>
        /// Content between tags.
        /// </summary>
        Content,
    }
}
