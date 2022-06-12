using System.Windows;

namespace Jamiras.Services
{
    /// <summary>
    /// Defines a service for remembering window locations.
    /// </summary>
    public interface IWindowSettingsRepository
    {
        /// <summary>
        /// Stores the current window position/size in the repository.
        /// </summary>
        void RememberSettings(Window window);

        /// <summary>
        /// Resets the window position/size to the value stored in the repository.
        /// </summary>
        void RestoreSettings(Window window);
    }
}
