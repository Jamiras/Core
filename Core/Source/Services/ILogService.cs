using Jamiras.Components;
using System;
using System.Collections.Generic;

namespace Jamiras.Services
{
    /// <summary>
    /// A service for managing the active log targets and logging level.
    /// </summary>
    public interface ILogService
    {
        /// <summary>
        /// Gets or sets the active logging level.
        /// </summary>
        LogLevel Level { get; set; }

        /// <summary>
        /// Gets or sets whether the timestamp should be logged.
        /// </summary>
        bool IsTimestampLogged { get; set; }

        /// <summary>
        /// Gets the collection of loggers that messages will be written to.
        /// </summary>
        ICollection<ILogTarget> Loggers { get; }

        /// <summary>
        /// Gets a logger for the provided key.
        /// </summary>
        ILogger GetLogger(string key);
    }

    /// <summary>
    /// The various levels supported by an <see cref="ILogger"/>
    /// </summary>
    [Flags]
    public enum LogLevel
    {
        /// <summary>
        /// Logging is disabled.
        /// </summary>
        None = 0,

        /// <summary>
        /// Error logging is enabled.
        /// </summary>
        Error = 1,

        /// <summary>
        /// Warning logging is enabled.
        /// </summary>
        Warning = 2,

        /// <summary>
        /// General logging is enabled.
        /// </summary>
        General = 3,

        /// <summary>
        /// Detailed logging is enabled.
        /// </summary>
        Verbose = 4,
    }
}
