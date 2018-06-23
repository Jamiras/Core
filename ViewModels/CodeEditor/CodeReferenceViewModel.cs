namespace Jamiras.ViewModels.CodeEditor
{
    /// <summary>
    /// Represents a region within the <see cref="CodeEditor"/>'s text.
    /// </summary>
    /// <seealso cref="Jamiras.ViewModels.ViewModelBase" />
    public class CodeReferenceViewModel : ViewModelBase
    {
        /// <summary>
        /// Gets or sets the line where the reference starts.
        /// </summary>
        public int StartLine { get; set; }

        /// <summary>
        /// Gets or sets the column where the reference ends.
        /// </summary>
        public int StartColumn { get; set; }

        /// <summary>
        /// Gets or sets the line where the reference starts.
        /// </summary>
        public int EndLine { get; set; }

        /// <summary>
        /// Gets or sets the column where the reference ends.
        /// </summary>
        public int EndColumn { get; set; }

        /// <summary>
        /// Gets or sets a message to associated to the reference.
        /// </summary>
        public string Message { get; set; }
    }
}
