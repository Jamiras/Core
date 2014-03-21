using System;
using System.Collections.Generic;

namespace Jamiras.Services
{
    public interface ILogService
    {
        /// <summary>
        /// Gets or sets the active logging levels.
        /// </summary>
        LogLevels Levels { get; set; }

        /// <summary>
        /// Gets the most recent buffered log messages.
        /// </summary>
        IEnumerable<string> Messages { get; }

        /// <summary>
        /// Writes a general message to the log.
        /// </summary>
        void Write(string message);

        /// <summary>
        /// Writes a general message to the log.
        /// </summary>
        void Write(string message, params object[] args);

        /// <summary>
        /// Writes a verbose message to the log.
        /// </summary>
        void WriteVerbose(string message);

        /// <summary>
        /// Writes a verbose message to the log.
        /// </summary>
        void WriteVerbose(string message, params object[] args);

        /// <summary>
        /// Writes a warning message to the log.
        /// </summary>
        void WriteWarning(string message);

        /// <summary>
        /// Writes a warning message to the log.
        /// </summary>
        void WriteWarning(string message, params object[] args);

        /// <summary>
        /// Writes an error message to the log.
        /// </summary>
        void WriteError(string message);

        /// <summary>
        /// Writes an error message to the log.
        /// </summary>
        void WriteError(string message, params object[] args);
    }

    [Flags]
    public enum LogLevels
    {
        /// <summary>
        /// Logging is disabled.
        /// </summary>
        None = 0,

        /// <summary>
        /// Detailed logging is enabled.
        /// </summary>
        Verbose = 0x01,

        /// <summary>
        /// General logging is enabled.
        /// </summary>
        General = 0x02,

        /// <summary>
        /// Warning logging is enabled.
        /// </summary>
        Warning = 0x04,

        /// <summary>
        /// Error logging is enabled.
        /// </summary>
        Error = 0x08,

        /// <summary>
        /// Include timestamps on each message.
        /// </summary>
        Timestamps = 0x10,
    }
}
