using System;

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
        /// Gets the <see cref="TextLocation"/> of the <see cref="Tokenizer.NextChar"/>.
        /// </summary>
        public TextLocation Location
        {
            get { return new TextLocation(Line, Column); }
        }

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
            else if (baseTokenizer.NextChar != '\0')
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

        private class PositionalTokenizerState
        {
            public object BaseTokenizerState;
            public int Line;
            public int Column;
        }

        /// <summary>
        /// Captures the current state of the tokenizer.
        /// </summary>
        /// <returns>An object that can be passed to <see cref="RestoreState"/> to return the tokenizer to the current state.</returns>
        protected override object CreateState()
        {
            return new PositionalTokenizerState
            {
                BaseTokenizerState = baseTokenizer.CreateStateInternal(),
                Line = Line,
                Column = Column
            };
        }

        /// <summary>
        /// Restores the state of the tokenizer to some previous state.
        /// </summary>
        /// <returns>An object that was captured by <see cref="CreateState"/> representing the state of the tokenizer when it was captured.</returns>
        protected override void RestoreState(object state)
        {
            var positionalTokenizerState = state as PositionalTokenizerState;
            if (positionalTokenizerState != null)
            {
                baseTokenizer.RestoreStateInternal(positionalTokenizerState.BaseTokenizerState);
                NextChar = baseTokenizer.NextChar;
                Line = positionalTokenizerState.Line;
                Column = positionalTokenizerState.Column;
            }
        }
    }
}
