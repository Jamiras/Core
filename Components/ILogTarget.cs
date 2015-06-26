namespace Jamiras.Components
{
    public interface ILogTarget
    {
        /// <summary>
        /// Writes a message to the log.
        /// </summary>
        void Write(string message);
    }
}
