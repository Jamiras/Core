using System.IO;

namespace Jamiras.Components
{
    /// <summary>
    /// <see cref="ILogTarget"/> implementation for logging to a file.
    /// </summary>
    public class FileLogger : ILogTarget
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileLogger"/> class.
        /// </summary>
        /// <param name="filename">The file to write to. Will overwrite any existing file.</param>
        public FileLogger(string filename)
        {
            _stream = File.CreateText(filename);
            _stream.AutoFlush = true;
        }

        private readonly StreamWriter _stream;

        /// <summary>
        /// Writes a message to the log.
        /// </summary>
        public void Write(string message)
        {
            _stream.WriteLine(message);
        }
    }
}
