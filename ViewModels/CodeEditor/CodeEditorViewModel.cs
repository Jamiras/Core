using Jamiras.Commands;
using Jamiras.Components;
using Jamiras.DataModels;
using Jamiras.Services;
using Jamiras.ViewModels.CodeEditor.ToolWindows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Jamiras.ViewModels.CodeEditor
{
    /// <summary>
    /// View model for a simple code editor
    /// </summary>
    public class CodeEditorViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CodeEditorViewModel"/> class.
        /// </summary>
        public CodeEditorViewModel()
            : this(ServiceRepository.Instance.FindService<IClipboardService>(),
                   ServiceRepository.Instance.FindService<ITimerService>())
        {
        }

        internal CodeEditorViewModel(IClipboardService clipboardService, ITimerService timerService)
        {
            _clipboardService = clipboardService;
            _timerService = timerService;

            _lines = new ObservableCollection<LineViewModel>();
            _linesWrapper = new ReadOnlyObservableCollection<LineViewModel>(_lines);

            _undoStack = new FixedSizeStack<UndoItem>(128);
            _redoStack = new Stack<UndoItem>(32);

            Style = new EditorProperties();
            Resources = new EditorResources(Style);

            GotoLineCommand = new DelegateCommand(HandleGotoLine);
            UndoCommand = new DelegateCommand(HandleUndo);
            RedoCommand = new DelegateCommand(HandleRedo);
            CutCommand = new DelegateCommand(CutSelection);
            CopyCommand = new DelegateCommand(CopySelection);
            PasteCommand = new DelegateCommand(HandlePaste);
        }

        private readonly ITimerService _timerService;
        private readonly IClipboardService _clipboardService;

        private int _selectionStartLine, _selectionStartColumn, _selectionEndLine, _selectionEndColumn;

        private FixedSizeStack<UndoItem> _undoStack;
        private Stack<UndoItem> _redoStack;

        // remebers the cursor column when moving up or down even if the line doesn't have that many columns
        private int? _virtualCursorColumn;

        /// <summary>
        /// Gets the goto line command.
        /// </summary>
        public CommandBase GotoLineCommand { get; private set; }

        /// <summary>
        /// Gets the undo command.
        /// </summary>
        public CommandBase UndoCommand { get; private set; }

        /// <summary>
        /// Gets the redo command.
        /// </summary>
        public CommandBase RedoCommand { get; private set; }

        /// <summary>
        /// Gets the cut command.
        /// </summary>
        public CommandBase CutCommand { get; private set; }

        /// <summary>
        /// Gets the copy command.
        /// </summary>
        public CommandBase CopyCommand { get; private set; }

        /// <summary>
        /// Gets the paste command.
        /// </summary>
        public CommandBase PasteCommand { get; private set; }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="AreLineNumbersVisible"/>
        /// </summary>
        public static readonly ModelProperty AreLineNumbersVisibleProperty = ModelProperty.Register(typeof(CodeEditorViewModel), "AreLineNumbersVisible", typeof(bool), true);

        /// <summary>
        /// Gets or sets a value indicating whether line numbers should be displayed.
        /// </summary>
        public bool AreLineNumbersVisible
        {
            get { return (bool)GetValue(AreLineNumbersVisibleProperty); }
            set { SetValue(AreLineNumbersVisibleProperty, value); }
        }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="CursorColumn"/>
        /// </summary>
        public static readonly ModelProperty CursorColumnProperty = ModelProperty.Register(typeof(CodeEditorViewModel), "CursorColumn", typeof(int), 1);

        /// <summary>
        /// Gets the column where the cursor is currently located.
        /// </summary>
        public int CursorColumn
        {
            get { return (int)GetValue(CursorColumnProperty); }
            private set { SetValue(CursorColumnProperty, value); }
        }

        /// <summary>
        /// Gets the currentl visible tool window.
        /// </summary>
        private static readonly ModelProperty ToolWindowProperty = ModelProperty.Register(typeof(CodeEditorViewModel), "ToolWindow", typeof(ToolWindowViewModel), null);
        public ToolWindowViewModel ToolWindow
        {
            get { return (ToolWindowViewModel)GetValue(ToolWindowProperty); }
            private set { SetValue(ToolWindowProperty, value); }
        }

        public static readonly ModelProperty IsToolWindowVisibleProperty = ModelProperty.Register(typeof(CodeEditorViewModel), "IsToolWindowVisible", typeof(bool), false);
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool IsToolWindowVisible
        {
            get { return (bool)GetValue(IsToolWindowVisibleProperty); }
            private set { SetValue(IsToolWindowVisibleProperty, value); }
        }

        protected void ShowToolWindow(ToolWindowViewModel toolWindow)
        {
            if (ToolWindow != null)
            {
                IsToolWindowVisible = false;
                _timerService.Schedule(() => {
                    ToolWindow = null;
                    ShowToolWindow(toolWindow);
                }, TimeSpan.FromMilliseconds(200));
            }

            ToolWindow = toolWindow;
            IsToolWindowVisible = (toolWindow != null);
        }

        internal void CloseToolWindow()
        {
            ShowToolWindow(null);
        }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="CursorLine"/>
        /// </summary>
        public static readonly ModelProperty CursorLineProperty = ModelProperty.Register(typeof(CodeEditorViewModel), "CursorLine", typeof(int), 1);

        /// <summary>
        /// Gets the line where the cursor is currently located.
        /// </summary>
        public int CursorLine
        {
            get { return (int)GetValue(CursorLineProperty); }
            private set { SetValue(CursorLineProperty, value); }
        }

        /// <summary>
        /// Builds a string containing all of the text in the editor.
        /// </summary>
        public string GetContent()
        {
            var builder = new StringBuilder();
            foreach (var line in _lines)
            {
                builder.Append(line.Text);
                builder.Append('\n');
            }

            return builder.ToString();
        }

        /// <summary>
        /// Sets the text for the editor.
        /// </summary>
        public void SetContent(string value)
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

        /// <summary>
        /// Called after <see cref="SetContent"/> has updated the <see cref="Lines"/>.
        /// </summary>
        /// <param name="newValue">The value passed to <see cref="SetContent"/>.</param>
        protected virtual void OnContentChanged(string newValue)
        {

        }

        private void WaitForTyping()
        {
            _timerService.WaitForTyping(() =>
            {
                EndTypingUndo();
                Refresh();
            });
        }

        /// <summary>
        /// Commits and pending changes to the editor text.
        /// </summary>
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
                line.CommitPending();
        }

        /// <summary>
        /// Raised whenever the text of a line changes.
        /// </summary>
        public EventHandler<LineEventArgs> LineChanged;
        internal void RaiseLineChanged(LineEventArgs e)
        {
            OnLineChanged(e);
        }

        /// <summary>
        /// Raises the <see cref="E:LineChanged" /> event.
        /// </summary>
        /// <param name="e">Information about which line changed.</param>
        protected virtual void OnLineChanged(LineEventArgs e)
        {
            if (LineChanged != null)
                LineChanged(this, e);
        }

        /// <summary>
        /// Raised to format the text of a line.
        /// </summary>
        public EventHandler<LineFormatEventArgs> FormatLine;
        internal void RaiseFormatLine(LineFormatEventArgs e)
        {
            OnFormatLine(e);
        }

        /// <summary>
        /// Raises the <see cref="E:FormatLine" /> event.
        /// </summary>
        /// <param name="e">Information about the line that needs to be formatted.</param>
        protected virtual void OnFormatLine(LineFormatEventArgs e)
        {
            if (FormatLine != null)
                FormatLine(this, e);
        }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="LineCount"/>
        /// </summary>
        public static readonly ModelProperty LineCountProperty = ModelProperty.Register(typeof(CodeEditorViewModel), "LineCount", typeof(int), 1);

        /// <summary>
        /// Gets the number of lines in the editor.
        /// </summary>
        public int LineCount
        {
            get { return (int)GetValue(LineCountProperty); }
            private set { SetValue(LineCountProperty, value); }
        }

        /// <summary>
        /// Gets the individual lines.
        /// </summary>
        public ReadOnlyObservableCollection<LineViewModel> Lines
        {
            get { return _linesWrapper; }
        }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ReadOnlyObservableCollection<LineViewModel> _linesWrapper;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ObservableCollection<LineViewModel> _lines;

        /// <summary>
        /// Gets an object containing the settings for the editor.
        /// </summary>
        public EditorProperties Style { get; private set; }

        /// <summary>
        /// Gets an object containing the resources for the editor.
        /// </summary>
        /// <remarks>
        /// Constructed from the <see cref="Style"/> object. Cannot be directly modified.
        /// </remarks>
        public EditorResources Resources { get; private set; }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="LineNumberColumnWidth"/>
        /// </summary>
        public static readonly ModelProperty LineNumberColumnWidthProperty =
            ModelProperty.RegisterDependant(typeof(CodeEditorViewModel), "LineNumberColumnWidth", typeof(double),
                new[] { LineCountProperty }, GetLineNumberColumnWidth);

        /// <summary>
        /// Gets the width of the line number column (for UI binding only).
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public double LineNumberColumnWidth
        {
            get { return (double)GetValue(LineNumberColumnWidthProperty); }
        }
        private static object GetLineNumberColumnWidth(ModelBase model)
        {
            var viewModel = (CodeEditorViewModel)model;
            double characterWidth = viewModel.Resources.CharacterWidth;

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

        /// <summary>
        /// Raises the <see cref="E:KeyPressed" /> event.
        /// </summary>
        /// <param name="e">Information about which key was pressed.</param>
        protected virtual void OnKeyPressed(KeyPressedEventArgs e)
        {
            var moveCursorFlags = ((e.Modifiers & ModifierKeys.Shift) != 0) ? MoveCursorFlags.Highlighting : MoveCursorFlags.None;

            switch (e.Key)
            {
                case Key.Up:
                    MoveCursorTo(CursorLine - 1, CursorColumn, moveCursorFlags | MoveCursorFlags.RememberColumn);
                    e.Handled = true;
                    break;

                case Key.Down:
                    MoveCursorTo(CursorLine + 1, CursorColumn, moveCursorFlags | MoveCursorFlags.RememberColumn);
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
                    HandleTab((e.Modifiers & ModifierKeys.Shift) != 0);
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

                case Key.C:
                    if ((e.Modifiers & ModifierKeys.Control) != 0)
                    {
                        CopySelection();
                        e.Handled = true;
                    }
                    else
                    {
                        goto default;
                    }
                    break;

                case Key.X:
                    if ((e.Modifiers & ModifierKeys.Control) != 0)
                    {
                        CutSelection();
                        e.Handled = true;
                    }
                    else
                    {
                        goto default;
                    }
                    break;

                case Key.V:
                    if ((e.Modifiers & ModifierKeys.Control) != 0)
                    {
                        HandlePaste();
                        e.Handled = true;
                    }
                    else
                    {
                        goto default;
                    }
                    break;

                case Key.Z:
                    if ((e.Modifiers & ModifierKeys.Control) != 0)
                    {
                        HandleUndo();
                        e.Handled = true;
                    }
                    else
                    {
                        goto default;
                    }
                    break;

                case Key.Y:
                    if ((e.Modifiers & ModifierKeys.Control) != 0)
                    {
                        HandleRedo();
                        e.Handled = true;
                    }
                    else
                    {
                        goto default;
                    }
                    break;

                case Key.G:
                    if ((e.Modifiers & ModifierKeys.Control) != 0)
                    {
                        HandleGotoLine();
                        e.Handled = true;
                    }
                    else
                    {
                        goto default;
                    }
                    break;

                default:
                    if ((e.Modifiers & ModifierKeys.Control) == 0)
                    {
                        char c = e.GetChar();
                        if (c != '\0')
                        {
                            HandleCharacter(c);
                            e.Handled = true;
                        }
                    }
                    break;
            }
        }

        private UndoItem BeginTypingUndo()
        {
            var undoItem = _undoStack.Peek();
            if (undoItem.Before == null || undoItem.After.Text != null)
            {
                BeginUndo();
                undoItem = _undoStack.Pop();

                if (HasSelection())
                    RemoveSelection();

                var line = CursorLine;
                var column = CursorColumn;
                undoItem.After = new Selection
                {
                    StartLine = line, StartColumn = column,
                    EndLine = line, EndColumn = column
                };

                _undoStack.Push(undoItem);
            }

            return undoItem;
        }

        private void EndTypingUndo()
        {
            var undoItem = _undoStack.Peek();
            if (undoItem.After != null && undoItem.After.Text == null)
            {
                undoItem.After.EndLine = CursorLine;
                undoItem.After.EndColumn = CursorColumn;
                undoItem.After.Text = GetText(undoItem.After);

                var beforeOrdered = undoItem.Before.GetOrderedSelection();
                if (!ReferenceEquals(beforeOrdered, undoItem.Before))
                {
                    undoItem.Before.StartLine = beforeOrdered.StartLine;
                    undoItem.Before.StartColumn = beforeOrdered.StartColumn;
                    undoItem.Before.EndLine = beforeOrdered.EndLine;
                    undoItem.Before.EndColumn = beforeOrdered.EndColumn;
                }
            }
        }

        internal void HandleCharacter(char c)
        {
            var undoItem = BeginTypingUndo();
            if (undoItem.Before.IsEndBeforeStart())
            {
                EndTypingUndo();
                undoItem = BeginTypingUndo();
            }

            var column = CursorColumn;
            var line = CursorLine;
            var lineViewModel = _lines[line - 1];
            lineViewModel.Insert(column, c.ToString());
            MoveCursorTo(line, column + 1, MoveCursorFlags.None);

            undoItem.After.EndColumn++;

            WaitForTyping();
        }

        private void HandleDelete()
        {
            if (HasSelection())
            {
                DeleteSelection();
            }
            else
            {
                var undoItem = BeginTypingUndo();
                if (undoItem.Before.IsEndBeforeStart())
                {
                    EndTypingUndo();
                    undoItem = BeginTypingUndo();
                }

                var column = CursorColumn;
                var line = CursorLine;
                var lineViewModel = _lines[line - 1];
                if (column <= lineViewModel.LineLength)
                {
                    var text = lineViewModel.PendingText ?? lineViewModel.Text;
                    undoItem.Before.EndColumn++;
                    undoItem.Before.Text += text[column - 1];

                    lineViewModel.Remove(column, column);
                    WaitForTyping();
                }
                else if (line < LineCount)
                {
                    undoItem.Before.EndLine++;
                    undoItem.Before.EndColumn = 1;
                    undoItem.Before.Text += '\n';

                    MergeNextLine();
                }
            }
        }

        private void HandleBackspace()
        {
            if (HasSelection())
            {
                DeleteSelection();
            }
            else
            {
                var undoItem = BeginTypingUndo();
                if (undoItem.Before.IsStartBeforeEnd())
                {
                    EndTypingUndo();
                    undoItem = BeginTypingUndo();
                }

                var column = CursorColumn;
                var line = CursorLine;

                if (column > 1)
                {
                    column--;

                    var lineViewModel = _lines[line - 1];
                    var text = lineViewModel.PendingText ?? lineViewModel.Text;
                    undoItem.Before.EndColumn--;
                    undoItem.Before.Text = text[column - 1] + undoItem.Before.Text;
                    undoItem.After.StartColumn--;
                    undoItem.After.EndColumn--;

                    lineViewModel.Remove(column, column);
                    MoveCursorTo(line, column, MoveCursorFlags.None);

                    WaitForTyping();
                }
                else if (line > 1)
                {
                    line--;
                    column = _lines[line - 1].LineLength + 1;

                    undoItem.Before.EndLine--;
                    undoItem.Before.EndColumn = column;
                    undoItem.Before.Text = '\n' + undoItem.Before.Text;
                    undoItem.After.StartLine = undoItem.After.EndLine = line;
                    undoItem.After.StartColumn = undoItem.After.EndColumn = column;

                    MoveCursorTo(line, column, MoveCursorFlags.None);
                    MergeNextLine();
                }
            }
        }

        /// <summary>
        /// Selects all of the text in the editor.
        /// </summary>
        public void SelectAll()
        {
            MoveCursorTo(LineCount, Int32.MaxValue, MoveCursorFlags.None);
            MoveCursorTo(1, 1, MoveCursorFlags.Highlighting);
        }

        private bool HasSelection()
        {
            if (_selectionStartLine == 0)
                return false;

            return (_selectionStartLine != _selectionEndLine || _selectionStartColumn != _selectionEndColumn);
        }

        /// <summary>
        /// Builds a string containing the text selected in the editor.
        /// </summary>
        public string GetSelectedText()
        {
            if (!HasSelection())
                return String.Empty;

            var selection = GetOrderedSelection();
            return GetText(selection);
        }

        private string GetText(Selection selection)
        {
            if (selection.Text == null)
            {
                var builder = new StringBuilder();
                if (selection.StartColumn != selection.EndColumn || selection.StartLine != selection.EndLine)
                {
                    var orderedSelection = selection.GetOrderedSelection();
                    for (int i = orderedSelection.StartLine; i <= orderedSelection.EndLine; ++i)
                    {
                        if (i != orderedSelection.StartLine)
                            builder.Append('\n');

                        var line = _lines[i - 1];
                        var text = line.PendingText ?? line.Text;

                        var firstChar = (i == orderedSelection.StartLine) ? orderedSelection.StartColumn - 1 : 0;
                        var lastChar = (i == orderedSelection.EndLine) ? orderedSelection.EndColumn - 1 : text.Length;
                        builder.Append(text, firstChar, lastChar - firstChar);
                    }
                }

                selection.Text = builder.ToString();
            }
            return selection.Text;
        }

        /// <summary>
        /// Removes the text in the current selection.
        /// </summary>
        internal void DeleteSelection()
        {
            if (!HasSelection())
                return;

            BeginUndo();
            RemoveSelection();
            EndUndo(String.Empty);

            Refresh();
        }

        private void RemoveSelection()
        {
            var selection = GetOrderedSelection();

            var line = _lines[selection.StartLine - 1];
            line.Remove(line.SelectionStart, line.SelectionEnd);

            if (selection.StartLine != selection.EndLine)
            {
                var lastLine = _lines[selection.EndLine - 1];
                if (lastLine.SelectionEnd < lastLine.LineLength)
                    line.Insert(line.LineLength + 1, lastLine.Text.Substring(lastLine.SelectionEnd, lastLine.LineLength - lastLine.SelectionEnd));

                for (int i = selection.EndLine - 1; i >= selection.StartLine; --i)
                    _lines.RemoveAt(i);

                var linesRemoved = (selection.EndLine - selection.StartLine);
                LineCount -= linesRemoved;

                for (int i = selection.StartLine; i < _lines.Count; ++i)
                    _lines[i].Line -= linesRemoved;
            }

            MoveCursorTo(_selectionStartLine, _selectionStartColumn, MoveCursorFlags.None);

            _selectionStartColumn = _selectionEndColumn = _selectionStartLine = _selectionEndLine = 0;
        }

        /// <summary>
        /// Replaces the current selection with new text.
        /// </summary>
        /// <param name="newText">The new text.</param>
        internal void ReplaceSelection(string newText)
        {
            BeginUndo();

            var selection = GetSelection();
            ReplaceText(selection, newText);

            EndUndo(newText);

            var item = _undoStack.Pop();
            item.After.StartLine = selection.StartLine;
            item.After.StartColumn = selection.StartColumn;
            _undoStack.Push(item);

            Refresh();
        }

        /// <summary>
        /// Replaces a selection with new text.
        /// </summary>
        /// <param name="selection">The selection.</param>
        /// <param name="newText">The new text.</param>
        /// <remarks>Should not update the undo buffer, used by <see cref="HandleUndo"/> and <see cref="HandleRedo"/>.</remarks>
        private void ReplaceText(Selection selection, string newText)
        {
            selection = selection.GetOrderedSelection();

            LineViewModel line;
            var linesAdded = 0;
            if (selection.StartLine == selection.EndLine)
            {
                line = _lines[selection.StartLine - 1];
                if (selection.StartColumn < selection.EndColumn)
                    line.Remove(selection.StartColumn, selection.EndColumn - 1);

                if (!newText.Contains("\n"))
                {
                    line.Insert(selection.StartColumn, newText);

                    MoveCursorTo(selection.StartLine, selection.StartColumn + newText.Length, MoveCursorFlags.None);
                    return;
                }

                var remaining = (line.PendingText ?? line.Text).Substring(selection.StartColumn - 1);
                if (selection.StartColumn <= line.LineLength)
                    line.Remove(selection.StartColumn, line.LineLength);

                line = new LineViewModel(this, line.Line + 1) { Text = remaining };
                _lines.Insert(selection.StartLine, line);
                ++selection.EndLine;
                linesAdded = 1;
            }
            else
            {
                line = _lines[selection.StartLine - 1];
                if (selection.StartColumn < line.LineLength)
                    line.Remove(selection.StartColumn, line.LineLength);

                line = _lines[selection.EndLine - 1];
                if (selection.EndColumn > 1)
                    line.Remove(1, selection.EndColumn - 1);

                for (int i = selection.EndLine - 2; i >= selection.StartLine; --i)
                {
                    _lines.RemoveAt(i);
                    linesAdded--;
                }

                line.Line += linesAdded;
            }

            var newTextLines = newText.Split('\n');
            line = _lines[selection.StartLine - 1];
            line.Insert(selection.StartColumn, newTextLines[0].TrimEnd('\r'));

            if (newTextLines.Length == 1)
            {
                selection.EndColumn = line.LineLength + 1;

                var startLine = _lines[selection.StartLine];
                line.Insert(line.LineLength + 1, startLine.PendingText ?? startLine.Text);

                _lines.RemoveAt(selection.StartLine);
                linesAdded--;
            }
            else
            {
                line = _lines[selection.StartLine];
                var text = newTextLines[newTextLines.Length - 1].TrimEnd('\r');
                line.Insert(1, text);
                line.Line += newTextLines.Length - 2;

                selection.EndColumn = text.Length + 1;
            }

            for (int i = 1; i < newTextLines.Length - 1; i++)
            {
                line = new LineViewModel(this, selection.StartLine + i) { Text = newTextLines[i].TrimEnd('\r') };
                _lines.Insert(selection.StartLine + i - 1, line);
                linesAdded++;
            }

            selection.EndLine = selection.StartLine + newTextLines.Length - 1;

            if (linesAdded != 0)
            {
                for (int i = selection.EndLine; i < _lines.Count; ++i)
                    _lines[i].Line += linesAdded;
            }

            LineCount += linesAdded;

            MoveCursorTo(selection.EndLine, selection.EndColumn, MoveCursorFlags.None);
        }

        /// <summary>
        /// Selected the word at the specified location.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <param name="column">The column.</param>
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

        private void HandleTab(bool isShift)
        {
            if (HasSelection())
            {
                var selection = GetOrderedSelection();
                if (isShift)
                {
                    Indent(selection, false);
                    return;
                }

                if (selection.StartLine != selection.EndLine)
                {
                    Indent(selection, true);
                    return;
                }
                if (selection.StartLine == selection.EndLine)
                {
                    var line = _lines[selection.StartLine - 1];
                    if (selection.StartColumn == 1 && selection.EndColumn == line.LineLength + 1)
                    {
                        Indent(selection, true);
                        return;
                    }
                }

                DeleteSelection();
            }

            if (isShift)
                return;

            var cursorLine = CursorLine;
            var cursorColumn = CursorColumn;
            int newColumn = (((cursorColumn - 1) / 4) + 1) * 4 + 1;
            _lines[cursorLine - 1].Insert(cursorColumn, new string(' ', newColumn - cursorColumn));
            MoveCursorTo(cursorLine, newColumn, MoveCursorFlags.None);
            CursorColumn = newColumn;
        }

        private void Indent(Selection orderedSelection, bool isIndent)
        {
            var endLine = orderedSelection.EndLine;
            if (orderedSelection.EndColumn == 1)
                endLine--;

            MoveCursorTo(orderedSelection.StartLine, 1, MoveCursorFlags.None);
            MoveCursorTo(endLine + 1, 1, MoveCursorFlags.Highlighting);

            BeginUndo();

            for (int i = orderedSelection.StartLine; i <= endLine; i++)
            {
                var line = _lines[i - 1];
                var text = line.PendingText ?? line.Text;
                if (isIndent)
                {
                    if (!text.All(c => Char.IsWhiteSpace(c)))
                        line.Insert(1, "    ");
                }
                else
                {
                    if (text.StartsWith("    "))
                        line.Remove(1, 4);
                    else if (text.StartsWith("   "))
                        line.Remove(1, 3);
                    else if (text.StartsWith("  "))
                        line.Remove(1, 2);
                    else if (text.StartsWith(" "))
                        line.Remove(1, 1);
                }
            }

            MoveCursorTo(orderedSelection.StartLine, 1, MoveCursorFlags.None);
            MoveCursorTo(endLine + 1, 1, MoveCursorFlags.Highlighting);

            EndUndo(GetSelectedText());

            Refresh();
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
            WaitForTyping();
        }

        private void HandleEnter()
        {
            var undoItem = BeginTypingUndo();
            if (undoItem.Before.IsEndBeforeStart())
            {
                EndTypingUndo();
                undoItem = BeginTypingUndo();
            }

            // split the current line at the cursor
            var cursorLine = CursorLine;
            var cursorLineViewModel = _lines[cursorLine - 1];
            string text = cursorLineViewModel.PendingText ?? cursorLineViewModel.Text;
            var cursorColumn = CursorColumn - 1; // string index is 0-based
            string left = (cursorColumn > 0) ? text.Substring(0, cursorColumn) : String.Empty;
            string right = (cursorColumn < text.Length) ? text.Substring(cursorColumn) : String.Empty;

            // truncate the first line
            if (right.Length > 0)
                cursorLineViewModel.Remove(cursorColumn + 1, text.Length);

            // add a new line
            var newLineViewModel = new LineViewModel(this, cursorLine + 1) { PendingText = right };
            _lines.Insert(cursorLine, newLineViewModel);
            LineCount++;

            // create TextPieces for the new line so it appears
            var e = new LineFormatEventArgs(newLineViewModel);
            newLineViewModel.SetValue(LineViewModel.TextPiecesProperty, e.BuildTextPieces());

            // update the cursor position
            MoveCursorTo(cursorLine + 1, 1, MoveCursorFlags.None);

            // update the line numbers
            for (int i = CursorLine; i < _lines.Count; i++)
                _lines[i].Line++;

            // update undo item
            undoItem.After.Text += "\n";
            undoItem.After.EndLine++;
            undoItem.After.EndColumn = 1;

            // schedule a refresh to update the syntax highlighting
            WaitForTyping();
        }

        /// <summary>
        /// Behavioral flags to pass to the <see cref="MoveCursorTo" method. />
        /// </summary>
        public enum MoveCursorFlags
        {
            /// <summary>
            /// No special behavior.
            /// </summary>
            None = 0,

            /// <summary>
            /// Update the selected region while moving the cursor.
            /// </summary>
            Highlighting = 0x01,

            /// <summary>
            /// Remember (or restore) the column value when changing lines.
            /// </summary>
            RememberColumn = 0x02,
        }

        /// <summary>
        /// Moves the cursor to the specified location.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <param name="column">The column.</param>
        /// <param name="flags">Additional logic to perform while moving the cursor.</param>
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
            if (column < 1)
                column = 1;

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
            else 
            {
                // update highlighted region
                if (_selectionEndLine != 0)
                {
                    for (int i = _selectionEndLine + 1; i < line; i++)
                        _lines[i - 1].ClearSelection();
                    for (int i = line + 1; i <= _selectionEndLine; i++)
                        _lines[i - 1].ClearSelection();
                }

                _selectionEndLine = line;
                _selectionEndColumn = column;

                if (_selectionStartLine == _selectionEndLine)
                {
                    if (_selectionStartColumn < _selectionEndColumn)
                        _lines[_selectionStartLine - 1].Select(_selectionStartColumn, _selectionEndColumn - 1);
                    else
                        _lines[_selectionStartLine - 1].Select(_selectionEndColumn, _selectionStartColumn - 1);
                }
                else
                {
                    var selection = GetOrderedSelection();

                    _lines[selection.StartLine - 1].Select(selection.StartColumn, _lines[selection.StartLine - 1].LineLength);
                    for (int i = selection.StartLine; i < selection.EndLine - 1; i++)
                        _lines[i].Select(1, _lines[i].LineLength);
                    _lines[selection.EndLine - 1].Select(1, selection.EndColumn - 1);
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

        private void CopySelection()
        {
            _clipboardService.SetData(GetSelectedText());
        }

        private void CutSelection()
        {
            _clipboardService.SetData(GetSelectedText());
            DeleteSelection();
        }

        private void HandlePaste()
        {
            var text = _clipboardService.GetText();            
            ReplaceSelection(text);
        }

        [DebuggerDisplay("{StartLine}:{StartColumn}-{EndLine}:{EndColumn} {Text}")]
        private class Selection
        {
            public int StartLine { get; set; }
            public int StartColumn { get; set; }
            public int EndLine { get; set; }
            public int EndColumn { get; set; }
            public string Text { get; set; }

            public bool IsStartBeforeEnd()
            {
                if (StartLine < EndLine)
                    return true;
                if (StartLine > EndLine)
                    return false;
                return (StartColumn < EndColumn);
            }

            public bool IsEndBeforeStart()
            {
                if (EndLine < StartLine)
                    return true;
                if (EndLine > StartLine)
                    return false;
                return (EndColumn < StartColumn);
            }

            public Selection GetOrderedSelection()
            {
                if (StartLine < EndLine)
                    return this;

                if (StartLine == EndLine && StartColumn < EndColumn)
                    return this;

                return new Selection
                {
                    StartLine = EndLine,
                    StartColumn = EndColumn,
                    EndLine = StartLine,
                    EndColumn = StartColumn,
                };
            }
        }

        private struct UndoItem
        {
            public Selection Before { get; set; }
            public Selection After { get; set; }

            public override string ToString()
            {
                var builder = new StringBuilder();
                builder.Append('"');
                if (Before != null)
                    builder.Append(Before.Text);
                builder.Append("\" => \"");
                if (Before != null)
                    builder.Append(After.Text);
                builder.Append('"');
                return builder.ToString();
            }
        }

        private Selection GetSelection()
        {
            if (HasSelection())
            {
                return new Selection
                {
                    StartLine = _selectionStartLine,
                    StartColumn = _selectionStartColumn,
                    EndLine = _selectionEndLine,
                    EndColumn = _selectionEndColumn,
                };
            }
            else
            {
                var cursorLine = CursorLine;
                var cursorColumn = CursorColumn;
                return new Selection
                {
                    StartLine = cursorLine,
                    StartColumn = cursorColumn,
                    EndLine = cursorLine,
                    EndColumn = cursorColumn
                };
            }
        }

        private Selection GetOrderedSelection()
        {
            return GetSelection().GetOrderedSelection();
        }

        private void BeginUndo()
        {
            _redoStack.Clear();

            var item = new UndoItem();
            item.Before = GetSelection();
            item.Before.Text = GetText(item.Before);
            _undoStack.Push(item);
        }

        private void EndUndo(string text)
        {
            var item = _undoStack.Pop();
            item.After = GetSelection();
            item.After.Text = text;
            _undoStack.Push(item);
        }

        private void HandleUndo()
        {
            if (_undoStack.IsEmpty)
                return;

            var item = _undoStack.Pop();
            _redoStack.Push(item);

            MoveCursorTo(item.After.StartLine, item.After.StartColumn, MoveCursorFlags.None);
            ReplaceText(item.After, item.Before.Text);

            Debug.Assert(CursorLine == item.Before.EndLine);
            Debug.Assert(CursorColumn == item.Before.EndColumn);

            Refresh();
        }

        private void HandleRedo()
        {
            if (_redoStack.Count == 0)
                return;

            var item = _redoStack.Pop();
            _undoStack.Push(item);

            MoveCursorTo(item.Before.StartLine, item.Before.StartColumn, MoveCursorFlags.None);
            ReplaceText(item.Before, item.After.Text);

            Debug.Assert(CursorLine == item.After.EndLine);
            Debug.Assert(CursorColumn == item.After.EndColumn);

            Refresh();
        }

        private void HandleGotoLine()
        {
            bool isVisible = true;

            var toolWindow = ToolWindow as GotoLineToolWindowViewModel;
            if (toolWindow == null)
            {
                toolWindow = new GotoLineToolWindowViewModel(this);
                isVisible = false;
            }

            toolWindow.LineNumber.Value = CursorLine;
            toolWindow.ShouldFocusLineNumber = true;

            if (!isVisible)
                ShowToolWindow(toolWindow);
        }
    }
}
