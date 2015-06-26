using Jamiras.Services;

namespace Jamiras.Components
{
    public interface ILogger
    {
        /// <summary>
        /// Gets whether or not the specified logging level is enabled.
        /// </summary>
        bool IsEnabled(LogLevel level);

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
}
