using Jamiras.Controls;
using System.Diagnostics;
using System.Windows.Media;

namespace Jamiras.ViewModels.CodeEditor
{
    /// <summary>
    /// Represents a small portion of text for rendering purposes.
    /// </summary>
    /// <seealso cref="CodeEditorTextBlock"/>.
    [DebuggerDisplay("{Text}")]
    public class TextPiece
    {
        /// <summary>
        /// Gets or sets the text to render.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the foreground brush for the text.
        /// </summary>
        public Brush Foreground { get; set; }

        /// <summary>
        /// Gets or sets the tool tip to display when hovering over the text.
        /// </summary>
        public string ToolTip { get; set; }

        /// <summary>
        /// Gets or sets whether an error indicator should be drawn for the text.
        /// </summary>
        public bool IsError { get; set; }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var that = obj as TextPiece;
            if (that == null)
                return false;

            return this.IsError == that.IsError && this.Text == that.Text &&
                this.ToolTip == that.ToolTip && this.Foreground == that.Foreground;
        }

        public static bool operator ==(TextPiece left, TextPiece right)
        {
            if (ReferenceEquals(left, null))
                return ReferenceEquals(right, null);

            return left.Equals(right);
        }

        public static bool operator !=(TextPiece left, TextPiece right)
        {
            if (ReferenceEquals(left, null))
                return !ReferenceEquals(right, null);

            return !left.Equals(right);
        }
    }

    /// <summary>
    /// Complex return value for querying the <see cref="TextPiece"/> for a cursor position.
    /// </summary>
    [DebuggerDisplay("{Piece.Text}")]
    public struct TextPieceLocation
    {
        /// <summary>
        /// Gets the <see cref="TextPiece"/> at the queried location. 
        /// </summary>
        public TextPiece Piece { get; set; }

        /// <summary>
        /// Gets the number of characters into the <see cref="TextPiece"/> where the queried location is.
        /// </summary>
        public int Offset { get; set; }
    }
}
