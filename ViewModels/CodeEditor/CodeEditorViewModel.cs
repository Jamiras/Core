using Jamiras.Components;
using Jamiras.DataModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using Jamiras.ViewModels.Fields;

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

        public static readonly ModelProperty CursorColumnProperty = ModelProperty.Register(typeof(CodeEditorViewModel), "CursorColumn", typeof(int), 1, OnCursorColumnChanged);
        public int CursorColumn
        {
            get { return (int)GetValue(CursorColumnProperty); }
            set
            {
                if (value < 1)
                {
                    value = 0;
                }
                else
                {
                    var lineViewModel = _lines[CursorLine - 1];
                    var lineLength = lineViewModel.LineLength;
                    if (lineLength == 0)
                        value = 1;
                    else if (value > lineLength + 1)
                        value = lineLength + 1;
                }

                SetValue(CursorColumnProperty, value);
            }
        }

        private static void OnCursorColumnChanged(object sender, ModelPropertyChangedEventArgs e)
        {
            var viewModel = (CodeEditorViewModel)sender;

            var line = viewModel.CursorLine;
            if (line > 0 && line <= viewModel._lines.Count)
            {
                var lineViewModel = viewModel._lines[line - 1];

                var newColumn = (int)e.NewValue;
                if (newColumn < 1)
                {
                    newColumn = 0;
                }
                else 
                {
                    var lineLength = lineViewModel.LineLength;
                    if (lineLength == 0)
                        newColumn = 1;
                    else if (newColumn > lineLength + 1)
                        newColumn = lineLength + 1;
                }

                lineViewModel.CursorColumn = newColumn;
            }
        }

        public static readonly ModelProperty CursorLineProperty = ModelProperty.Register(typeof(CodeEditorViewModel), "CursorLine", typeof(int), 1, OnCursorLineChanged);
        public int CursorLine
        {
            get { return (int)GetValue(CursorLineProperty); }
            set
            {
                if (value < 1)
                    value = 1;
                else if (value > LineCount)
                    value = LineCount;

                SetValue(CursorLineProperty, value);
            }
        }

        private static void OnCursorLineChanged(object sender, ModelPropertyChangedEventArgs e)
        {
            var viewModel = (CodeEditorViewModel)sender;

            int oldLine = (int)e.OldValue;
            if (oldLine > 0 && oldLine <= viewModel._lines.Count)
                viewModel._lines[oldLine - 1].CursorColumn = 0;

            int newLine = (int)e.NewValue;
            if (newLine > 0 && newLine <= viewModel._lines.Count)
            {
                var lineViewModel = viewModel._lines[(int)e.NewValue - 1];
                lineViewModel.CursorColumn = Math.Min(viewModel.CursorColumn, lineViewModel.LineLength + 1);
            }
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

        private LineViewModel CursorLineViewModel
        {
            get { return _lines[CursorLine - 1]; }
        }

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
            switch (e.Key)
            {
                case Key.Down:
                    CursorLine++;
                    UpdateVirtualCursorColumn();
                    e.Handled = true;
                    break;

                case Key.Up:
                    CursorLine--;
                    UpdateVirtualCursorColumn();
                    e.Handled = true;
                    break;

                case Key.Left:
                    HandleLeft(e.Modifiers);
                    ClearVirtualCursorColumn();
                    e.Handled = true;
                    break;

                case Key.Right:
                    HandleRight(e.Modifiers);
                    ClearVirtualCursorColumn();
                    e.Handled = true;
                    break;

                case Key.PageDown:
                    CursorLine += VisibleLines - 1;
                    UpdateVirtualCursorColumn();
                    e.Handled = true;
                    break;

                case Key.PageUp:
                    CursorLine -= VisibleLines - 1;
                    UpdateVirtualCursorColumn();
                    e.Handled = true;
                    break;

                case Key.Home:
                    if (e.Modifiers == ModifierKeys.Control)
                        CursorLine = 1;
                    CursorColumn = 1;
                    ClearVirtualCursorColumn();
                    e.Handled = true;
                    break;

                case Key.End:
                    if (e.Modifiers == ModifierKeys.Control)
                        CursorLine = _lines.Count;
                    CursorColumn = CursorLineViewModel.LineLength + 1;
                    ClearVirtualCursorColumn();
                    e.Handled = true;
                    break;

                case Key.Back:
                    if (CursorColumn > 1)
                    {
                        CursorLineViewModel.Remove(CursorColumn - 1, CursorColumn - 1);
                        CursorColumn--;
                    }
                    else if (CursorLine > 1)
                    {
                        CursorLine--;
                        CursorColumn = CursorLineViewModel.LineLength + 1;
                        MergeNextLine();
                    }
                    ClearVirtualCursorColumn();
                    e.Handled = true;
                    break;

                case Key.Delete:
                    if (CursorColumn <= CursorLineViewModel.LineLength)
                    {
                        _lines[CursorLine - 1].Remove(CursorColumn, CursorColumn);
                    }
                    else if (CursorLine < LineCount)
                    {
                        MergeNextLine();
                    }
                    ClearVirtualCursorColumn();
                    e.Handled = true;
                    break;

                case Key.Enter:
                    SplitLineAtCursor();
                    ClearVirtualCursorColumn();
                    e.Handled = true;
                    break;

                case Key.Tab:
                    IndentSelection();
                    ClearVirtualCursorColumn();
                    e.Handled = true;
                    break;

                default:
                    char c = e.GetChar();
                    if (c != '\0')
                    {
                        CursorLineViewModel.Insert(CursorColumn, c.ToString());
                        CursorColumn++;
                        ClearVirtualCursorColumn();
                        e.Handled = true;
                    }
                    break;
            }
        }

        private void HandleLeft(ModifierKeys modifier)
        {
            if (modifier == ModifierKeys.Control)
            {
                var cursorLine = CursorLineViewModel;
                var text = cursorLine.Text;
                var count = 0;
                var offset = CursorColumn - 2;
                while (offset > 0 && Char.IsWhiteSpace(text[offset]))
                {
                    offset--;
                    count++;
                }

                if (offset < 0)
                {
                    if (CursorLine > 1)
                    {
                        CursorLine--;
                        CursorColumn = CursorLineViewModel.LineLength + 1;
                    }
                }
                else
                {
                    var textPiece = cursorLine.GetTextPiece(offset + 1);
                    var pieceLength = textPiece.Offset + 1;
                    var pieceCount = 0;
                    while (pieceCount < pieceLength && !Char.IsWhiteSpace(text[offset]))
                    {
                        offset--;
                        pieceCount++;
                    }

                    CursorColumn -= (count + pieceCount);
                }
            }
            else if (CursorColumn == 1)
            {
                if (CursorLine > 1)
                {
                    CursorLine--;
                    CursorColumn = CursorLineViewModel.LineLength + 1;
                }
            }
            else
            {
                CursorColumn--;
            }
        }

        private void HandleRight(ModifierKeys modifier)
        {
            var cursorLine = CursorLineViewModel;
            if (CursorColumn > cursorLine.LineLength)
            {
                if (CursorLine < _lines.Count)
                {
                    CursorLine++;
                    if (modifier == ModifierKeys.Control)
                    {
                        var text = CursorLineViewModel.Text;
                        var count = 0;
                        while (count < text.Length && Char.IsWhiteSpace(text[count]))
                            count++;

                        CursorColumn = count + 1;
                    }
                    else
                    {
                        CursorColumn = 1;
                    }
                }
            }
            else if (modifier == ModifierKeys.Control)
            {
                var currentTextPiece = cursorLine.GetTextPiece(CursorColumn);
                if (currentTextPiece.Piece == null)
                {
                    CursorColumn = cursorLine.LineLength + 1;
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
                        currentTextPiece = cursorLine.GetTextPiece(CursorColumn + count);
                        if (currentTextPiece.Piece != null)
                        {
                            text = currentTextPiece.Piece.Text;
                            while (offset < text.Length && Char.IsWhiteSpace(text[offset]))
                                offset++;
                        }
                    }

                    CursorColumn += (count + offset);
                }
            }
            else
            {
                CursorColumn++;
            }
        }

        // remebers the cursor column when moving up or down even if the line doesn't have that many columns
        private int? _virtualCursorColumn;

        private void UpdateVirtualCursorColumn()
        {
            if (_virtualCursorColumn == null)
                _virtualCursorColumn = CursorColumn;

            var maxColumn = CursorLineViewModel.LineLength + 1;
            CursorColumn = Math.Min(maxColumn, _virtualCursorColumn.GetValueOrDefault());
        }

        private void ClearVirtualCursorColumn()
        {
            _virtualCursorColumn = null;
        }

        private void IndentSelection()
        {
            var cursorColumn = CursorColumn;
            int newColumn = (((cursorColumn - 1) / 4) + 1) * 4 + 1;
            CursorLineViewModel.Insert(CursorColumn, new string(' ', newColumn - cursorColumn));
            CursorColumn = newColumn;
        }

        private void MergeNextLine()
        {
            // merge the text from the next line into the current line
            var cursorLineViewModel = CursorLineViewModel;
            var left = cursorLineViewModel.PendingText ?? cursorLineViewModel.Text;
            var nextLineViewModel = _lines[CursorLine];
            var right = nextLineViewModel.PendingText ?? nextLineViewModel.Text;
            cursorLineViewModel.PendingText = left + right;

            // merge the TextPieces so the merged text appears
            var newPieces = new List<TextPiece>(cursorLineViewModel.TextPieces);
            newPieces.AddRange(nextLineViewModel.TextPieces);
            cursorLineViewModel.SetValue(LineViewModel.TextPiecesProperty, newPieces.ToArray());

            // remove the line that was merged
            _lines.RemoveAt(CursorLine);
            LineCount--;

            // update the line numbers
            for (int i = CursorLine; i < _lines.Count; i++)
                _lines[i].Line--;

            // schedule a refresh to update the syntax highlighting
            ScheduleRefresh();
        }

        private void SplitLineAtCursor()
        {
            // split the current line at the cursor
            var cursorLineViewModel = CursorLineViewModel;
            string text = cursorLineViewModel.PendingText ?? cursorLineViewModel.Text;
            var cursorColumn = CursorColumn - 1; // string index is 0-based
            string left = (cursorColumn > 0) ? text.Substring(0, cursorColumn) : String.Empty;
            string right = (cursorColumn < text.Length) ? text.Substring(cursorColumn) : String.Empty;

            // truncate the first line
            if (right.Length > 0)
                cursorLineViewModel.Remove(CursorColumn, text.Length);

            // add a new line
            var newLineViewModel = new LineViewModel(this, CursorLine + 1) { PendingText = right };
            _lines.Insert(CursorLine, newLineViewModel);
            LineCount++;

            // create TextPieces for the new line so it appears
            var e = new LineChangedEventArgs(newLineViewModel);
            newLineViewModel.SetValue(LineViewModel.TextPiecesProperty, e.BuildTextPieces());

            // update the cursor position
            CursorLine++;
            CursorColumn = 1;

            // update the line numbers
            for (int i = CursorLine; i < _lines.Count; i++)
                _lines[i].Line++;

            // schedule a refresh to update the syntax highlighting
            ScheduleRefresh();
        }
    }
}
