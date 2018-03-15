namespace Jamiras.ViewModels.CodeEditor
{
    public class CodeReferenceViewModel : ViewModelBase
    {
        public int StartLine { get; set; }
        public int StartColumn { get; set; }
        public int EndLine { get; set; }
        public int EndColumn { get; set; }

        public string Message { get; set; }
    }
}
