using Jamiras.Commands;
using Jamiras.DataModels;
using Jamiras.ViewModels.Fields;
using System;
using System.Windows.Input;

namespace Jamiras.ViewModels.CodeEditor.ToolWindows
{
    /// <summary>
    /// Defines a tool window for finding and replacing text in the editor.
    /// </summary>
    /// <seealso cref="Jamiras.ViewModels.CodeEditor.ToolWindowViewModel" />
    public class ReplaceToolWindowViewModel : FindToolWindowViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GotoLineToolWindowViewModel"/> class.
        /// </summary>
        /// <param name="owner">The editor that owns the tool window.</param>
        public ReplaceToolWindowViewModel(CodeEditorViewModel owner)
            : base(owner)
        {
            Caption = "Replace";

            ReplaceText = new TextFieldViewModel("Replace", 255);

            ReplaceCommand = new DelegateCommand(Replace);
            ReplaceAllCommand = new DelegateCommand(ReplaceAll);
        }

        /// <summary>
        /// Gets the command to replace the selected highlight with the new text.
        /// </summary>
        public CommandBase ReplaceCommand { get; private set; }

        /// <summary>
        /// Gets the command to replace all matching items with the new text.
        /// </summary>
        public CommandBase ReplaceAllCommand { get; private set; }

        /// <summary>
        /// Gets the view model for the search text field.
        /// </summary>
        public TextFieldViewModel ReplaceText { get; private set; }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="IsReplaceTextFocused"/>
        /// </summary>
        public static readonly ModelProperty IsReplaceTextFocusedProperty = ModelProperty.Register(typeof(FindToolWindowViewModel), "IsReplaceTextFocused", typeof(bool), false);

        /// <summary>
        /// Bindable property for identifying if the ReplaceText field is focused.
        /// </summary>
        public bool IsReplaceTextFocused
        {
            get { return (bool)GetValue(IsReplaceTextFocusedProperty); }
            set { SetValue(IsReplaceTextFocusedProperty, value); }
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
                    if (IsReplaceTextFocused)
                    {
                        Replace();
                        e.Handled = true;
                        return;
                    }
                    break;
            }

            base.OnKeyPressed(e);
        }

        /// <summary>
        /// Replaces the selected highlight with the new text.
        /// </summary>
        public void Replace()
        {
            if (String.Compare(Owner.GetSelectedText(), SearchText.Text, StringComparison.OrdinalIgnoreCase) == 0)
            {
                Owner.ReplaceSelection(ReplaceText.Text);
                RemoveCurrentMatch();
            }
        }

        /// <summary>
        /// Replaces all matching items with the new text.
        /// </summary>
        public void ReplaceAll()
        {
            int replaceCount = MatchCount;
            if (replaceCount > 0)
            {
                int matchCount = replaceCount;
                while (matchCount > 1)
                {
                    Replace();
                    matchCount--;
                }

                var line = Owner.CursorLine;
                var column = Owner.CursorColumn;
                Replace();


                if (MatchCount == 0)
                    Owner.MoveCursorTo(line, column + SearchText.Text.Length, CodeEditorViewModel.MoveCursorFlags.None);
                else
                    replaceCount -= MatchCount;
            }

            MessageBoxViewModel.ShowMessage(string.Format("Replaced {0} occurrances", replaceCount));

            Owner.IsFocusRequested = true;
        }
    }
}
