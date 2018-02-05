using Jamiras.Components;
using Jamiras.DataModels;
using Jamiras.ViewModels.Fields;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Jamiras.ViewModels.CodeEditor
{
    public class CodeEditorViewModel : ViewModelBase
    {
        public CodeEditorViewModel()
        {
            _lines = new ObservableCollection<LineViewModel>();

            Style = new EditorProperties();
            Resources = new EditorResources(Style);

            var formattedText = new FormattedText("0", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(Resources.FontName), Resources.FontSize, Brushes.Black); 
            SetValue(CharacterWidthProperty, formattedText.Width);
        }

        private static readonly ModelProperty CharacterWidthProperty = ModelProperty.Register(typeof(CodeEditorViewModel), null, typeof(double), 8.0);
        internal double CharacterWidth
        {
            get { return (double)GetValue(CharacterWidthProperty); }
        }

        public static readonly ModelProperty AreLineNumbersVisibleProperty = ModelProperty.Register(typeof(CodeEditorViewModel), "AreLineNumbersVisible", typeof(bool), true);
        public bool AreLineNumbersVisible
        {
            get { return (bool)GetValue(AreLineNumbersVisibleProperty); }
            set { SetValue(AreLineNumbersVisibleProperty, value); }
        }

        public static readonly ModelProperty CursorColumnProperty = ModelProperty.Register(typeof(CodeEditorViewModel), "CursorColumn", typeof(int), 1);
        public int CursorColumn
        {
            get { return (int)GetValue(CursorColumnProperty); }
            private set { SetValue(CursorColumnProperty, value); }
        }

        public static readonly ModelProperty CursorLineProperty = ModelProperty.Register(typeof(CodeEditorViewModel), "CursorLine", typeof(int), 1);
        public int CursorLine
        {
            get { return (int)GetValue(CursorLineProperty); }
            private set { SetValue(CursorLineProperty, value); }
        }

        public string Content
        {
            get { return BuildContent(); }
            set { SetContent(value); }
        }

        private string BuildContent()
        {
            var builder = new StringBuilder();
            foreach (var line in _lines)
                builder.AppendLine(line.Text);

            return builder.ToString();
        }

        private void SetContent(string value)
        {
            _lines.Clear();

            int lineIndex = 1;
            var tokenizer = Tokenizer.CreateTokenizer(value);
            do
            {
                var line = tokenizer.ReadTo('\n');
                if (line.Length > 0 && line[line.Length - 1] == '\r')
                    line = line.SubToken(0, line.Length - 1);
                tokenizer.Advance();

                var lineViewModel = new LineViewModel(this, lineIndex) { Text = line.ToString() };
                _lines.Add(lineViewModel);
                lineIndex++;
            } while (tokenizer.NextChar != '\0');

            LineCount = _lines.Count;
            CursorLine = 1;
            CursorColumn = 1;

            OnContentChanged(value);
        }

        protected virtual void OnContentChanged(string newValue)
        {

        }

        internal void ScheduleRefresh()
        {
            TextFieldViewModelBase.WaitForTyping(Refresh);
        }

        public override void Refresh()
        {
            var updatedLines = new List<LineViewModel>();

            var newContent = new StringBuilder();
            foreach (var line in _lines)
            {
                var pendingText = line.PendingText;
                if (pendingText != null)
                {
                    newContent.AppendLine(pendingText);
                    updatedLines.Add(line);
                }
                else
                {
                    newContent.AppendLine(line.Text);
                }
            }

            OnContentChanged(newContent.ToString());

            foreach (var line in updatedLines)
            {
                var pendingText = line.PendingText;
                line.PendingText = null;
                line.Text = pendingText;
            }
        }

        public EventHandler<LineChangedEventArgs> LineChanged;
        internal void RaiseLineChanged(LineChangedEventArgs e)
        {
            OnLineChanged(e);
        }
        protected virtual void OnLineChanged(LineChangedEventArgs e)
        {
            if (LineChanged != null)
                LineChanged(this, e);
        }

        public static readonly ModelProperty LineCountProperty = ModelProperty.Register(typeof(CodeEditorViewModel), "LineCount", typeof(int), 1);
        public int LineCount
        {
            get { return (int)GetValue(LineCountProperty); }
            private set { SetValue(LineCountProperty, value); }
        }

        public IEnumerable<LineViewModel> Lines
        {
            get { return _lines; }
        }
        private ObservableCollection<LineViewModel> _lines;

        public EditorProperties Style { get; private set; }

        public EditorResources Resources { get; private set; }

        public static readonly ModelProperty LineNumberColumnWidthProperty =
            ModelProperty.RegisterDependant(typeof(CodeEditorViewModel), "LineNumberColumnWidth", typeof(double),
                new[] { LineCountProperty, CharacterWidthProperty }, GetLineNumberColumnWidth);
        public double LineNumberColumnWidth
        {
            get { return (double)GetValue(LineNumberColumnWidthProperty); }
        }
        private static object GetLineNumberColumnWidth(ModelBase model)
        {
            var viewModel = (CodeEditorViewModel)model;
            double characterWidth = viewModel.CharacterWidth;

            var lineCount = viewModel.LineCount;
            if (lineCount < 100)
                return characterWidth * 4;
            if (lineCount < 1000)
                return characterWidth * 5;
            if (lineCount < 10000)
                return characterWidth * 6;
            return characterWidth * 7;
        }

        internal static readonly ModelProperty VisibleLinesProperty = ModelProperty.Register(typeof(CodeEditorViewModel), "VisibleLines", typeof(int), 20);
        internal int VisibleLines
        {
            get { return (int)GetValue(VisibleLinesProperty); }
            set { SetValue(VisibleLinesProperty, value); }
        }

        internal bool HandleKey(Key key, ModifierKeys modifiers)
        {
            var e = new KeyPressedEventArgs(key, modifiers);
            OnKeyPressed(e);
            return e.Handled;
        }

        protected virtual void OnKeyPressed(KeyPressedEventArgs e)
        {
            var moveCursorFlags = ((e.Modifiers & ModifierKeys.Shift) != 0) ? MoveCursorFlags.Highlighting : MoveCursorFlags.None;

            switch (e.Key)
            {
                case Key.Down:
                    MoveCursorTo(CursorLine + 1, CursorColumn, moveCursorFlags | MoveCursorFlags.RememberColumn);
                    e.Handled = true;
                    break;

                case Key.Up:
                    MoveCursorTo(CursorLine - 1, CursorColumn, moveCursorFlags | MoveCursorFlags.RememberColumn);
                    e.Handled = true;
                    break;

                case Key.Left:
                    HandleLeft(moveCursorFlags, (e.Modifiers & ModifierKeys.Control) != 0);
                    e.Handled = true;
                    break;

                case Key.Right:
                    HandleRight(moveCursorFlags, (e.Modifiers & ModifierKeys.Control) != 0);
                    e.Handled = true;
                    break;

                case Key.PageDown:
                    MoveCursorTo(CursorLine + (VisibleLines - 1), CursorColumn, moveCursorFlags | MoveCursorFlags.RememberColumn);
                    e.Handled = true;
                    break;

                case Key.PageUp:
                    MoveCursorTo(CursorLine - (VisibleLines - 1), CursorColumn, moveCursorFlags | MoveCursorFlags.RememberColumn);
                    e.Handled = true;
                    break;

                case Key.Home:
                    if ((e.Modifiers & ModifierKeys.Control) != 0)
                        MoveCursorTo(1, 1, moveCursorFlags);
                    else
                        MoveCursorTo(CursorLine, 1, moveCursorFlags);
                    e.Handled = true;
                    break;

                case Key.End:
                    if ((e.Modifiers & ModifierKeys.Control) != 0)
                        MoveCursorTo(_lines.Count, _lines[_lines.Count - 1].LineLength + 1, moveCursorFlags);
                    else
                        MoveCursorTo(CursorLine, _lines[CursorLine - 1].LineLength + 1, moveCursorFlags);
                    e.Handled = true;
                    break;

                case Key.Back:
                    HandleBackspace();
                    e.Handled = true;
                    break;

                case Key.Delete:
                    HandleDelete();
                    e.Handled = true;
                    break;

                case Key.Enter:
                    HandleEnter();
                    e.Handled = true;
                    break;

                case Key.Tab:
                    HandleTab();
                    e.Handled = true;
                    break;

                case Key.A:
                    if ((e.Modifiers & ModifierKeys.Control) != 0)
                    {
                        SelectAll();
                        e.Handled = true;
                    }
                    else
                    {
                        goto default;
                    }
                    break;

                default:
                    char c = e.GetChar();
                    if (c != '\0')
                    {
                        HandleCharacter(c);
                        e.Handled = true;
                    }
                    break;
            }
        }

        private void HandleCharacter(char c)
        {
            if (_selectionStartLine != 0)
                DeleteSelection();

            var column = CursorColumn;
            var line = CursorLine;
            var lineViewModel = _lines[line - 1];

            lineViewModel.Insert(column, c.ToString());
            MoveCursorTo(line, column + 1, MoveCursorFlags.None);
        }

        private void HandleDelete()
        {
            if (_selectionStartLine != 0)
            {
                DeleteSelection();
            }
            else
            {
                var column = CursorColumn;
                var line = CursorLine;

                var lineViewModel = _lines[line - 1];
                if (column <= lineViewModel.LineLength)
                {
                    lineViewModel.Remove(column, column);
                }
                else if (line < LineCount)
                {
                    MergeNextLine();
                }
            }
        }

        private void HandleBackspace()
        {
            if (_selectionStartLine != 0)
            {
                DeleteSelection();
            }
            else
            {
                var column = CursorColumn;
                var line = CursorLine;

                if (column > 1)
                {
                    column--;
                    _lines[line - 1].Remove(column, column);
                    MoveCursorTo(line, column, MoveCursorFlags.None);
                }
                else if (line > 1)
                {
                    line--;
                    column = _lines[line - 1].LineLength + 1;
                    MoveCursorTo(line, column, MoveCursorFlags.None);
                    MergeNextLine();
                }
            }
        }

        public void SelectAll()
        {
            MoveCursorTo(LineCount, Int32.MaxValue, MoveCursorFlags.None);
            MoveCursorTo(1, 1, MoveCursorFlags.Highlighting);
        }

        private void DeleteSelection()
        {
            // TODO
        }

        public void HighlightWordAt(int line, int column)
        {
            var cursorLineViewModel = _lines[line - 1];
            var currentTextPiece = cursorLineViewModel.GetTextPiece(column);
            if (currentTextPiece.Piece == null) // column exceeds line length
                return;

            var text = currentTextPiece.Piece.Text;
            var offset = currentTextPiece.Offset;

            int wordStart, wordEnd;
            if (Char.IsWhiteSpace(text[offset]))
            {
                do
                {
                    offset--;
                } while (offset >= 0 && Char.IsWhiteSpace(text[offset]));

                wordStart = column - (currentTextPiece.Offset - offset) + 1;

                offset = currentTextPiece.Offset;
                do
                {
                    offset++;
                } while (offset < text.Length && Char.IsWhiteSpace(text[offset]));

                wordEnd = column + (offset - currentTextPiece.Offset);
            }
            else
            {
                do
                {
                    offset--;
                } while (offset >= 0 && !Char.IsWhiteSpace(text[offset]));

                wordStart = column - (currentTextPiece.Offset - offset) + 1;

                offset = currentTextPiece.Offset;
                do
                {
                    offset++;
                } while (offset < text.Length && !Char.IsWhiteSpace(text[offset]));

                wordEnd = column + (offset - currentTextPiece.Offset);
            }

            MoveCursorTo(line, wordStart, MoveCursorFlags.None);
            MoveCursorTo(line, wordEnd, MoveCursorFlags.Highlighting);
        }

        private void HandleLeft(MoveCursorFlags flags, bool nextWord)
        {
            var newLine = CursorLine;
            var newColumn = CursorColumn;

            if (nextWord)
            {
                var cursorLineViewModel = _lines[newLine - 1];
                var text = cursorLineViewModel.Text;
                var count = 0;
                var offset = CursorColumn - 2;
                while (offset >= 0 && Char.IsWhiteSpace(text[offset]))
                {
                    offset--;
                    count++;
                }

                if (offset < 0)
                {
                    // in whitespace at start of line, go to previous line
                    if (newLine > 1)
                    {
                        newLine--;
                        newColumn = _lines[newLine - 1].LineLength + 1;
                    }
                }
                else
                {
                    // find start of word
                    var textPiece = cursorLineViewModel.GetTextPiece(offset + 1);
                    var pieceLength = textPiece.Offset + 1;
                    var pieceCount = 0;
                    while (pieceCount < pieceLength && !Char.IsWhiteSpace(text[offset]))
                    {
                        offset--;
                        pieceCount++;
                    }

                    newColumn -= (count + pieceCount);
                }
            }
            else if (newColumn == 1)
            {
                if (newLine > 1)
                {
                    newLine--;
                    newColumn = _lines[newLine - 1].LineLength + 1;
                }
            }
            else
            {
                newColumn--;
            }

            MoveCursorTo(newLine, newColumn, flags);
        }

        private void HandleRight(MoveCursorFlags flags, bool nextWord)
        {
            var newLine = CursorLine;
            var newColumn = CursorColumn;

            var cursorLineViewModel = _lines[newLine - 1];
            if (newColumn > cursorLineViewModel.LineLength)
            {
                if (newLine < _lines.Count)
                {
                    newLine++;
                    if (nextWord)
                    {
                        var text = _lines[newLine - 1].Text;
                        var count = 0;
                        while (count < text.Length && Char.IsWhiteSpace(text[count]))
                            count++;

                        newColumn = count + 1;
                    }
                    else
                    {
                        newColumn = 1;
                    }
                }
            }
            else if (nextWord)
            {
                var currentTextPiece = cursorLineViewModel.GetTextPiece(newColumn);
                if (currentTextPiece.Piece == null)
                {
                    newColumn = cursorLineViewModel.LineLength + 1;
                }
                else
                {
                    var text = currentTextPiece.Piece.Text;
                    var offset = currentTextPiece.Offset;
                    var count = 0;
                    while ((offset + count) < text.Length && !Char.IsWhiteSpace(text[offset + count]))
                        count++;
                    while ((offset + count) < text.Length && Char.IsWhiteSpace(text[offset + count]))
                        count++;

                    offset = 0;
                    if (offset + count == text.Length)
                    {
                        currentTextPiece = cursorLineViewModel.GetTextPiece(CursorColumn + count);
                        if (currentTextPiece.Piece != null)
                        {
                            text = currentTextPiece.Piece.Text;
                            while (offset < text.Length && Char.IsWhiteSpace(text[offset]))
                                offset++;
                        }
                    }

                    newColumn += (count + offset);
                }
            }
            else
            {
                newColumn++;
            }

            MoveCursorTo(newLine, newColumn, flags);
        }

        private void HandleTab()
        {
            if (_selectionStartLine != 0) {
                // TODO: if entire line is selected, indent instead of delete
                DeleteSelection();
            }

            var cursorLine = CursorLine;
            var cursorColumn = CursorColumn;
            int newColumn = (((cursorColumn - 1) / 4) + 1) * 4 + 1;
            _lines[cursorLine - 1].Insert(cursorColumn, new string(' ', newColumn - cursorColumn));
            MoveCursorTo(cursorLine, newColumn, MoveCursorFlags.None);
            CursorColumn = newColumn;
        }

        private void MergeNextLine()
        {
            // merge the text from the next line into the current line
            var cursorLine = CursorLine;
            var cursorLineViewModel = _lines[cursorLine - 1];
            var left = cursorLineViewModel.PendingText ?? cursorLineViewModel.Text;
            var nextLineViewModel = _lines[cursorLine];
            var right = nextLineViewModel.PendingText ?? nextLineViewModel.Text;
            cursorLineViewModel.PendingText = left + right;

            // merge the TextPieces so the merged text appears
            var newPieces = new List<TextPiece>(cursorLineViewModel.TextPieces);
            newPieces.AddRange(nextLineViewModel.TextPieces);
            cursorLineViewModel.SetValue(LineViewModel.TextPiecesProperty, newPieces.ToArray());

            // remove the line that was merged
            _lines.RemoveAt(cursorLine);
            LineCount--;

            // update the line numbers
            for (int i = CursorLine; i < _lines.Count; i++)
                _lines[i].Line--;

            // schedule a refresh to update the syntax highlighting
            ScheduleRefresh();
        }

        private void HandleEnter()
        {
            if (_selectionStartLine != 0)
                DeleteSelection();

            // split the current line at the cursor
            var cursorLine = CursorLine;
            var cursorLineViewModel = _lines[cursorLine - 1];
            string text = cursorLineViewModel.PendingText ?? cursorLineViewModel.Text;
            var cursorColumn = CursorColumn - 1; // string index is 0-based
            string left = (cursorColumn > 0) ? text.Substring(0, cursorColumn) : String.Empty;
            string right = (cursorColumn < text.Length) ? text.Substring(cursorColumn) : String.Empty;

            // truncate the first line
            if (right.Length > 0)
                cursorLineViewModel.Remove(CursorColumn, text.Length);

            // add a new line
            var newLineViewModel = new LineViewModel(this, cursorLine + 1) { PendingText = right };
            _lines.Insert(cursorLine, newLineViewModel);
            LineCount++;

            // create TextPieces for the new line so it appears
            var e = new LineChangedEventArgs(newLineViewModel);
            newLineViewModel.SetValue(LineViewModel.TextPiecesProperty, e.BuildTextPieces());

            // update the cursor position
            MoveCursorTo(cursorLine + 1, 1, MoveCursorFlags.None);

            // update the line numbers
            for (int i = CursorLine; i < _lines.Count; i++)
                _lines[i].Line++;

            // schedule a refresh to update the syntax highlighting
            ScheduleRefresh();
        }

        // remebers the cursor column when moving up or down even if the line doesn't have that many columns
        private int? _virtualCursorColumn;

        private int _selectionStartLine, _selectionStartColumn, _selectionEndLine, _selectionEndColumn;

        public enum MoveCursorFlags
        {
            None = 0,
            Highlighting = 0x01,
            RememberColumn = 0x02,
        }

        public void MoveCursorTo(int line, int column, MoveCursorFlags flags)
        {
            var currentLine = CursorLine;
            var currentColumn = CursorColumn;

            // capture cursor location as start of highlighting if highlight region doesn't yet exist
            if ((flags & MoveCursorFlags.Highlighting) != 0 && _selectionStartLine == 0)
            {
                _selectionStartLine = _selectionEndLine = currentLine;
                _selectionStartColumn = _selectionEndColumn = currentColumn;
            }

            // switching lines, make sure the new line is valid
            if (line != currentLine)
            {
                if (line < 1)
                    line = 1;
                else if (line > LineCount)
                    line = LineCount;
            }

            // make sure cursor stays within the line's bounds
            var maxColumn = _lines[line - 1].LineLength + 1;

            if ((flags & MoveCursorFlags.RememberColumn) != 0)
            {
                if (_virtualCursorColumn == null)
                    _virtualCursorColumn = currentColumn;

                column = Math.Min(maxColumn, _virtualCursorColumn.GetValueOrDefault());
            }
            else
            {
                _virtualCursorColumn = null;
                if (column > maxColumn)
                    column = maxColumn;
            }

            // update highlighting
            if ((flags & MoveCursorFlags.Highlighting) == 0)
            {
                // remove highlighted region
                if (_selectionStartColumn != 0)
                {
                    if (_selectionStartLine > _selectionEndLine)
                    {
                        for (int i = _selectionEndLine - 1; i < _selectionStartLine; i++)
                            _lines[i].ClearSelection();
                    }
                    else
                    {
                        for (int i = _selectionStartLine - 1; i < _selectionEndLine; i++)
                            _lines[i].ClearSelection();
                    }

                    _selectionStartLine = 0;
                    _selectionStartColumn = 0;
                    _selectionEndLine = 0;
                    _selectionEndColumn = 0;
                }
            }
            else if (_selectionEndLine != line || _selectionEndColumn != column)
            {
                // update highlighted region
                if (_selectionEndLine != 0)
                {
                    for (int i = _selectionEndLine + 1; i < line; i++)
                        _lines[i - 1].ClearSelection();
                    for (int i = line + 1; i < _selectionEndLine; i++)
                        _lines[i - 1].ClearSelection();
                }

                _selectionEndLine = line;
                _selectionEndColumn = column;

                if (_selectionStartLine == _selectionEndLine)
                {
                    if (_selectionStartColumn < _selectionEndColumn)
                        _lines[_selectionStartLine - 1].Select(_selectionStartColumn, _selectionEndColumn);
                    else
                        _lines[_selectionStartLine - 1].Select(_selectionEndColumn, _selectionStartColumn);
                }
                else
                {
                    int firstLine, firstColumn, lastLine, lastColumn;
                    if (_selectionStartLine < _selectionEndLine)
                    {
                        firstLine = _selectionStartLine;
                        firstColumn = _selectionStartColumn;
                        lastLine = _selectionEndLine;
                        lastColumn = _selectionEndColumn;
                    }
                    else
                    {
                        firstLine = _selectionEndLine;
                        firstColumn = _selectionEndColumn;
                        lastLine = _selectionStartLine;
                        lastColumn = _selectionStartColumn;
                    }

                    _lines[firstLine - 1].Select(firstColumn, _lines[firstLine - 1].LineLength + 1);
                    for (int i = firstLine; i < lastLine - 1; i++)
                        _lines[i].Select(1, _lines[i].LineLength + 1);
                    _lines[lastLine - 1].Select(1, lastColumn);
                }
            }

            // update the cursor position
            if (line != currentLine)
            {
                _lines[currentLine - 1].CursorColumn = 0;
                _lines[line - 1].CursorColumn = column;

                if (column != currentColumn)
                {
                    // sneaky framework trick to set both values before raising propertychanged for either property
                    SetValueCore(CursorLineProperty, line);
                    CursorColumn = column;
                    OnModelPropertyChanged(new ModelPropertyChangedEventArgs(CursorLineProperty, line, currentLine));
                }
                else
                {
                    CursorLine = line;
                }
            }
            else if (column != currentColumn)
            {
                _lines[line - 1].CursorColumn = column;
                CursorColumn = column;
            }
        }
    }
}
