using System;

namespace Jamiras.ViewModels.CodeEditor
{
    /// <summary>
    /// Parameters associated to a <see cref="LineViewModel"/> event.
    /// </summary>
    /// <seealso cref="System.EventArgs" />
    public class LineEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LineEventArgs"/> class.
        /// </summary>
        /// <param name="line">The associated line.</param>
        public LineEventArgs(LineViewModel line)
        {
            Line = line;
            Text = line.PendingText ?? line.Text;
        }

        /// <summary>
        /// Gets the associated line.
        /// </summary>
        public LineViewModel Line { get; private set; }

        /// <summary>
        /// Gets the current text for the associated line.
        /// </summary>
        public string Text { get; private set; }
    }
}
