namespace Jamiras.Components
{
    /// <summary>
    /// Wraps a <see cref="Tokenizer"/> to keep track of line and column numbers
    /// </summary>
    public class PositionalTokenizer : Tokenizer
    {
        public PositionalTokenizer(Tokenizer tokenizer) : this(tokenizer, 1, 1)
        {
        }

        public PositionalTokenizer(Tokenizer tokenizer, int line, int column)
        {
            baseTokenizer = tokenizer;
            NextChar = tokenizer.NextChar;

            Line = line;
            Column = column;
        }

        private readonly Tokenizer baseTokenizer;

        public int Line { get; private set; }
        public int Column { get; private set; }

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

        public override Token ReadQuotedString()
        {
            Token quotedString = baseTokenizer.ReadQuotedString();
            NextChar = baseTokenizer.NextChar;
            return quotedString;
        }

        public override int MatchSubstring(string token)
        {
            return baseTokenizer.MatchSubstring(token);
        }
    }
}
