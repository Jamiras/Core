namespace Jamiras.Components
{
    /// <summary>
    /// Wraps a <see cref="Tokenizer"/> to keep track of line and column numbers
    /// </summary>
    public class PositionalTokenizer : Tokenizer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PositionalTokenizer"/> class with <see cref="Line"/> and <see cref="Column"/> both set to 1.
        /// </summary>
        /// <param name="tokenizer">The <see cref="Tokenizer"/> to extend with line and column tracking.</param>
        public PositionalTokenizer(Tokenizer tokenizer) : this(tokenizer, 1, 1)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PositionalTokenizer"/> class.
        /// </summary>
        /// <param name="tokenizer">The <see cref="Tokenizer"/> to extend with line and column tracking.</param>
        /// <param name="line">The initial value of <see cref="Line"/></param>
        /// <param name="column">The initial value of <see cref="Column"/></param>
        public PositionalTokenizer(Tokenizer tokenizer, int line, int column)
        {
            baseTokenizer = tokenizer;
            NextChar = tokenizer.NextChar;

            Line = line;
            Column = column;
        }

        private readonly Tokenizer baseTokenizer;

        /// <summary>
        /// Gets the line number of the <see cref="Tokenizer.NextChar"/>.
        /// </summary>
        public int Line { get; private set; }

        /// <summary>
        /// Gets the column number of the <see cref="Tokenizer.NextChar"/>.
        /// </summary>
        public int Column { get; private set; }

        /// <summary>
        /// Advances to the next character in the source.
        /// </summary>
        public override void Advance()
        {
            if (baseTokenizer.NextChar == '\n')
            {
                Line++;
                Column = 1;
            }
            else
            {
                Column++;
            }

            baseTokenizer.Advance();

            NextChar = baseTokenizer.NextChar;
        }

        internal override void StartToken()
        {
            baseTokenizer.StartToken();
        }

        internal override Token EndToken()
        {
            return baseTokenizer.EndToken();
        }

        /// <summary>
        /// Matches a quoted string.
        /// </summary>
        public override Token ReadQuotedString()
        {
            Token quotedString = baseTokenizer.ReadQuotedString();
            NextChar = baseTokenizer.NextChar;
            return quotedString;
        }

        /// <summary>
        /// Attempts to match as much of the provided token as possible.
        /// </summary>
        /// <param name="token">The token to match</param>
        /// <returns>
        /// The number of matching characters. The Tokenizer is not advanced.
        /// </returns>
        public override int MatchSubstring(string token)
        {
            return baseTokenizer.MatchSubstring(token);
        }
    }
}
