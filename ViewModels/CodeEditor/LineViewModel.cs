using Jamiras.DataModels;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows;

namespace Jamiras.ViewModels.CodeEditor
{
    public class LineViewModel : ViewModelBase
    {
        public LineViewModel(CodeEditorViewModel owner, int line)
        {
            _owner = owner;
            SetValueCore(LineProperty, line);
        }

        private readonly CodeEditorViewModel _owner;

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="Line"/>
        /// </summary>
        public static readonly ModelProperty LineProperty = ModelProperty.Register(typeof(LineViewModel), "Line", typeof(int), 1);

        /// <summary>
        /// Gets the line number for this line.
        /// </summary>
        public int Line
        {
            get { return (int)GetValue(LineProperty); }
            internal set { SetValue(LineProperty, value); }
        }

        private static readonly ModelProperty SelectionStartProperty = ModelProperty.Register(typeof(LineViewModel), "SelectionStart", typeof(int), 0);

        /// <summary>
        /// Gets the first column of the selected text (0 if no selection).
        /// </summary>
        /// <remarks>
        /// If "es" is selected in "Test", SelectionStart will be 2
        /// </remarks>
        public int SelectionStart
        {
            get { return (int)GetValue(SelectionStartProperty); }
            private set { SetValue(SelectionStartProperty, value); }
        }

        private static readonly ModelProperty SelectionEndProperty = ModelProperty.Register(typeof(LineViewModel), "SelectionEnd", typeof(int), 0);

        /// <summary>
        /// Gets the last column of the selected text (0 if no selection).
        /// </summary>
        /// <remarks>
        /// If "es" is selected in "Test", SelectionEnd will be 3
        /// </remarks>
        public int SelectionEnd
        {
            get { return (int)GetValue(SelectionEndProperty); }
            internal set { SetValue(SelectionEndProperty, value); }
        }

        private static readonly ModelProperty SelectionLocationProperty =
            ModelProperty.RegisterDependant(typeof(LineViewModel), "SelectionLocation", typeof(Thickness), new[] { SelectionStartProperty }, GetSelectionLocation);

        /// <summary>
        /// Gets the left render edge of the selection rectangle (for UI binding only).
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Thickness SelectionLocation
        {
            get { return (Thickness)GetValue(SelectionLocationProperty); }
        }
        private static object GetSelectionLocation(ModelBase model)
        {
            var viewModel = (LineViewModel)model;
            var selectionStart = viewModel.SelectionStart;
            if (selectionStart == 0)
                return new Thickness();

            var offset = (selectionStart - 1) * viewModel.Resources.CharacterWidth;
            return new Thickness(offset, 0.0, 0.0, 0.0);
        }

        private static readonly ModelProperty SelectionWidthProperty =
            ModelProperty.RegisterDependant(typeof(LineViewModel), "SelectionWidth", typeof(double), new[] { SelectionStartProperty, SelectionEndProperty }, GetSelectionWidth);

        /// <summary>
        /// Gets the render width of the selection rectangle (for UI binding only).
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public double SelectionWidth
        {
            get { return (double)GetValue(SelectionWidthProperty); }
        }
        private static object GetSelectionWidth(ModelBase model)
        {
            var viewModel = (LineViewModel)model;
            var selectionStart = viewModel.SelectionStart;
            if (selectionStart == 0)
                return 0.0;

            return (viewModel.SelectionEnd - selectionStart + 1) * viewModel.Resources.CharacterWidth;
        }

        private static readonly ModelProperty CursorColumnProperty = ModelProperty.Register(typeof(LineViewModel), "CursorColumn", typeof(int), 0);

        /// <summary>
        /// Gets the column where the cursor is currently located (0 if the cursor is not on this line).
        /// </summary>
        public int CursorColumn
        {
            get { return (int)GetValue(CursorColumnProperty); }
            internal set { SetValue(CursorColumnProperty, value); }
        }

        private static readonly ModelProperty CursorLocationProperty = 
            ModelProperty.RegisterDependant(typeof(LineViewModel), "CursorLocation", typeof(Thickness), new[] { CursorColumnProperty }, GetCursorLocation);

        /// <summary>
        /// Gets the left render edge of the cursor (for UI binding only).
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Thickness CursorLocation
        {
            get { return (Thickness)GetValue(CursorLocationProperty); }
        }
        private static object GetCursorLocation(ModelBase model)
        {
            var viewModel = (LineViewModel)model;
            var offset = (viewModel.CursorColumn - 1) * viewModel.Resources.CharacterWidth;
            return new Thickness((int)offset, 0, 0, 0);
        }

        /// <summary>
        /// Gets the <see cref="EditorResources"/> for this line (for UI binding only).
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public EditorResources Resources
        {
            get { return _owner.Resources; }
        }

        /// <summary>
        /// Gets the number of characters in the line
        /// </summary>
        internal int LineLength
        {
            get
            {
                var text = PendingText ?? Text;
                return text.Length;
            }
        }

        /// <summary>
        /// Commits the <see cref="PendingText"/>.
        /// </summary>
        public void CommitPending()
        {
            var pendingText = PendingText;
            if (pendingText != null)
            {
                PendingText = null;
                if (Text == pendingText)
                {
                    // force update of TextPieces, even if Text didn't really change
                    Refresh();
                }
                else
                {
                    Text = pendingText;
                }
            }
        }

        /// <summary>
        /// Reconstructs the syntax highlighting for the line.
        /// </summary>
        public override void Refresh()
        {
            var text = Text;

            // framework trick to force update of dependent property TextPieces, even if Text didn't really change
            OnModelPropertyChanged(new ModelPropertyChangedEventArgs(TextProperty, text, text));
        }

        internal static readonly ModelProperty PendingTextProperty = ModelProperty.Register(typeof(LineViewModel), "PendingText", typeof(string), null);

        /// <summary>
        /// Gets or sets text being typed.
        /// </summary>
        internal string PendingText
        {
            get { return (string)GetValue(PendingTextProperty); }
            set { SetValue(PendingTextProperty, value); }
        }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="Text"/>
        /// </summary>
        public static readonly ModelProperty TextProperty = ModelProperty.Register(typeof(LineViewModel), "Text", typeof(string), string.Empty);

        /// <summary>
        /// Gets the text in the line.
        /// </summary>
        /// <remarks>
        /// May not be updated if the user is in the middle of typing.
        /// </remarks>
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            internal set { SetValue(TextProperty, value); }
        }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="TextPieces"/>
        /// </summary>
        public static readonly ModelProperty TextPiecesProperty = 
            ModelProperty.RegisterDependant(typeof(LineViewModel), "TextPieces", typeof(IEnumerable<TextPiece>), new[] { TextProperty }, GetTextPieces);

        /// <summary>
        /// Gets the <see cref="TextPiece"/>s for this line (for UI binding only).
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public IEnumerable<TextPiece> TextPieces
        {
            get { return (IEnumerable<TextPiece>)GetValue(TextPiecesProperty); }
        }

        private static object GetTextPieces(ModelBase model)
        {
            var viewModel = (LineViewModel)model;

            var e = new LineFormatEventArgs(viewModel);
            viewModel._owner.RaiseFormatLine(e);

            return e.BuildTextPieces();
        }

        /// <summary>
        /// Gets the text piece containing the specified column.
        /// </summary>
        /// <param name="column">The column.</param>
        internal TextPieceLocation GetTextPiece(int column)
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

        /// <summary>
        /// Inserts the provided text at the specified column.
        /// </summary>
        /// <param name="column">The column to insert at.</param>
        /// <param name="str">The string to insert.</param>
        /// <remarks>
        /// Columns are 1-based, so inserting "a" at column 3 of "Test" would result in "Teast".
        /// </remarks>
        internal void Insert(int column, string str)
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

            Debug.Assert(newPieces.Count > 0);
            SetValue(TextPiecesProperty, newPieces.ToArray());

            _owner.RaiseLineChanged(new LineEventArgs(this));
        }

        /// <summary>
        /// Removes the text from <paramref name="startColumn"/> to <paramref name="endColumn"/> (inclusive)
        /// </summary>
        /// <param name="startColumn">The first column to remove.</param>
        /// <param name="endColumn">The last column to remove.</param>
        /// <remarks>
        /// Columns are 1-based, so removing columns 2 through 3 of "Test" would result in "Tt".
        /// </remarks>
        internal void Remove(int startColumn, int endColumn)
        {
            Debug.Assert(endColumn >= startColumn);

            // deleting columns 1 through 1 (first character) is really Text[0] because it's 0-based
            startColumn--;
            Debug.Assert(startColumn >= 0);

            var text = PendingText ?? Text;
            var newPieces = new List<TextPiece>(TextPieces); // make sure TextPieces are captured before we update PendingText

            endColumn--;
            Debug.Assert(endColumn <= text.Length);

            // update text
            int removeCount = endColumn - startColumn + 1;
            text = text.Remove(startColumn, removeCount);
            PendingText = text;

            // update the text pieces
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

                if (index + removeCount >= piece.Text.Length)
                {
                    if (index > 0)
                    {
                        removeCount -= (piece.Text.Length - index);
                        piece.Text = piece.Text.Substring(0, index);
                        if (removeCount == 0)
                            break;

                        Debug.Assert(pieceIndex < newPieces.Count);
                        piece = newPieces[pieceIndex++];
                        index = 0;
                    }

                    while (removeCount >= piece.Text.Length)
                    {
                        removeCount -= piece.Text.Length;

                        if (newPieces.Count > 1)
                            newPieces.RemoveAt(pieceIndex - 1);
                        else
                            newPieces[pieceIndex - 1].Text = "";

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

            Debug.Assert(newPieces.Count > 0);
            SetValue(TextPiecesProperty, newPieces.ToArray());

            // update selection
            int newSelectionStart = SelectionStart;
            if (newSelectionStart != 0)
            {
                // reset values for comparisons
                removeCount = endColumn - startColumn + 1;
                startColumn++;
                endColumn++;

                if (newSelectionStart > startColumn)
                {
                    if (newSelectionStart <= endColumn)
                        newSelectionStart = startColumn;
                    else
                        newSelectionStart -= removeCount;
                }

                int newSelectionEnd = SelectionEnd;
                if (newSelectionEnd >= startColumn)
                {
                    if (newSelectionEnd < endColumn)
                    {
                        if (SelectionStart > startColumn)
                            newSelectionStart = 0;
                        newSelectionEnd = newSelectionStart;
                    }
                    else
                    {
                        newSelectionEnd -= removeCount;
                    }
                }

                if (newSelectionStart > newSelectionEnd)
                    ClearSelection();
                else
                    Select(newSelectionStart, newSelectionEnd);
            }

            // notify line updated
            _owner.RaiseLineChanged(new LineEventArgs(this));
        }

        /// <summary>
        /// Clears the selection.
        /// </summary>
        internal void ClearSelection()
        {
            Select(0, 0);
        }

        /// <summary>
        /// Selects the text from <paramref name="startColumn"/> to <paramref name="endColumn"/> (inclusive).
        /// </summary>
        internal void Select(int startColumn, int endColumn)
        {
            var oldStartColumn = SelectionStart;
            if (startColumn != oldStartColumn)
            {
                // framework trickery to update both values before raising property changed events
                SetValueCore(SelectionStartProperty, startColumn);
                SelectionEnd = endColumn;
                OnModelPropertyChanged(new ModelPropertyChangedEventArgs(SelectionStartProperty, oldStartColumn, startColumn));
            }
            else
            {
                SelectionEnd = endColumn;
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append(Line);
            if (PendingText != null)
                builder.Append('*');
            builder.Append(": ");
            builder.Append(PendingText ?? Text);
            return builder.ToString();
        }
    }
}
