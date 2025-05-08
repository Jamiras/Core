using Jamiras.Components;
using Jamiras.DataModels;
using Jamiras.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows;

namespace Jamiras.ViewModels.CodeEditor
{
    /// <summary>
    /// Defines a single line in the <see cref="CodeEditor"/>
    /// </summary>
    /// <seealso cref="Jamiras.ViewModels.ViewModelBase" />
    public class LineViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LineViewModel"/> class.
        /// </summary>
        /// <param name="owner">The editor the line is to be displayed in.</param>
        /// <param name="line">The line number.</param>
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

            var pixelWidth = viewModel.Resources.GetPixelWidth(viewModel.Text, 0, selectionStart - 1);
            return new Thickness(Math.Ceiling(pixelWidth), 0, 0, 0);
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

            // selection indices are 1-based
            selectionStart--;

            var selectionEnd = viewModel.SelectionEnd;
            if (selectionStart > selectionEnd)
                return -viewModel.Resources.GetPixelWidth(viewModel.Text, selectionEnd, selectionStart - selectionEnd);

            return viewModel.Resources.GetPixelWidth(viewModel.Text, selectionStart, selectionEnd - selectionStart);
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

            if (viewModel.CursorColumn < 1)
                return new Thickness(0, 0, 0, 0);

            var pixelWidth = viewModel.Resources.GetPixelWidth(viewModel.Text, 0, viewModel.CursorColumn - 1);
            return new Thickness((int)Math.Ceiling(pixelWidth), 0, 0, 0);
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
        /// Gets the number of characters in the line.
        /// </summary>
        internal int LineLength
        {
            get
            {
                var text = CurrentText;
                return text.Length;
            }
        }

        /// <summary>
        /// Commits the <see cref="PendingText"/>.
        /// </summary>
        internal void CommitPending(ref bool isWhitespaceOnly)
        {
            bool needsRefresh = false;

            lock (_lockObject)
            {
                var pendingText = PendingText;
                if (pendingText != null)
                {
                    PendingText = null;
                    if (Text == pendingText)
                    {
                        // force update of TextPieces, even if Text didn't really change
                        needsRefresh = true;
                    }
                    else
                    {
                        // if another line has already indicated a non-whitespace change has occurred, we don't have to check
                        if (isWhitespaceOnly)
                            isWhitespaceOnly = DifferOnlyByWhitespace(Text, pendingText);

                        Text = pendingText;
                    }
                }
            }

            if (needsRefresh)
                Refresh();
        }

        private static bool IsWhitespaceOnlyChange(string str, int startIndex, int endIndex)
        {
            // if any non-whitespace character present in the changed portion of the string, return false
            for (int i = startIndex; i <= endIndex; i++)
            {
                if (!Char.IsWhiteSpace(str[i]))
                    return false;
            }

            // if the preceeding character is whitespace, return true
            if (startIndex == 0 || Char.IsWhiteSpace(str[startIndex - 1]))
                return true;

            // if the following character is whitespace, return true
            if (endIndex == str.Length - 1 || Char.IsWhiteSpace(str[endIndex + 1]))
                return true;

            // entire change is whitespace, but not neighbored by whitespace
            // indicates that a token was split, return false
            return false;
        }

        private static bool DifferOnlyByWhitespace(string left, string right)
        {
            var leftIndex = 0;
            var rightIndex = 0;
            var leftStop = left.Length - 1;
            var rightStop = right.Length - 1;

            while (leftIndex <= leftStop && rightIndex <= rightStop && left[leftIndex] == right[rightIndex])
            {
                leftIndex++;
                rightIndex++;
            }

            while (leftIndex <= leftStop && rightIndex <= rightStop && left[leftStop] == right[rightStop])
            {
                leftStop--;
                rightStop--;
            }

            // text added to right string, or removed from left string
            if (leftIndex > leftStop)
            {
                // no differences
                if (rightIndex > rightStop)
                    return true;

                return IsWhitespaceOnlyChange(right, rightIndex, rightStop);
            }

            // text added to left string, or removed from right string
            if (rightIndex > rightStop)
                return IsWhitespaceOnlyChange(left, leftIndex, leftStop);

            // text changed, both sides must be whitespace only
            for (int i = leftIndex; i <= leftStop; i++)
            {
                if (!Char.IsWhiteSpace(left[i]))
                    return false;
            }
            for (int i = rightIndex; i <= rightStop; i++)
            {
                if (!Char.IsWhiteSpace(right[i]))
                    return false;
            }

            // text changed, but only from one form of whitespace to another
            return true;
        }

        internal bool IsVisible { get; set; }

        /// <summary>
        /// Reconstructs the syntax highlighting for the line.
        /// </summary>
        public override void Refresh()
        {
            if (IsVisible)
            {
                if (IsValueUninitialized(TextPiecesProperty))
                    return;

                var oldPieces = TextPieces;
                var newPieces = (IEnumerable<TextPiece>)GetTextPieces(this);
                if (TextPiecesChanged(oldPieces, newPieces))
                    SetValue(TextPiecesProperty, newPieces);
            }
            else
            {
                // reset dependent property to uninitialized value - if it triggers a PropertyChanged event, bound record will ask for the updated value and it will be re-evaluated.
                SetValue(TextPiecesProperty, TextPiecesProperty.DefaultValue);
            }
        }

        /// <summary>
        /// Gets or sets text being typed.
        /// </summary>
        internal string PendingText { get; set; }

        /// <summary>
        /// Gets the current text value (may differ from <see cref="Text"/> if user is typing.
        /// </summary>
        public string CurrentText
        {
            get
            {
                lock (_lockObject)
                {
                    return PendingText ?? Text;
                }
            }
        }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="Text"/>
        /// </summary>
        public static readonly ModelProperty TextProperty = ModelProperty.Register(typeof(LineViewModel), "Text", typeof(string), string.Empty);

        /// <summary>
        /// Gets the text in the line.
        /// </summary>
        /// <remarks>
        /// May not be updated if the user is in the middle of typing. Use <see cref="CurrentText"/> for that.
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
            if (viewModel.Text.Length == 0)
                return new TextPiece[] { new TextPiece { Text = "", Foreground = viewModel.Resources.Foreground.Brush } };

            var e = new LineFormatEventArgs(viewModel);
            try
            {
                viewModel._owner.RaiseFormatLine(e);
            }
            catch (Exception ex)
            {
                if (!ServiceRepository.Instance.FindService<IExceptionDispatcher>().TryHandleException(ex))
                    throw;
            }

            return e.BuildTextPieces();
        }

        private static bool TextPiecesChanged(IEnumerable<TextPiece> oldPieces, IEnumerable<TextPiece> newPieces)
        {
            var oldEnumerator = oldPieces.GetEnumerator();
            var newEnumerator = newPieces.GetEnumerator();

            while (oldEnumerator.MoveNext())
            {
                if (!newEnumerator.MoveNext())
                    return true;

                if (oldEnumerator.Current != newEnumerator.Current)
                    return true;
            }

            if (newEnumerator.MoveNext())
                return true;

            return false;
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

                var textPieces = TextPieces;
                if (textPieces != null)
                {
                    foreach (var textPiece in textPieces)
                    {
                        if (textPiece.Text.Length > column)
                            return new TextPieceLocation { Piece = textPiece, Offset = column };

                        column -= textPiece.Text.Length;
                    }
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
            var newPieces = new List<TextPiece>(TextPieces); // may call _owner.RaiseFormatLine. don't call inside lock

            // cursor between characters 1 and 2 is inserting at column 2, but since the string is indexed via 0-based indexing, adjust the insert location
            column--;
            Debug.Assert(column >= 0);

            lock (_lockObject)
            {
                var text = PendingText ?? Text;

                Debug.Assert(column <= text.Length);

                text = text.Insert(column, str);
                PendingText = text;

                var pieces = TextPieces;
                Debug.Assert(pieces != null);
                newPieces = new List<TextPiece>(pieces);

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
            var newPieces = new List<TextPiece>(TextPieces); // may call _owner.RaiseFormatLine. don't call inside lock
            int removeCount;

            Debug.Assert(endColumn >= startColumn);

            // deleting columns 1 through 1 (first character) is really Text[0] because it's 0-based
            startColumn--;
            Debug.Assert(startColumn >= 0);

            lock (_lockObject)
            {
                var text = PendingText ?? Text;

                endColumn--;
                Debug.Assert(endColumn <= text.Length);

                // update text
                removeCount = endColumn - startColumn + 1;
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
            lock (_lockObject)
            {
                builder.Append(Line);
                if (PendingText != null)
                    builder.Append('*');
                builder.Append(": ");
                builder.Append(PendingText ?? Text);
            }
            return builder.ToString();
        }
    }
}
