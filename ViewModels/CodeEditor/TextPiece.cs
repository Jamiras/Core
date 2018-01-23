using System.Diagnostics;
using System.Windows.Media;

namespace Jamiras.ViewModels.CodeEditor
{
    [DebuggerDisplay("{Text}")]
    public class TextPiece
    {
        public string Text { get; set; }
        public Brush Foreground { get; set; }
        public string ToolTip { get; set; }
        public bool IsError { get; set; }
    }

    [DebuggerDisplay("{Piece.Text}")]
    public struct TextPieceLocation
    {
        public TextPiece Piece { get; set; }
        public int Offset { get; set; }
    }
}
