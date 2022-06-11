namespace Jamiras.Components
{
    /// <summary>
    /// Defines a destination for messages written to an <see cref="ILogger"/>
    /// </summary>
    public interface ILogTarget
    {
        /// <summary>
        /// Writes a message to the log.
        /// </summary>
        void Write(string message);
    }
}
