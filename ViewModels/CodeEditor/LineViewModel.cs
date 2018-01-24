using Jamiras.DataModels;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System;
using System.Linq;

namespace Jamiras.ViewModels.CodeEditor
{
    [DebuggerDisplay("{Text}")]
    public class LineViewModel : ViewModelBase
    {
        public LineViewModel(CodeEditorViewModel owner, int line)
        {
            _owner = owner;
            SetValueCore(LineProperty, line);
        }

        private readonly CodeEditorViewModel _owner;

        public static readonly ModelProperty LineProperty = ModelProperty.Register(typeof(LineViewModel), "Line", typeof(int), 1);
        public int Line
        {
            get { return (int)GetValue(LineProperty); }
            internal set { SetValue(LineProperty, value); }
        }

        private static readonly ModelProperty SelectionStartProperty = ModelProperty.Register(typeof(LineViewModel), "SelectionStart", typeof(int), 0);
        public int SelectionStart
        {
            get { return (int)GetValue(SelectionStartProperty); }
            private set { SetValue(SelectionStartProperty, value); }
        }

        private static readonly ModelProperty SelectionEndProperty = ModelProperty.Register(typeof(LineViewModel), "SelectionEnd", typeof(int), 0);
        public int SelectionEnd
        {
            get { return (int)GetValue(SelectionEndProperty); }
            internal set { SetValue(SelectionEndProperty, value); }
        }

        private static readonly ModelProperty SelectionLocationProperty =
            ModelProperty.RegisterDependant(typeof(LineViewModel), "SelectionLocation", typeof(Thickness), new[] { SelectionStartProperty }, GetSelectionLocation);
        public Thickness SelectionLocation
        {
            get { return (Thickness)GetValue(SelectionLocationProperty); }
        }
        private static object GetSelectionLocation(ModelBase model)
        {
            var viewModel = (LineViewModel)model;
            var offset = (viewModel.SelectionStart - 1) * viewModel._owner.CharacterWidth;
            return new Thickness((int)offset, 0, 0, 0);
        }

        private static readonly ModelProperty SelectionWidthProperty =
            ModelProperty.RegisterDependant(typeof(LineViewModel), "SelectionWidth", typeof(double), new[] { SelectionStartProperty, SelectionEndProperty }, GetSelectionWidth);
        public double SelectionWidth
        {
            get { return (double)GetValue(SelectionWidthProperty); }
        }
        private static object GetSelectionWidth(ModelBase model)
        {
            var viewModel = (LineViewModel)model;
            return (viewModel.SelectionEnd - viewModel.SelectionStart) * viewModel._owner.CharacterWidth;
        }

        private static readonly ModelProperty CursorColumnProperty = ModelProperty.Register(typeof(LineViewModel), "CursorColumn", typeof(int), 0);
        public int CursorColumn
        {
            get { return (int)GetValue(CursorColumnProperty); }
            internal set { SetValue(CursorColumnProperty, value); }
        }

        private static readonly ModelProperty CursorLocationProperty = 
            ModelProperty.RegisterDependant(typeof(LineViewModel), "CursorLocation", typeof(Thickness), new[] { CursorColumnProperty }, GetCursorLocation);
        public Thickness CursorLocation
        {
            get { return (Thickness)GetValue(CursorLocationProperty); }
        }
        private static object GetCursorLocation(ModelBase model)
        {
            var viewModel = (LineViewModel)model;
            var offset = (viewModel.CursorColumn - 1) * viewModel._owner.CharacterWidth;
            return new Thickness((int)offset, 0, 0, 0);
        }

        public EditorResources Resources
        {
            get { return _owner.Resources; }
        }

        public int LineLength
        {
            get
            {
                var text = PendingText ?? Text;
                return text.Length;
            }
        }

        internal static readonly ModelProperty PendingTextProperty = ModelProperty.Register(typeof(LineViewModel), "PendingText", typeof(string), null);
        internal string PendingText
        {
            get { return (string)GetValue(PendingTextProperty); }
            set { SetValue(PendingTextProperty, value); }
        }

        public static readonly ModelProperty TextProperty = ModelProperty.Register(typeof(LineViewModel), "Text", typeof(string), string.Empty);
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly ModelProperty TextPiecesProperty = 
            ModelProperty.RegisterDependant(typeof(LineViewModel), "TextPieces", typeof(IEnumerable<TextPiece>), new[] { TextProperty }, GetTextPieces);
        public IEnumerable<TextPiece> TextPieces
        {
            get { return (IEnumerable<TextPiece>)GetValue(TextPiecesProperty); }
        }

        private static object GetTextPieces(ModelBase model)
        {
            var viewModel = (LineViewModel)model;

            var e = new LineChangedEventArgs(viewModel);
            viewModel._owner.RaiseLineChanged(e);

            return e.BuildTextPieces();
        }

        public TextPieceLocation GetTextPiece(int column)
        {
            if (column >= 1)
            {
                column--;
                foreach (var textPiece in TextPieces)
                {
                    if (textPiece.Text.Length > column)
                        return new TextPieceLocation { Piece = textPiece, Offset = column };

                    column -= textPiece.Text.Length;
                }
            }

            return new TextPieceLocation();
        }

        public void Insert(int column, string str)
        {
            // cursor between characters 1 and 2 is inserting at column 2, but since the string is indexed via 0-based indexing, adjust the insert location
            column--;
            Debug.Assert(column >= 0);

            var text = PendingText;
            if (text == null)
                text = Text;

            Debug.Assert(column <= text.Length);
            text = text.Insert(column, str);
            PendingText = text;

            var newPieces = new List<TextPiece>(TextPieces);

            var index = column;
            var enumerator = newPieces.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var piece = enumerator.Current;
                if (piece.Text.Length < index)
                {
                    index -= piece.Text.Length;
                    continue;
                }

                if (piece.Text.Length == index && ReferenceEquals(piece.Foreground, Resources.Foreground)) // boundary between pieces, prefer non-default
                {
                    if (enumerator.MoveNext())
                    {
                        piece = enumerator.Current;
                        index = 0;
                    }
                }

                piece.Text = piece.Text.Insert(index, str);
                break;
            }

            SetValue(TextPiecesProperty, newPieces.ToArray());

            _owner.ScheduleRefresh();
        }

        internal void Remove(int startColumn, int endColumn)
        {
            Debug.Assert(endColumn >= startColumn);

            // deleting columns 1 through 1 (first character) is really Text[0] because it's 0-based
            startColumn--;
            Debug.Assert(startColumn >= 0);
            endColumn--;

            var text = PendingText;
            if (text == null)
                text = Text;

            Debug.Assert(endColumn <= text.Length);
            int removeCount = endColumn - startColumn + 1;
            text = text.Remove(startColumn, removeCount);
            PendingText = text;

            var newPieces = new List<TextPiece>(TextPieces);

            var index = startColumn;
            var pieceIndex = 0;
            while (pieceIndex < newPieces.Count)
            {
                var piece = newPieces[pieceIndex++];
                if (piece.Text.Length < index)
                {
                    index -= piece.Text.Length;
                    continue;
                }

                if (piece.Text.Length == index && !ReferenceEquals(piece.Foreground, Resources.Foreground)) // boundary between pieces - prefer default
                {
                    if (pieceIndex < newPieces.Count)
                    {
                        piece = newPieces[pieceIndex++];
                        index = 0;
                    }
                }

                if (removeCount >= piece.Text.Length)
                {
                    if (index > 0)
                    {
                        removeCount -= (piece.Text.Length - index);
                        piece.Text = piece.Text.Substring(0, index);

                        Debug.Assert(pieceIndex < newPieces.Count);
                        piece = newPieces[pieceIndex++];
                        index = 0;
                    }

                    while (removeCount >= piece.Text.Length)
                    {
                        removeCount -= piece.Text.Length;

                        newPieces.RemoveAt(pieceIndex - 1);
                        if (removeCount == 0)
                            break;

                        Debug.Assert(pieceIndex <= newPieces.Count);
                        piece = newPieces[pieceIndex - 1];
                    }
                }

                if (removeCount > 0)
                    piece.Text = piece.Text.Remove(index, removeCount);

                break;
            }

            SetValue(TextPiecesProperty, newPieces.ToArray());

            _owner.ScheduleRefresh();
        }

        internal void ClearSelection()
        {
            SelectionEnd = 0;
            SelectionStart = 0;
        }

        internal void Select(int startColumn, int endColumn)
        {
            SelectionStart = startColumn;
            SelectionEnd = endColumn;
        }
    }
}
