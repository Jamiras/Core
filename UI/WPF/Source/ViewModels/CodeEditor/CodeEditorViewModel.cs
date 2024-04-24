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
                   ServiceRepository.Instance.FindService<ITimerService>(),
                   ServiceRepository.Instance.FindService<IBackgroundWorkerService>())
        {
        }

        internal CodeEditorViewModel(IClipboardService clipboardService, ITimerService timerService, IBackgroundWorkerService backgroundWorkerService)
        {
            _clipboardService = clipboardService;
            _timerService = timerService;
            _backgroundWorkerService = backgroundWorkerService;

            _lines = new ObservableCollection<LineViewModel>();
            _linesWrapper = new ReadOnlyObservableCollection<LineViewModel>(_lines);

            _undoStack = new FixedSizeStack<UndoItem>(128);
            _redoStack = new Stack<UndoItem>(32);

            _braces = new TinyDictionary<char, char>();
            _braceStack = new Stack<char>();

            Style = new EditorProperties();
            Resources = new EditorResources(Style);

            _findWindow = new FindToolWindowViewModel(this);

            GotoLineCommand = new DelegateCommand(HandleGotoLine);
            FindCommand = new DelegateCommand(HandleFind);
            ReplaceCommand = new DelegateCommand(HandleReplace);
            UndoCommand = new DelegateCommand(HandleUndo);
            RedoCommand = new DelegateCommand(HandleRedo);
            CutCommand = new DelegateCommand(CutSelection);
            CopyCommand = new DelegateCommand(CopySelection);
            PasteCommand = new DelegateCommand(HandlePaste);
        }

        private readonly ITimerService _timerService;
        private readonly IClipboardService _clipboardService;
        private readonly IBackgroundWorkerService _backgroundWorkerService;

        private int _version;

        private int _selectionStartLine, _selectionStartColumn, _selectionEndLine, _selectionEndColumn;

        private FixedSizeStack<UndoItem> _undoStack;
        private Stack<UndoItem> _redoStack;

        private FindToolWindowViewModel _findWindow;

        /// <summary>
        /// Gets the mapping of opening braces to closing braces.
        /// </summary>
        /// <remarks>
        /// Must be populated by subclass to enable brace matching.
        /// </remarks>
        protected IDictionary<char, char> Braces
        {
            get { return _braces; }
        }
        private TinyDictionary<char, char> _braces;
        private Stack<char> _braceStack;

        // remebers the cursor column when moving up or down even if the line doesn't have that many columns
        private int? _virtualCursorColumn;

        /// <summary>
        /// Gets the command to open the goto line tool window.
        /// </summary>
        public CommandBase GotoLineCommand { get; private set; }

        /// <summary>
        /// Gets the command to open the find tool window.
        /// </summary>
        public CommandBase FindCommand { get; private set; }

        /// <summary>
        /// Gets the command to open the replace tool window.
        /// </summary>
        public CommandBase ReplaceCommand { get; private set; }

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
        /// <see cref="ModelProperty"/> for <see cref="IsFocusRequested"/>
        /// </summary>
        public static readonly ModelProperty IsFocusRequestedProperty = ModelProperty.Register(typeof(CodeEditorViewModel), "IsFocusRequested", typeof(bool), false);

        /// <summary>
        /// Gets or sets whether the code editor should be focused (used by tool windows to move the cursor back to the editor).
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool IsFocusRequested
        {
            get { return (bool)GetValue(IsFocusRequestedProperty); }
            set { SetValue(IsFocusRequestedProperty, value); }
        }

        private static readonly ModelProperty ToolWindowProperty = ModelProperty.Register(typeof(CodeEditorViewModel), "ToolWindow", typeof(ToolWindowViewModel), null);
        /// <summary>
        /// Gets the currently visible tool window.
        /// </summary>
        public ToolWindowViewModel ToolWindow
        {
            get { return (ToolWindowViewModel)GetValue(ToolWindowProperty); }
            private set { SetValue(ToolWindowProperty, value); }
        }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="IsToolWindowVisible"/>
        /// </summary>
        public static readonly ModelProperty IsToolWindowVisibleProperty = ModelProperty.Register(typeof(CodeEditorViewModel), "IsToolWindowVisible", typeof(bool), false);

        /// <summary>
        /// Gets a value indicating whether the tool window is visible.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool IsToolWindowVisible
        {
            get { return (bool)GetValue(IsToolWindowVisibleProperty); }
            private set { SetValue(IsToolWindowVisibleProperty, value); }
        }

        /// <summary>
        /// Shows the specified tool window.
        /// </summary>
        protected void ShowToolWindow(ToolWindowViewModel toolWindow)
        {
            if (toolWindow != null)
            {
                ToolWindow = toolWindow;
                IsToolWindowVisible = true;
            }
            else
            {
                IsToolWindowVisible = false;
                _timerService.Schedule(() =>
                {
                    if (!IsToolWindowVisible)
                        ToolWindow = null;
                }, TimeSpan.FromMilliseconds(300));
            }
        }

        /// <summary>
        /// Closes the current tool window.
        /// </summary>
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

            var enumerator = _lines.GetEnumerator();
            if (enumerator.MoveNext())
            {
                builder.Append(enumerator.Current.Text);
                while (enumerator.MoveNext())
                {
                    builder.AppendLine();
                    builder.Append(enumerator.Current.Text);
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Sets the text for the editor.
        /// </summary>
        public void SetContent(string value)
        {
            _lines.Clear();

            while (_undoStack.Count > 0)
                _undoStack.Pop();

            while (_redoStack.Count > 0)
                _redoStack.Pop();

            int lineIndex = 1;
            var tokenizer = Tokenizer.CreateTokenizer(value);
            char lineEnd;
            do
            {
                var line = tokenizer.ReadTo('\n');
                if (line.Length > 0 && line[line.Length - 1] == '\r')
                    line = line.SubToken(0, line.Length - 1);
                lineEnd = tokenizer.NextChar;
                tokenizer.Advance();

                var lineViewModel = new LineViewModel(this, lineIndex) { Text = line.ToString() };
                _lines.Add(lineViewModel);
                lineIndex++;
            } while (tokenizer.NextChar != '\0');

            if (lineEnd == '\n')
                _lines.Add(new LineViewModel(this, lineIndex));

            LineCount = _lines.Count;
            CursorLine = 1;
            CursorColumn = 1;

            OnUpdateSyntax(new ContentChangedEventArgs(value, _version, this, ContentChangeType.Refresh, null, false));
        }

        /// <summary>
        /// The type of change that occurred.
        /// </summary>
        protected enum ContentChangeType
        {
            /// <summary>
            /// No change occurred.
            /// </summary>
            None = 0,

            /// <summary>
            /// The entire content was updated.
            /// </summary>
            Refresh,

            /// <summary>
            /// The specified lines changed.
            /// </summary>
            Update,
        }

        /// <summary>
        /// Information about changes to the editor content.
        /// </summary>
        protected class ContentChangedEventArgs
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ContentChangedEventArgs"/> class.
            /// </summary>
            /// <param name="content">The new content of the editor.</param>
            /// <param name="version">An internal counter indicating the revision.</param>
            /// <param name="editor">A reference to the editor.</param>
            /// <param name="type">The type of change that occurred.</param>
            /// <param name="affectedLines">The lines that were updated.</param>
            /// <param name="isWhitespaceOnly">Flag indicating that the changes did not affect syntax.</param>
            public ContentChangedEventArgs(string content, int version, CodeEditorViewModel editor, 
                ContentChangeType type, IEnumerable<int> affectedLines, bool isWhitespaceOnly)
            {
                Content = content;
                _version = version;
                _editor = editor;
                Type = type;
                AffectedLines = affectedLines;
                IsWhitespaceOnlyChange = isWhitespaceOnly;
            }

            private readonly int _version;
            private readonly CodeEditorViewModel _editor;

            /// <summary>
            /// Gets the new content of the editor.
            /// </summary>
            public string Content { get; private set; }

            /// <summary>
            /// Gets the type of change.
            /// </summary>
            public ContentChangeType Type { get; private set; }

            /// <summary>
            /// Gets the lines that were updated.
            /// </summary>
            public IEnumerable<int> AffectedLines { get; private set; }

            /// <summary>
            /// Gets whether or not the change was only whitespace.
            /// </summary>    
            public bool IsWhitespaceOnlyChange { get; private set; }

            /// <summary>
            /// Gets a value indicating whether the content has been changed again and the current processing should be aborted.
            /// </summary>
            public bool IsAborted
            {
                get
                {
                    lock (_editor._lines)
                    {
                        return (_editor._version != _version);
                    }
                }
            }
        }

        /// <summary>
        /// Called shortly after text is loaded or changes to update the syntax highlighting.
        /// </summary>
        protected virtual void OnUpdateSyntax(ContentChangedEventArgs e)
        {

        }

        /// <summary>
        /// Called shortly after text changes for processing that should not occur when text is initially loaded.
        /// </summary>
        protected virtual void OnContentChanged(ContentChangedEventArgs e)
        {

        }

        private void WaitForTyping()
        {
            _timerService.Reschedule(() =>
            {
                EndTypingUndo();
                Refresh();
            }, TimeSpan.FromMilliseconds(300));
        }

        /// <summary>
        /// Commits and pending changes to the editor text.
        /// </summary>
        public override void Refresh()
        {
            var updatedLines = new List<LineViewModel>();
            if (!PerformUpdate(updatedLines))
            {
                // more changes occurred while updating, quickly repaint only the affected lines for this change, then let the new change do the full update
                foreach (var line in updatedLines)
                    line.Refresh();
            }
        }

        private bool PerformUpdate(List<LineViewModel> updatedLines)
        {
            int version;
            lock (_lines)
            {
                // capture a version, if it changes while we're processing, we'll abort and let the new version proceed
                version = ++_version;
            }

            var allLines = new List<string>(_lines.Count);
            int newContentLength = _lines.Count * 2; // crlf for each line
            for (int i = 0; i < _lines.Count; i++)
            {
                var line = _lines[i];
                string pendingText;

                lock (line._lockObject)
                {
                    pendingText = line.PendingText;
                    if (pendingText != null)
                        updatedLines.Add(line);
                    else
                        pendingText = line.Text;
                }

                allLines.Add(pendingText);
                newContentLength += pendingText.Length;

                if ((i & 127) == 127)
                {
                    // every 128 lines, check to see if more changes have been made
                    lock (_lines)
                    {
                        if (_version != version)
                            return false;
                    }
                }
            }

            // see if changes have been made
            lock (_lines)
            {
                if (_version != version)
                    return false;
            }

            var newContent = new StringBuilder(newContentLength);
            foreach (var line in allLines)
                newContent.AppendLine(line);

            var newContentString = newContent.ToString();
            var updatedLineNumbers = updatedLines.Select(l => l.Line).ToArray();

            bool isWhitespaceOnly = true;
            lock (_lines)
            {
                // final check to see if more changes have been made before committing the current data and raising the events
                if (_version != version)
                    return false;

                // commit the changes
                foreach (var line in updatedLines)
                    line.CommitPending(ref isWhitespaceOnly);
            }

            var e = new ContentChangedEventArgs(newContentString, version, this, ContentChangeType.Update, updatedLineNumbers, isWhitespaceOnly);

            OnUpdateSyntax(e);

            if (e.IsAborted)
                return false;

            UpdateSyntaxHighlighting(e);

            if (!e.IsAborted)
            {
                // processing that should not occur on initial load
                OnContentChanged(e);
            }

            return true;
        }

        /// <summary>
        /// Call to force each line of the editor to repaint, starting with the lines in e.AffectedLines
        /// </summary>
        protected void UpdateSyntaxHighlighting(ContentChangedEventArgs e)
        {
            byte[] nearbyLines;

            lock (_lines)
            {
                if (e.IsAborted)
                    return;

                // request to refresh all lines
                if (e.AffectedLines == null)
                {
                    foreach (var line in _lines)
                        line.Refresh();

                    return;
                }

                // repaint six lines on either side of each updated line (will include the affected line)
                nearbyLines = new byte[_lines.Count];
                foreach (var index in e.AffectedLines)
                {
                    var start = Math.Max(index - 6, 0);
                    var end = Math.Min(index + 6, nearbyLines.Length);
                    for (int i = start; i < end; i++)
                        nearbyLines[i] = 1;
                }

                for (int i = 0; i < nearbyLines.Length; i++)
                {
                    if (nearbyLines[i] != 0)
                        _lines[i].Refresh();
                }
            }

            lock (_lines)
            {
                if (e.IsAborted)
                    return;

                // repaint remaining lines
                for (int i = 0; i < Math.Min(nearbyLines.Length, _lines.Count); i++)
                {
                    if (nearbyLines[i] == 0)
                        _lines[i].Refresh();
                }
            }
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
        private readonly ReadOnlyObservableCollection<LineViewModel> _linesWrapper;
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

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="LineCount"/>
        /// </summary>
        public static readonly ModelProperty VisibleLinesProperty = ModelProperty.Register(typeof(CodeEditorViewModel), "VisibleLines", typeof(int), 20);

        /// <summary>
        /// Gets the number of lines being rendered.
        /// </summary>
        public int VisibleLines
        {
            get { return (int)GetValue(VisibleLinesProperty); }
            internal set { SetValue(VisibleLinesProperty, value); }
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

                case Key.F:
                    if ((e.Modifiers & ModifierKeys.Control) != 0)
                    {
                        HandleFind();
                        e.Handled = true;
                    }
                    else
                    {
                        goto default;
                    }
                    break;

                case Key.F3:
                    if ((e.Modifiers & ModifierKeys.Shift) != 0)
                        _findWindow.FindPrevious();
                    else
                        _findWindow.FindNext();
                    e.Handled = true;
                    break;

                case Key.H:
                    if ((e.Modifiers & ModifierKeys.Control) != 0)
                    {
                        HandleReplace();
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
            if (undoItem == null || undoItem.AfterText != null)
            {
                BeginUndo();
                undoItem = _undoStack.Pop();

                if (HasSelection())
                    RemoveSelection();

                var line = CursorLine;
                var column = CursorColumn;
                undoItem.After = new TextRange(line, column, line, column);

                _undoStack.Push(undoItem);
            }

            return undoItem;
        }

        private void EndTypingUndo()
        {
            var undoItem = _undoStack.Peek();
            if (undoItem.AfterText == null)
                undoItem.AfterText = GetText(undoItem.After);
        }

        /// <summary>
        /// Simulates typing a single character.
        /// </summary>
        protected void TypeCharacter(char c)
        {
            HandleCharacter(c);
        }

        internal void HandleCharacter(char c)
        {
            var undoItem = BeginTypingUndo();
            if (undoItem.Before.End < undoItem.Before.Start)
            {
                EndTypingUndo();
                undoItem = BeginTypingUndo();
            }

            var column = CursorColumn;
            var line = CursorLine;
            var lineViewModel = _lines[line - 1];
            char brace;

            if (_braceStack.Count > 0 && _braceStack.Peek() == c)
            {
                // typed the matching brace, just advance over it
                _braceStack.Pop();
            }
            else if (_braces.TryGetValue(c, out brace))
            {
                var text = lineViewModel.CurrentText;

                // typed a brace. if the next character is whitespace or punctuation, insert it and the matching character
                if (column > text.Length || Char.IsWhiteSpace(text[column - 1]) || Char.IsPunctuation(text[column - 1]))
                {
                    lineViewModel.Insert(column, c.ToString() + brace.ToString());
                    undoItem.After.End.Column += 2;
                    _braceStack.Push(brace);
                }
                else
                {
                    // next character is not whitespace or punctuation, just insert it
                    lineViewModel.Insert(column, c.ToString());
                    undoItem.After.End.Column++;
                }
            }
            else
            {
                // not a brace, just insert it
                lineViewModel.Insert(column, c.ToString());
                undoItem.After.End.Column++;
            }

            MoveCursorTo(line, column + 1, MoveCursorFlags.Typing);

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
                if (undoItem.Before.End < undoItem.Before.Start)
                {
                    EndTypingUndo();
                    undoItem = BeginTypingUndo();
                }

                var column = CursorColumn;
                var line = CursorLine;
                var lineViewModel = _lines[line - 1];
                if (column <= lineViewModel.LineLength)
                {
                    var text = lineViewModel.CurrentText;
                    undoItem.Before.End.Column++;
                    undoItem.BeforeText += text[column - 1];

                    if (_braceStack.Count > 0 && column < text.Length && text[column] == _braceStack.Peek())
                    {
                        // deleting closing brace
                        _braceStack.Pop();
                    }

                    lineViewModel.Remove(column, column);
                    WaitForTyping();
                }
                else if (line < LineCount)
                {
                    undoItem.Before.End.Line++;
                    undoItem.Before.End.Column = 1;
                    undoItem.BeforeText += '\n';

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
                if (undoItem.Before.Start < undoItem.Before.End)
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
                    var text = lineViewModel.CurrentText;

                    if (!undoItem.After.IsEmpty)
                    {
                        // if the after selection is not empty, the user has been typing, just remove the most recent character
                        undoItem.After.End.Column--;
                    }
                    else
                    {
                        // after is empty, consume a character from before and update the location of the after selection
                        undoItem.Before.End.Column--;
                        undoItem.BeforeText = text[column - 1] + undoItem.BeforeText;
                        undoItem.After.Start.Column--;
                        undoItem.After.End.Column--;
                    }

                    if (_braceStack.Count > 0 && column < text.Length && text[column] == _braceStack.Peek())
                    {
                        char brace;
                        if (_braces.TryGetValue(text[column - 1], out brace) && brace == _braceStack.Peek())
                        {
                            // erasing opening brace, also remove closing brace
                            _braceStack.Pop();
                            lineViewModel.Remove(column + 1, column + 1);
                        }
                    }

                    lineViewModel.Remove(column, column);
                    MoveCursorTo(line, column, MoveCursorFlags.Typing);

                    WaitForTyping();
                }
                else if (line > 1)
                {
                    line--;
                    column = _lines[line - 1].LineLength + 1;

                    undoItem.Before.End = new TextLocation(undoItem.Before.End.Line - 1, column);
                    undoItem.BeforeText = '\n' + undoItem.BeforeText;
                    undoItem.After = new TextRange(line, column, line, column);

                    MoveCursorTo(line, column, MoveCursorFlags.Typing);
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

            var selection = GetSelection();
            return GetText(selection);
        }

        /// <summary>
        /// Builds a string containing the text for the specified region of the editor.
        /// </summary>
        protected string GetText(int startLine, int startColumn, int endLine, int endColumn)
        {
            var selection = new TextRange(startLine, startColumn, endLine, endColumn);
            return GetText(selection);
        }

        /// <summary>
        /// Builds a string containing the text for the specified region of the editor.
        /// </summary>
        protected string GetText(TextRange selection)
        {
            if (selection.IsEmpty)
                return String.Empty;

            var builder = new StringBuilder();

            var orderedSelection = selection;
            orderedSelection.EnsureForward();
            for (int i = orderedSelection.Start.Line; i <= orderedSelection.End.Line; ++i)
            {
                if (i > _lines.Count)
                    break;

                if (i != orderedSelection.Start.Line)
                    builder.AppendLine();

                var line = _lines[i - 1];
                var text = line.CurrentText;

                var firstChar = (i == orderedSelection.Start.Line) ? orderedSelection.Start.Column - 1 : 0;
                if (firstChar > text.Length)
                    firstChar = text.Length;

                int lastChar;
                if (i == orderedSelection.End.Line)
                {
                    lastChar = orderedSelection.End.Column - 1;
                    if (lastChar > text.Length)
                        lastChar = text.Length;
                }
                else
                {
                    lastChar = text.Length;
                }
                builder.Append(text, firstChar, lastChar - firstChar);
            }

            return builder.ToString();
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
            var selection = GetSelection();
            selection.EnsureForward();

            var line = _lines[selection.Start.Line - 1];
            if (line.SelectionEnd >= line.SelectionStart)
                line.Remove(line.SelectionStart, line.SelectionEnd);

            if (selection.Start.Line == selection.End.Line)
            {
                // update the cursor location
                MoveCursorTo(selection.Start.Line, selection.Start.Column, MoveCursorFlags.None);
            }
            else
            {
                var lastLine = _lines[selection.End.Line - 1];
                if (lastLine.SelectionEnd < lastLine.LineLength)
                    line.Insert(line.LineLength + 1, lastLine.Text.Substring(lastLine.SelectionEnd, lastLine.LineLength - lastLine.SelectionEnd));

                // relocate the cursor before we remove any lines
                MoveCursorTo(selection.Start.Line, selection.Start.Column, MoveCursorFlags.None);

                for (int i = selection.End.Line - 1; i >= selection.Start.Line; --i)
                    _lines.RemoveAt(i);

                var linesRemoved = (selection.End.Line - selection.Start.Line);
                LineCount -= linesRemoved;

                for (int i = selection.Start.Line; i < _lines.Count; ++i)
                    _lines[i].Line -= linesRemoved;
            }
        }

        /// <summary>
        /// Replaces the current selection with new text.
        /// </summary>
        /// <param name="newText">The new text.</param>
        internal void ReplaceSelection(string newText)
        {
            if (newText == null)
                throw new ArgumentNullException(nameof(newText));

            BeginUndo();

            var selection = GetSelection();
            ReplaceText(selection, newText);

            EndUndo(newText);

            var item = _undoStack.Pop();

            item.After = new TextRange(selection.Front, item.After.End);

            _undoStack.Push(item);

            Refresh();
        }

        /// <summary>
        /// Replaces a selection with new text.
        /// </summary>
        /// <param name="selection">The selection.</param>
        /// <param name="newText">The new text.</param>
        /// <remarks>Should not update the undo buffer, used by <see cref="HandleUndo"/> and <see cref="HandleRedo"/>.</remarks>
        private void ReplaceText(TextRange selection, string newText)
        {
            var front = selection.Front;
            var back = selection.Back;
            var cursorLine = back.Line;
            var cursorColumn = back.Column;

            LineViewModel line;
            var linesAdded = 0;
            if (front.Line == back.Line)
            {
                line = _lines[front.Line - 1];
                if (front.Column < back.Column)
                    line.Remove(front.Column, back.Column - 1);

                if (!newText.Contains("\n"))
                {
                    line.Insert(front.Column, newText);

                    MoveCursorTo(front.Line, front.Column + newText.Length, MoveCursorFlags.None);
                    return;
                }

                var text = line.CurrentText;
                var remaining = text.Substring(front.Column - 1);
                if (front.Column <= text.Length)
                    line.Remove(front.Column, text.Length);

                line = new LineViewModel(this, line.Line + 1) { Text = remaining };
                _lines.Insert(front.Line, line);
                linesAdded = 1;
            }
            else
            {
                if (front.Line <= _lines.Count)
                {
                    line = _lines[front.Line - 1];
                    if (front.Column < line.LineLength)
                        line.Remove(front.Column, line.LineLength);
                }

                if (back.Line <= _lines.Count)
                {
                    line = _lines[back.Line - 1];
                    if (back.Column > 1)
                        line.Remove(1, back.Column - 1);

                    line.Line -= (back.Line - front.Line - 1);
                }

                for (int i = back.Line - 2; i >= front.Line; --i)
                {
                    if (i < _lines.Count - 1)
                        _lines.RemoveAt(i);
                    linesAdded--;
                }
            }

            var newTextLines = newText.Split('\n');
            line = _lines[front.Line - 1];
            line.Insert(front.Column, newTextLines[0].TrimEnd('\r'));

            if (newTextLines.Length == 1)
            {
                cursorColumn = line.LineLength + 1;

                var startLine = _lines[front.Line];
                line.Insert(line.LineLength + 1, startLine.CurrentText);

                _lines.RemoveAt(front.Line);
                linesAdded--;
            }
            else if (front.Line < _lines.Count)
            {
                line = _lines[front.Line];
                var text = newTextLines[newTextLines.Length - 1].TrimEnd('\r');
                line.Insert(1, text);
                line.Line += newTextLines.Length - 2;

                cursorColumn = text.Length + 1;
            }

            for (int i = 1; i < newTextLines.Length - 1; i++)
            {
                line = new LineViewModel(this, front.Line + i) { PendingText = newTextLines[i].TrimEnd('\r') };
                _lines.Insert(front.Line + i - 1, line);
                linesAdded++;
            }

            cursorLine = front.Line + newTextLines.Length - 1;

            if (linesAdded != 0)
            {
                for (int i = cursorLine; i < _lines.Count; ++i)
                    _lines[i].Line += linesAdded;
            }

            LineCount += linesAdded;

            MoveCursorTo(cursorLine, cursorColumn, MoveCursorFlags.Typing);
        }

        /// <summary>
        /// Selected the word at the specified location.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <param name="column">The column.</param>
        public void HighlightWordAt(int line, int column)
        {
            var word = GetWordSelection(line, column);
            if (word.Start.Line == line)
            {
                MoveCursorTo(line, word.Start.Column, MoveCursorFlags.None);
                MoveCursorTo(line, word.End.Column, MoveCursorFlags.Highlighting);
            }
        }

        /// <summary>
        /// Gets the word at the specified location.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <param name="column">The column.</param>
        /// <returns>A string representing the portion of text believed to be a separate token at the specified location.</returns>
        protected string GetWordAt(int line, int column)
        {
            var word = GetWordSelection(line, column);
            if (word.Start.Line == line)
                return GetText(word);

            return null;
        }

        private static bool IsPunctuation(char c)
        {
            // Char.IsPunctionation returned true for underscore, we want it to be
            // treated as part of the identifier, so classify it directly.
            return (c != '_' && Char.IsPunctuation(c));
        }

        private TextRange GetWordSelection(int line, int column)
        { 
            var cursorLineViewModel = _lines[line - 1];
            var currentTextPiece = cursorLineViewModel.GetTextPiece(column);
            if (currentTextPiece.Piece == null) // column exceeds line length
                return new TextRange();

            var text = currentTextPiece.Piece.Text;
            var offset = currentTextPiece.Offset;

            Predicate<char> isPartOfWord;
            if (Char.IsWhiteSpace(text[offset]))
            {
                // target character is whitespace, select all whitespace on either side
                isPartOfWord = Char.IsWhiteSpace;
            }
            else if (IsPunctuation(text[offset]))
            {
                // target character is punctuation, select all punctuation on either side
                isPartOfWord = IsPunctuation;
            }
            else
            {
                // target character is not punctuation or whitespace, select all characters
                // on either side until whitespace or punctuation is found
                isPartOfWord = c => !Char.IsWhiteSpace(c) && !IsPunctuation(c);
            }

            // find the boundaries of the word
            do
            {
                offset--;
            } while (offset >= 0 && isPartOfWord(text[offset]));

            int wordStart = column - (currentTextPiece.Offset - offset) + 1;

            offset = currentTextPiece.Offset;
            do
            {
                offset++;
            } while (offset < text.Length && isPartOfWord(text[offset]));

            int wordEnd = column + (offset - currentTextPiece.Offset);

            // return the bounds
            return new TextRange(line, wordStart, line, wordEnd);
        }

        private void HandleLeft(MoveCursorFlags flags, bool nextWord)
        {
            var newLine = CursorLine;
            var newColumn = CursorColumn;

            if (nextWord)
            {
                var cursorLineViewModel = _lines[newLine - 1];
                var text = cursorLineViewModel.CurrentText;
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
                var selection = GetSelection();
                if (selection.Start.Line != selection.End.Line)
                {
                    // indent or unindent block
                    Indent(selection, !isShift);
                    return;
                }

                var line = _lines[selection.Start.Line - 1];
                if (selection.Start.Column == 1 && selection.End.Column == line.LineLength + 1)
                {
                    // indent or unindent single line
                    Indent(selection, !isShift);
                    return;
                }

                // only portion of line selected - replace with tab for tab, or nothing for shift+tab
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

        private void Indent(TextRange selection, bool isIndent)
        {
            var orderedSelection = selection;
            orderedSelection.EnsureForward();

            var endLine = orderedSelection.End.Line;
            if (orderedSelection.End.Column == 1)
                endLine--;

            MoveCursorTo(orderedSelection.Start.Line, 1, MoveCursorFlags.None);
            MoveCursorTo(endLine + 1, 1, MoveCursorFlags.Highlighting);

            BeginUndo();

            for (int i = orderedSelection.Start.Line; i <= endLine; i++)
            {
                var line = _lines[i - 1];
                var text = line.CurrentText;
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

            MoveCursorTo(orderedSelection.Start.Line, 1, MoveCursorFlags.None);
            MoveCursorTo(endLine + 1, 1, MoveCursorFlags.Highlighting);

            EndUndo(GetSelectedText());

            Refresh();
        }

        private void MergeNextLine()
        {
            // merge the text from the next line into the current line
            var cursorLine = CursorLine;
            var cursorLineViewModel = _lines[cursorLine - 1];
            var left = cursorLineViewModel.CurrentText;
            var nextLineViewModel = _lines[cursorLine];
            var right = nextLineViewModel.CurrentText;
            cursorLineViewModel.PendingText = left + right;

            // merge the TextPieces so the merged text appears
            var newPieces = new List<TextPiece>(cursorLineViewModel.TextPieces);
            newPieces.AddRange(nextLineViewModel.TextPieces);
            cursorLineViewModel.SetValue(LineViewModel.TextPiecesProperty, newPieces.ToArray());

            // remove the line that was merged
            RaiseLineChanged(new LineEventArgs(cursorLineViewModel));
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
            if (undoItem.Before.End < undoItem.Before.Start)
            {
                EndTypingUndo();
                undoItem = BeginTypingUndo();
            }

            // split the current line at the cursor
            var cursorLine = CursorLine;
            var cursorLineViewModel = _lines[cursorLine - 1];
            string text = cursorLineViewModel.CurrentText;
            var cursorColumn = CursorColumn - 1; // string index is 0-based
            string right = (cursorColumn < text.Length) ? text.Substring(cursorColumn) : String.Empty;

            // truncate the first line
            if (right.Length > 0)
                cursorLineViewModel.Remove(cursorColumn + 1, text.Length);

            // indent
            var indent = 0;
            while (indent < text.Length && Char.IsWhiteSpace(text[indent]))
                indent++;
            if (indent > 0)
                right = new string(' ', indent) + right.TrimStart();

            // add a new line
            int newLines = 1;
            var newLineViewModel = new LineViewModel(this, cursorLine + 1) { PendingText = right };
            _lines.Insert(cursorLine, newLineViewModel);
            RaiseLineChanged(new LineEventArgs(newLineViewModel));

            // create TextPieces for the new line so it appears
            if (right.Length > 0)
            {
                var e = new LineFormatEventArgs(newLineViewModel);
                newLineViewModel.SetValue(LineViewModel.TextPiecesProperty, e.BuildTextPieces());
            }

            // update undo item
            undoItem.After.End = new TextLocation(undoItem.After.End.Line + 1, indent + 1);

            // if breaking apart braces, insert an extra newline indented an additional level
            if (_braceStack.Count > 0 && cursorColumn < text.Length && text[cursorColumn] == _braceStack.Peek())
            {
                _braceStack.Clear();           // no longer try to match the closing brace
                undoItem.After.End.Line++;     // ensure closing brace is included in undo information
                newLineViewModel.Line++;       // adjust the closing brace line number

                indent += 4;
                newLineViewModel = new LineViewModel(this, cursorLine + 1) { PendingText = new string(' ', indent) };
                _lines.Insert(cursorLine, newLineViewModel);
                newLines++;
            }
            else if (indent > cursorColumn)
            {
                // enter pressed in the indent margin (or more likely at the start of line)
                // keep cursor in the column where it was
                indent = cursorColumn;
            }

            // update line count (have to do before moving cursor)
            LineCount += newLines;

            // update the cursor position
            MoveCursorTo(cursorLine + 1, indent + 1, MoveCursorFlags.None);

            // update the line numbers
            for (int i = CursorLine + newLines - 1; i < _lines.Count; i++)
                _lines[i].Line += newLines;

            // schedule a refresh to update the syntax highlighting
            WaitForTyping();
        }

        /// <summary>
        /// Behavioral flags to pass to the <see cref="MoveCursorTo"/> method.
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

            /// <summary>
            /// Indicates the cursor is being moved as the result of inserting or removing text.
            /// </summary>
            Typing = 0x04,
        }

        /// <summary>
        /// Moves the cursor to the specified location.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <param name="column">The column.</param>
        /// <param name="flags">Additional logic to perform while moving the cursor.</param>
        public virtual void MoveCursorTo(int line, int column, MoveCursorFlags flags)
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

            // update the virtual cursor column
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

            // if the cursor moved and we're not typing, stop trying to match braces
            if ((flags & MoveCursorFlags.Typing) == 0)
                _braceStack.Clear();

            // update highlighting
            if (_selectionStartLine > _lines.Count)
            {
                _selectionStartLine = _lines.Count;
                _selectionStartColumn = _lines[_lines.Count - 1].LineLength + 1;
            }

            if (_selectionEndLine > _lines.Count)
            {
                _selectionEndLine = _lines.Count;
                _selectionEndColumn = _lines[_lines.Count - 1].LineLength + 1;
            }

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
                    for (int i = _selectionEndLine; i < line; i++)
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
                    var selection = GetSelection();
                    selection.EnsureForward();

                    _lines[selection.Start.Line - 1].Select(selection.Start.Column, _lines[selection.Start.Line - 1].LineLength);
                    for (int i = selection.Start.Line; i < selection.End.Line - 1; i++)
                        _lines[i].Select(1, _lines[i].LineLength);
                    _lines[selection.End.Line - 1].Select(1, selection.End.Column - 1);
                }
            }

            // update the cursor position
            if (line != currentLine)
            {
                if (currentLine <= _lines.Count)
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
            if (text != null)
                ReplaceSelection(text);
        }

        private class UndoItem
        {
            public TextRange Before;
            public TextRange After;

            public string BeforeText;
            public string AfterText;

            public override string ToString()
            {
                var builder = new StringBuilder();
                builder.Append('"');
                if (BeforeText != null)
                    builder.Append(BeforeText);
                builder.Append("\" => \"");
                if (AfterText != null)
                    builder.Append(AfterText);
                builder.Append('"');
                return builder.ToString();
            }
        }

        internal TextRange GetSelection()
        {
            if (HasSelection())
                return new TextRange(_selectionStartLine, _selectionStartColumn, _selectionEndLine, _selectionEndColumn);

            var cursorLine = CursorLine;
            var cursorColumn = CursorColumn;
            return new TextRange(cursorLine, cursorColumn, cursorLine, cursorColumn);
        }

        private void BeginUndo()
        {
            _redoStack.Clear();

            var item = new UndoItem();
            item.Before = GetSelection();
            item.BeforeText = GetText(item.Before);
            _undoStack.Push(item);
        }

        private void EndUndo(string text)
        {
            var item = _undoStack.Pop();
            item.After = GetSelection();
            item.AfterText = text;
            _undoStack.Push(item);
        }

        private void HandleUndo()
        {
            if (_undoStack.IsEmpty)
                return;

            var item = _undoStack.Pop();
            _redoStack.Push(item);

            MoveCursorTo(item.After.Start.Line, item.After.Start.Column, MoveCursorFlags.None);
            ReplaceText(item.After, item.BeforeText);
            MoveCursorTo(item.Before.End.Line, item.Before.End.Column, MoveCursorFlags.None);

            Refresh();
        }

        private void HandleRedo()
        {
            if (_redoStack.Count == 0)
                return;

            var item = _redoStack.Pop();
            _undoStack.Push(item);

            MoveCursorTo(item.Before.Start.Line, item.Before.Start.Column, MoveCursorFlags.None);
            ReplaceText(item.Before, item.AfterText);
            MoveCursorTo(item.After.End.Line, item.After.End.Column, MoveCursorFlags.None);

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

        /// <summary>
        /// Moves the cursor to the first character of the specified line and centers the line in the editor
        /// </summary>
        /// <param name="lineNumber">The line number.</param>
        public void GotoLine(int lineNumber)
        {
            var column = 0;
            if (lineNumber >= 1 && lineNumber <= _lines.Count)
            {
                var lineViewModel = _lines[lineNumber - 1];
                var text = lineViewModel.CurrentText;

                while (column < text.Length && Char.IsWhiteSpace(text[column]))
                    column++;
            }

            MoveCursorTo(lineNumber, column, MoveCursorFlags.None);
            IsCenterOnLineRequested = true;
        }

        internal static readonly ModelProperty IsCenterOnLineRequestedProperty = ModelProperty.Register(typeof(CodeEditorViewModel), "IsCenterOnLineRequested", typeof(bool), false);
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal bool IsCenterOnLineRequested
        {
            get { return (bool)GetValue(IsCenterOnLineRequestedProperty); }
            set { SetValue(IsCenterOnLineRequestedProperty, value); }
        }

        private void HandleFind()
        {
            if (_selectionStartLine == _selectionEndLine)
            {
                if (_selectionStartLine == 0)
                    _findWindow.SearchText.Text = GetWordAt(CursorLine, CursorColumn);
                else
                    _findWindow.SearchText.Text = GetSelectedText();
            }

            _findWindow.ShouldFocusSearchText = true;

            ShowToolWindow(_findWindow);
        }

        private void HandleReplace()
        {
            bool isVisible = true;

            var toolWindow = ToolWindow as ReplaceToolWindowViewModel;
            if (toolWindow == null)
            {
                toolWindow = new ReplaceToolWindowViewModel(this);
                isVisible = false;
            }

            if (_selectionStartLine == _selectionEndLine)
            {
                if (_selectionStartLine == 0)
                    toolWindow.SearchText.Text = GetWordAt(CursorLine, CursorColumn);
                else
                    toolWindow.SearchText.Text = GetSelectedText();
            }

            toolWindow.ShouldFocusSearchText = true;

            if (!isVisible)
                ShowToolWindow(toolWindow);
        }
    }
}
