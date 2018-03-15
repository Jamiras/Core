using Jamiras.Commands;
using Jamiras.DataModels;
using System.Collections.ObjectModel;

namespace Jamiras.ViewModels.CodeEditor.ToolWindows
{
    public class CodeReferencesToolWindowViewModel : ToolWindowViewModel
    {
        public CodeReferencesToolWindowViewModel(string caption, CodeEditorViewModel owner)
            : base(owner)
        {
            Caption = caption;

            References = new ObservableCollection<CodeReferenceViewModel>();
            GotoReferenceCommand = new DelegateCommand<CodeReferenceViewModel>(GotoReference);
        }

        public override void Close()
        {
            IsVisible = false;
        }

        public ModelProperty IsVisibleProperty = ModelProperty.Register(typeof(CodeReferencesToolWindowViewModel), "IsVisible", typeof(bool), false);
        public bool IsVisible
        {
            get { return (bool)GetValue(IsVisibleProperty); }
            set { SetValue(IsVisibleProperty, value); }
        }

        public ModelProperty HeightProperty = ModelProperty.Register(typeof(CodeReferencesToolWindowViewModel), "Height", typeof(int), 80);
        public int Height
        {
            get { return (int)GetValue(HeightProperty); }
            set { SetValue(HeightProperty, value); }
        }

        public ObservableCollection<CodeReferenceViewModel> References { get; private set; }

        public CommandBase<CodeReferenceViewModel> GotoReferenceCommand { get; private set; }

        private void GotoReference(CodeReferenceViewModel reference)
        {
            Owner.MoveCursorTo(reference.StartLine, reference.StartColumn, CodeEditorViewModel.MoveCursorFlags.None);
            Owner.MoveCursorTo(reference.EndLine, reference.EndColumn + 1, CodeEditorViewModel.MoveCursorFlags.Highlighting);
        }
    }
}
