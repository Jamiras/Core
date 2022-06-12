using System.Collections.Generic;
using Jamiras.Components;
using Jamiras.Services;

namespace Jamiras.Core.Services.Impl
{
    [Export(typeof(ILogService))]
    internal class LogService : ILogService
    {
        public LogService()
        {
            Level = LogLevel.General;
            _loggers = new List<ILogTarget>();
        }

        private readonly List<ILogTarget> _loggers;

        /// <summary>
        /// Gets or sets the active logging level.
        /// </summary>
        public LogLevel Level { get; set; }

        /// <summary>
        /// Gets or sets whether the timestamp should be logged.
        /// </summary>
        public bool IsTimestampLogged { get; set; }

        /// <summary>
        /// Gets the collection of loggers that messages will be written to.
        /// </summary>
        public ICollection<ILogTarget> Loggers
        {
            get { return _loggers; }
        }

        /// <summary>
        /// Gets a logger for the provided key.
        /// </summary>
        public ILogger GetLogger(string key)
        {
            return new Logger(this, IsEnabledForLevel);
        }

        private bool IsEnabledForLevel(LogLevel level)
        {
            return level <= Level && _loggers.Count > 0;
        }
    }
}
