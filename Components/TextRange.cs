using System;

namespace Jamiras.Components
{
    /// <summary>
    /// Represents a contiguous region of a document.
    /// </summary>
    public struct TextRange
    {
        /// <summary>
        /// Constructs a new TextRange
        /// </summary>
        public TextRange(TextLocation start, TextLocation end)
        {
            Start = start;
            End = end;
        }

        /// <summary>
        /// Constructs a new TextRange
        /// </summary>
        public TextRange(int startLine, int startColumn, int endLine, int endColumn)
            : this(new TextLocation(startLine, startColumn), new TextLocation(endLine, endColumn))
        {
        }

        /// <summary>
        /// Gets or sets the <see cref="TextLocation"/> of the first character of the range.
        /// </summary>
        public TextLocation Start;

        /// <summary>
        /// Gets or sets the <see cref="TextLocation"/> after the last character of the range.
        /// </summary>
        public TextLocation End;
        
        /// <summary>
        /// Gets the front of the selection.
        /// </summary>
        public TextLocation Front
        {
            get
            {
                return (Start <= End) ? Start : End;
            }
        }

        /// <summary>
        /// Gets the back of the selection.
        /// </summary>
        public TextLocation Back
        {
            get
            {
                return (Start >= End) ? Start : End;
            }
        }

        /// <summary>
        /// Gets a string representation of the text location.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format("{0}-{1}", Start, End);
        }

        /// <summary>
        /// Determines if a <see cref="TextLocation"/> is within the selection.
        /// </summary>
        public bool Contains(TextLocation location)
        {
            if (Start <= End)
                return (location >= Start && location <= End);

            return (location >= End && location <= Start);
        }

        /// <summary>
        /// Determines if two <see cref="TextRange"/>s are equivalent (ignoring direction).
        /// </summary>
        public bool RangeEquals(TextRange that)
        {
            if (this.Start == that.Start)
                return (this.End == that.End);

            if (this.Start == that.End)
                return (this.End == that.Start);

            return false;
        }

        /// <summary>
        /// Determines if the <see cref="Start"/> and <see cref="End"/> of the range as the same <see cref="TextLocation"/>.
        /// </summary>
        public bool IsEmpty
        {
            get { return Start == End; }
        }

        /// <summary>
        /// Makes sure the <see cref="Start"/> location is before the <see cref="End"/> location.
        /// </summary>
        public void EnsureForward()
        {
            if (End < Start)
            {
                var temp = End;
                End = Start;
                Start = temp;
            }
        }
    }
}
