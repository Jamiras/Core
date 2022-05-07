using System;

namespace Jamiras.Components
{
    /// <summary>
    /// Represents a position within a document.
    /// </summary>
    public struct TextLocation
    {
        /// <summary>
        /// Constructs a new TextLocation
        /// </summary>
        public TextLocation(int line, int column)
        {
            Line = line;
            Column = column;
        }

        /// <summary>
        /// Gets or sets the line of the text location.
        /// </summary>
        public int Line;

        /// <summary>
        /// Gets or sets the column of the text location.
        /// </summary>
        public int Column;

        /// <summary>
        /// Gets a string representation of the text location.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format("{0},{1}", Line, Column);
        }

        /// <summary>
        /// Compares two <see cref="TextLocation"/>s.
        /// </summary>
        private int Compare(TextLocation that)
        {
            int diff = this.Line - that.Line;
            if (diff == 0)
                diff = this.Column - that.Column;

            return diff;
        }

        /// <summary>
        /// Compares two <see cref="TextLocation"/>s.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (!(obj is TextLocation))
                return false;

            return Compare((TextLocation)obj) == 0;
        }

        /// <summary>
        /// Calculates a unique identifier for the <see cref="TextLocation"/>.
        /// </summary>
        public override int GetHashCode()
        {
            return Column * 1690691 + Line;
        }

        /// <summary>
        /// Compares two <see cref="TextLocation"/>s.
        /// </summary>
        public static bool operator ==(TextLocation left, TextLocation right)
        {
            return left.Compare(right) == 0;
        }

        /// <summary>
        /// Compares two <see cref="TextLocation"/>s.
        /// </summary>
        public static bool operator !=(TextLocation left, TextLocation right)
        {
            return left.Compare(right) != 0;
        }

        /// <summary>
        /// Compares two <see cref="TextLocation"/>s.
        /// </summary>
        public static bool operator <(TextLocation left, TextLocation right)
        {
            return left.Compare(right) < 0;
        }

        /// <summary>
        /// Compares two <see cref="TextLocation"/>s.
        /// </summary>
        public static bool operator >(TextLocation left, TextLocation right)
        {
            return left.Compare(right) > 0;
        }

        /// <summary>
        /// Compares two <see cref="TextLocation"/>s.
        /// </summary>
        public static bool operator <=(TextLocation left, TextLocation right)
        {
            return left.Compare(right) <= 0;
        }

        /// <summary>
        /// Compares two <see cref="TextLocation"/>s.
        /// </summary>
        public static bool operator >=(TextLocation left, TextLocation right)
        {
            return left.Compare(right) >= 0;
        }
    }
}
