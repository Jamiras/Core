using Jamiras.Components;

namespace Jamiras.Services
{
    public static class CoreServices
    {
        /// <summary>
        /// Registers default implementations for services defined in the core dll.
        /// </summary>
        public static void RegisterServices()
        {
            var repository = ServiceRepository.Instance;
            if (repository == null)
            {
                ServiceRepository.Reset();
                repository = ServiceRepository.Instance;
            }

            repository.RegisterService(typeof(DialogService));
            repository.RegisterService(typeof(FileSystemService));
            repository.RegisterService(typeof(PersistantDataRepository));
            repository.RegisterService(typeof(WindowSettingsRepository));
            repository.RegisterService(typeof(LogService));
        }
    }
}
