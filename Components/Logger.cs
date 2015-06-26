using System;
using System.Text;
using Jamiras.Services;

namespace Jamiras.Components
{
    public class Logger : ILogger
    {
        private Logger()
        {
        }

        internal Logger(ILogService owner, Predicate<LogLevel> isEnabled)
        {
            _owner = owner;
            _isEnabled = isEnabled;
        }

        private ILogService _owner;
        private Predicate<LogLevel> _isEnabled;

        /// <summary>
        /// Gets whether or not the specified logging level is enabled.
        /// </summary>
        public bool IsEnabled(LogLevel level)
        {
            return _isEnabled(level);
        }

        private void Write(LogLevel level, string message, params object[] args)
        {
            var builder = new StringBuilder();

            if (_owner.IsTimestampLogged)
            {
                builder.Append(DateTime.Now.ToString("HH:mm:ss.fff"));
                builder.Append(' ');
            }

            switch (level)
            {
                case LogLevel.General:
                    builder.Append("GEN ");
                    break;
                case LogLevel.Verbose:
                    builder.Append("VER ");
                    break;
                case LogLevel.Warning:
                    builder.Append("WRN ");
                    break;
                case LogLevel.Error:
                    builder.Append("ERR ");
                    break;
            }

            if (args.Length > 0)
                builder.AppendFormat(message, args);
            else
                builder.Append(message);

            var logMessage = builder.ToString();

            foreach (var logger in _owner.Loggers)
                logger.Write(logMessage);
        }

        /// <summary>
        /// Writes a general message to the log.
        /// </summary>
        public void Write(string message)
        {
            if (IsEnabled(LogLevel.General))
                Write(LogLevel.General, message);
        }

        /// <summary>
        /// Writes a general message to the log.
        /// </summary>
        public void Write(string message, params object[] args)
        {
            if (IsEnabled(LogLevel.General))
                Write(LogLevel.General, message, args);
        }

        /// <summary>
        /// Writes a verbose message to the log.
        /// </summary>
        public void WriteVerbose(string message)
        {
            if (IsEnabled(LogLevel.Verbose))
                Write(LogLevel.Verbose, message);
        }

        /// <summary>
        /// Writes a verbose message to the log.
        /// </summary>
        public void WriteVerbose(string message, params object[] args)
        {
            if (IsEnabled(LogLevel.Verbose))
                Write(LogLevel.Verbose, message, args);
        }

        /// <summary>
        /// Writes a warning message to the log.
        /// </summary>
        public void WriteWarning(string message)
        {
            if (IsEnabled(LogLevel.Warning))
                Write(LogLevel.Warning, message);
        }

        /// <summary>
        /// Writes a warning message to the log.
        /// </summary>
        public void WriteWarning(string message, params object[] args)
        {
            if (IsEnabled(LogLevel.Warning))
                Write(LogLevel.Warning, message, args);
        }

        /// <summary>
        /// Writes an error message to the log.
        /// </summary>
        public void WriteError(string message)
        {
            if (IsEnabled(LogLevel.Error))
                Write(LogLevel.Error, message);
        }

        /// <summary>
        /// Writes an error message to the log.
        /// </summary>
        public void WriteError(string message, params object[] args)
        {
            if (IsEnabled(LogLevel.Error))
                Write(LogLevel.Error, message, args);
        }

        /// <summary>
        /// Gets the logger for a key.
        /// </summary>
        public static ILogger GetLogger(string key)
        {
            var logger = new Logger();
            logger._isEnabled = logger.InitializeLogger;
            return logger;
        }

        private bool InitializeLogger(LogLevel level)
        {
            try
            {
                _owner = ServiceRepository.Instance.FindService<ILogService>();
                _isEnabled = IsEnabledForLevel;
            }
            catch (Exception)
            {
                _isEnabled = l => false;
            }

            return IsEnabled(level);
        }

        private bool IsEnabledForLevel(LogLevel level)
        {
            return (level >= _owner.Level && _owner.Loggers.Count > 0);
        }
    }
}
