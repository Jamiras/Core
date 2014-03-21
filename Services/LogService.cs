using System;
using System.Collections.Generic;
using System.Text;
using Jamiras.Components;

namespace Jamiras.Services
{
    [Export(typeof(ILogService))]
    internal class LogService : ILogService
    {
        public LogService()
        {
            Levels = LogLevels.General | LogLevels.Warning | LogLevels.Error;
            _messages = new Queue<string>(QueueSize);
        }

        private readonly Queue<string> _messages;
        private const int QueueSize = 512;

        /// <summary>
        /// Gets the most recent buffered log messages.
        /// </summary>
        public IEnumerable<string> Messages
        {
            get { return _messages; } 
        }

        /// <summary>
        /// Gets or sets the active logging levels.
        /// </summary>
        public LogLevels Levels { get; set; }

        private bool IsEnabled(LogLevels level)
        {
            return (Levels & level) != 0;
        }

        private void Write(LogLevels level, string message)
        {
            var builder = new StringBuilder();

            if ((Levels & LogLevels.Timestamps) != 0)
            {
                builder.Append(DateTime.Now.ToString("HH:mm:ss.fff"));
                builder.Append(' ');
            }

            switch (level)
            {
                case LogLevels.General:
                    builder.Append("GEN ");
                    break;
                case LogLevels.Verbose:
                    builder.Append("VER ");
                    break;
                case LogLevels.Warning:
                    builder.Append("WRN ");
                    break;
                case LogLevels.Error:
                    builder.Append("ERR ");
                    break;
            }

            builder.Append(message);

            if (_messages.Count == QueueSize)
                _messages.Dequeue();

            _messages.Enqueue(builder.ToString());
        }

        /// <summary>
        /// Writes a general message to the log.
        /// </summary>
        public void Write(string message)
        {
            if (IsEnabled(LogLevels.General))
                Write(LogLevels.General, message);
        }

        /// <summary>
        /// Writes a general message to the log.
        /// </summary>
        public void Write(string message, params object[] args)
        {
            if (IsEnabled(LogLevels.General))
                Write(LogLevels.General, String.Format(message, args));
        }

        /// <summary>
        /// Writes a verbose message to the log.
        /// </summary>
        public void WriteVerbose(string message)
        {
            if (IsEnabled(LogLevels.Verbose))
                Write(LogLevels.Verbose, message);
        }

        /// <summary>
        /// Writes a verbose message to the log.
        /// </summary>
        public void WriteVerbose(string message, params object[] args)
        {
            if (IsEnabled(LogLevels.Verbose))
                Write(LogLevels.Verbose, String.Format(message, args));
        }

        /// <summary>
        /// Writes a warning message to the log.
        /// </summary>
        public void WriteWarning(string message)
        {
            if (IsEnabled(LogLevels.Warning))
                Write(LogLevels.Warning, message);
        }

        /// <summary>
        /// Writes a warning message to the log.
        /// </summary>
        public void WriteWarning(string message, params object[] args)
        {
            if (IsEnabled(LogLevels.Warning))
                Write(LogLevels.Warning, String.Format(message, args));
        }

        /// <summary>
        /// Writes an error message to the log.
        /// </summary>
        public void WriteError(string message)
        {
            if (IsEnabled(LogLevels.Error))
                Write(LogLevels.Error, message);
        }

        /// <summary>
        /// Writes an error message to the log.
        /// </summary>
        public void WriteError(string message, params object[] args)
        {
            if (IsEnabled(LogLevels.Error))
                Write(LogLevels.Error, String.Format(message, args));
        }
    }
}
