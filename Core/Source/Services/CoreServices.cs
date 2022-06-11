using Jamiras.Components;
using Jamiras.Core.Services.Impl;
using Jamiras.DataModels.Metadata;
using System;

namespace Jamiras.Services
{
    /// <summary>
    /// Helper class for defining default service implementations.
    /// </summary>
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

            // explicitly instantiate and register the ExceptionDispatcher to hook up the UnhandledException handler
            var dispatcher = new ExceptionDispatcher();
            repository.RegisterInstance<IExceptionDispatcher>(dispatcher);
            dispatcher.SetExceptionHandler(DefaultExceptionHandler);

            repository.RegisterService(typeof(FileSystemService));
            repository.RegisterService(typeof(HttpRequestService));
            repository.RegisterService(typeof(PersistantDataRepository));
            repository.RegisterService(typeof(AsyncDispatcher));
            repository.RegisterService(typeof(LogService));
            repository.RegisterService(typeof(EventBus));
            repository.RegisterService(typeof(DataModelMetadataRepository));
            repository.RegisterService(typeof(HttpListener));
            repository.RegisterService(typeof(TimerService));
            repository.RegisterService(typeof(BrowserService));
        }

        private static void DefaultExceptionHandler(object sender, DispatchExceptionEventArgs e)
        {
            var innerException = e.Exception;
            while (innerException.InnerException != null)
                innerException = innerException.InnerException;

            try
            {
                var logService = ServiceRepository.Instance.FindService<ILogService>();
                var logger = logService.GetLogger("Jamiras.Core");
                logger.WriteError(innerException.Message + "\n" + innerException.StackTrace);
            }
            catch 
            {
                // ignore exception trying to log exception
            }
        }
    }
}
