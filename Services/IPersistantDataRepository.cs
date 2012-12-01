
namespace Jamiras.Services
{
    public interface IPersistantDataRepository
    {
        /// <summary>
        /// Gets a value from the repository.
        /// </summary>
        /// <param name="key">Unique identifier of value to retrieve.</param>
        /// <returns>Requested value, null if not found.</returns>
        string GetValue(string key);

        /// <summary>
        /// Sets a value in the repository.
        /// </summary>
        /// <param name="key">Unique identifier of value to set.</param>
        /// <param name="newValue">New value for entry.</param>
        void SetValue(string key, string newValue);

        /// <summary>
        /// Delays writing settings until <see cref="EndUpdate"/> is called.
        /// </summary>
        void BeginUpdate();

        /// <summary>
        /// Resumes writing settings.
        /// </summary>
        void EndUpdate();

        /// <summary>
        /// Gets the path where application-specific user data files should be stored.
        /// </summary>
        string ApplicationUserDataDirectory { get; }
    }
}
