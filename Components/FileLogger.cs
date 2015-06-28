using System.IO;

namespace Jamiras.Components
{
    public class FileLogger : ILogTarget
    {
        public FileLogger(string filename)
        {
            _stream = File.CreateText(filename);
            _stream.AutoFlush = true;
        }

        private readonly StreamWriter _stream;

        public void Write(string message)
        {
            _stream.WriteLine(message);
        }
    }
}
