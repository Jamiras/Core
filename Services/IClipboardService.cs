namespace Jamiras.Services
{
    /// <summary>
    /// Service for interacting with the system clipboard
    /// </summary>
    public interface IClipboardService
    {
        /// <summary>
        /// Puts text data onto the clipboard.
        /// </summary>
        void SetData(string text);

        /// <summary>
        /// Gets the text data from the clipboard.
        /// </summary>
        /// <returns></returns>
        string GetText();
    }
}
