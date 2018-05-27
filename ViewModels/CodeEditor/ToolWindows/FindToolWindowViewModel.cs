using Jamiras.Commands;
using Jamiras.DataModels;
using Jamiras.ViewModels.Fields;
using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace Jamiras.ViewModels.CodeEditor.ToolWindows
{
    /// <summary>
    /// Defines a tool window for searching for text in the editor.
    /// </summary>
    /// <seealso cref="Jamiras.ViewModels.CodeEditor.ToolWindowViewModel" />
    public class FindToolWindowViewModel : ToolWindowViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GotoLineToolWindowViewModel"/> class.
        /// </summary>
        /// <param name="owner">The editor that owns the tool window.</param>
        public FindToolWindowViewModel(CodeEditorViewModel owner)
            : base(owner)
        {
            Caption = "Find";

            SearchText = new TextFieldViewModel("Find", 255);
            SearchText.AddPropertyChangedHandler(TextFieldViewModel.TextProperty, OnTextChanged);

            FindNextCommand = new DelegateCommand(FindNext);
            FindPreviousCommand = new DelegateCommand(FindPrevious);

            _matches = new List<MatchLocation>();
        }

        private List<MatchLocation> _matches;

        /// <summary>
        /// Gets the command to move the selected highlight to the next match.
        /// </summary>
        public CommandBase FindNextCommand { get; private set; }

        /// <summary>
        /// Gets the command to move the selected highlight to the next match.
        /// </summary>
        public CommandBase FindPreviousCommand { get; private set; }

        /// <summary>
        /// Gets the view model for the search text field.
        /// </summary>
        public TextFieldViewModel SearchText { get; private set; }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="ShouldFocusSearchText"/>
        /// </summary>
        public static readonly ModelProperty ShouldFocusSearchTextProperty = ModelProperty.Register(typeof(FindToolWindowViewModel), "ShouldFocusSearchText", typeof(bool), false);

        /// <summary>
        /// Bindable property for causing the search text field to be focused.
        /// </summary>
        /// <remarks>
        /// Set to <c>true</c> to cause the search text field to be focused.
        /// </remarks>
        public bool ShouldFocusSearchText
        {
            get { return (bool)GetValue(ShouldFocusSearchTextProperty); }
            set { SetValue(ShouldFocusSearchTextProperty, value); }
        }

        /// <summary>
        /// Allows the tool window to process a key press before the editor if the tool window has focus.
        /// </summary>
        /// <param name="e">Information about which key was pressed.</param>
        protected override void OnKeyPressed(KeyPressedEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                case Key.Down:
                    FindNext();
                    e.Handled = true;
                    return;

                case Key.Up:
                    FindPrevious();
                    e.Handled = true;
                    return;
            }

            base.OnKeyPressed(e);
        }

        private void OnTextChanged(object sender, ModelPropertyChangedEventArgs e)
        {
            _matches.Clear();

            var searchText = (string)e.NewValue;
            if (String.IsNullOrEmpty(searchText))
            {
                Index = 0;
                MatchCount = 0;
                return;
            }

            var nextIndex = -1;
            var cursorLine = Owner.CursorLine;
            var cursorColumn = Owner.CursorColumn;
            foreach (var line in Owner.Lines)
            {
                var lineText = line.PendingText ?? line.Text;
                int start = 0;
                do
                {
                    var index = lineText.IndexOf(searchText, start, StringComparison.OrdinalIgnoreCase);
                    if (index == -1)
                        break;

                    if (nextIndex == -1)
                    {
                        if (line.Line > cursorLine || (line.Line == cursorLine && index + 1 + searchText.Length >= cursorColumn))
                            nextIndex = _matches.Count;
                    }

                    _matches.Add(new MatchLocation { Line = line.Line, Column = index + 1 });
                    start = index + searchText.Length;
                } while (true);
            }

            MatchCount = _matches.Count;

            // force OnIndexChanged by toggling to 0 briefly before setting the actual new value
            Index = 0;
            Index = nextIndex + 1;
        }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="Index"/>
        /// </summary>
        public static readonly ModelProperty IndexProperty = ModelProperty.Register(typeof(FindToolWindowViewModel), "Index", typeof(int), 0, OnIndexChanged);

        /// <summary>
        /// Get the index of the currently highlighted result.
        /// </summary>
        public int Index
        {
            get { return (int)GetValue(IndexProperty); }
            private set { SetValue(IndexProperty, value); }
        }

        private static void OnIndexChanged(object sender, ModelPropertyChangedEventArgs e)
        {
            var vm = (FindToolWindowViewModel)sender;
            var index = (int)e.NewValue;
            if (index == 0)
                return;

            index--;
            if (index >= vm._matches.Count)
                return;

            var match = vm._matches[index];
            vm.Owner.MoveCursorTo(match.Line, match.Column, CodeEditorViewModel.MoveCursorFlags.None);
            vm.Owner.MoveCursorTo(match.Line, match.Column + vm.SearchText.Text.Length, CodeEditorViewModel.MoveCursorFlags.Highlighting);
        }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="MatchCount"/>
        /// </summary>
        public static readonly ModelProperty MatchCountProperty = ModelProperty.Register(typeof(FindToolWindowViewModel), "MatchCount", typeof(int), 0);

        /// <summary>
        /// Get the number of matches found.
        /// </summary>
        public int MatchCount
        {
            get { return (int)GetValue(MatchCountProperty); }
            private set { SetValue(MatchCountProperty, value); }
        }

        private struct MatchLocation
        {
            public int Line;
            public int Column;
        }

        /// <summary>
        /// Moves the selected highlight to the next match.
        /// </summary>
        public void FindNext()
        {
            if (_matches.Count == 0)
                return;

            if (Index == _matches.Count)
                Index = 1;
            else
                Index = Index + 1;
        }

        /// <summary>
        /// Moves the selected highlight to the previous match.
        /// </summary>
        public void FindPrevious()
        {
            if (_matches.Count == 0)
                return;

            if (Index == 1)
                Index = _matches.Count;
            else
                Index = Index - 1;
        }

        /// <summary>
        /// Removes the current item from the matches list.
        /// </summary>
        protected void RemoveCurrentMatch()
        {
            if (_matches.Count == 0)
                return;

            var index = Index - 1;
            var matchLine = _matches[index].Line;
            _matches.RemoveAt(index);
            MatchCount--;

            // quickly set index to invalid so setting it back to the current index raises a property changed event
            Index = 0;

            if (_matches.Count > 0)
            {
                if (index == _matches.Count)
                {
                    // move to the first match
                    Index = 1;
                }
                else
                {
                    // if any other matches are on current line, make sure their offsets are still correct
                    if (_matches[index].Line == matchLine)
                    {
                        var searchText = SearchText.Text;
                        var lineViewModel = Owner.Lines[matchLine - 1];
                        var lineText = lineViewModel.PendingText ?? lineViewModel.Text;
                        var offset = lineText.IndexOf(searchText, StringComparison.OrdinalIgnoreCase);
                        if (offset > 0)
                        {
                            offset = offset - _matches[index].Column + 1;
                            if (offset != 0)
                            {
                                var index2 = index;
                                do
                                {
                                    _matches[index2] = new MatchLocation { Line = matchLine, Column = _matches[index2].Column + offset };
                                    index2++;
                                } while (index2 < _matches.Count && _matches[index2].Line == matchLine);
                            }
                        }
                    }

                    // move to the next match
                    Index = index + 1;
                }
            }
        }
    }
}
