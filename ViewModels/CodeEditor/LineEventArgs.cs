using System;

namespace Jamiras.ViewModels.CodeEditor
{
    public class LineEventArgs : EventArgs
    {
        public LineEventArgs(LineViewModel line)
        {
            Line = line;
            Text = line.PendingText ?? line.Text;
        }

        public LineViewModel Line { get; private set; }

        public string Text { get; private set; }
    }
}
