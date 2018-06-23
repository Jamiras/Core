using Jamiras.Commands;
using Jamiras.DataModels;
using System.Collections.ObjectModel;

namespace Jamiras.ViewModels.CodeEditor.ToolWindows
{
    /// <summary>
    /// <see cref="ToolWindowViewModel"/> for display a list of <see cref="CodeReferenceViewModel"/>s.
    /// </summary>
    /// <seealso cref="Jamiras.ViewModels.CodeEditor.ToolWindowViewModel" />
    public class CodeReferencesToolWindowViewModel : ToolWindowViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CodeReferencesToolWindowViewModel"/> class.
        /// </summary>
        /// <param name="caption">The tool window caption.</param>
        /// <param name="owner">The editor that owns the tool window.</param>
        public CodeReferencesToolWindowViewModel(string caption, CodeEditorViewModel owner)
            : base(owner)
        {
            Caption = caption;

            References = new ObservableCollection<CodeReferenceViewModel>();
            GotoReferenceCommand = new DelegateCommand<CodeReferenceViewModel>(GotoReference);
        }

        /// <summary>
        /// Closes the tool window.
        /// </summary>
        public override void Close()
        {
            IsVisible = false;
        }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="IsVisible"/>.
        /// </summary>
        public ModelProperty IsVisibleProperty = ModelProperty.Register(typeof(CodeReferencesToolWindowViewModel), "IsVisible", typeof(bool), false);

        /// <summary>
        /// Gets or sets whether this tool window should be visible.
        /// </summary>
        public bool IsVisible
        {
            get { return (bool)GetValue(IsVisibleProperty); }
            set { SetValue(IsVisibleProperty, value); }
        }

        /// <summary>
        /// Gets the list of <see cref="CodeReferenceViewModel"/>s to display in this tool window.
        /// </summary>
        public ObservableCollection<CodeReferenceViewModel> References { get; private set; }

        /// <summary>
        /// Gets a command that jumps to the specified reference in the editor.
        /// </summary>
        public CommandBase<CodeReferenceViewModel> GotoReferenceCommand { get; private set; }

        private void GotoReference(CodeReferenceViewModel reference)
        {
            Owner.GotoLine(reference.StartLine);
            Owner.MoveCursorTo(reference.StartLine, reference.StartColumn, CodeEditorViewModel.MoveCursorFlags.None);
            Owner.MoveCursorTo(reference.EndLine, reference.EndColumn + 1, CodeEditorViewModel.MoveCursorFlags.Highlighting);
        }
    }
}
