namespace Jamiras.Services
{
    /// <summary>
    /// Service for interacting with the default browser
    /// </summary>
    public interface IBrowserService
    {
        /// <summary>
        /// Opens the specified URL.
        /// </summary>
        void OpenUrl(string url);
    }
}
