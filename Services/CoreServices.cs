using System;
using System.Reflection;
using System.Windows;
using Jamiras.Components;
using Jamiras.DataModels.Metadata;

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

            // explicitly instantiate and register the ExceptionDispatcher to hook up the UnhandledException handler
            var dispatcher = new ExceptionDispatcher();
            repository.RegisterInstance<IExceptionDispatcher>(dispatcher);
            dispatcher.SetExceptionHandler(DefaultExceptionHandler);

            repository.RegisterService(typeof(DialogService));
            repository.RegisterService(typeof(FileSystemService));
            repository.RegisterService(typeof(HttpRequestService));
            repository.RegisterService(typeof(PersistantDataRepository));
            repository.RegisterService(typeof(WindowSettingsRepository));
            repository.RegisterService(typeof(LogService));
            repository.RegisterService(typeof(DataModelMetadataRepository));
            repository.RegisterService(typeof(SoundPlayer));
        }

        private static void DefaultExceptionHandler(object sender, DispatchExceptionEventArgs e)
        {
            string title;
            try
            {
                var dialogService = ServiceRepository.Instance.FindService<IDialogService>();
                title = dialogService.MainWindow.Title;
            }
            catch (Exception)
            {
                try
                {
                    title = Application.Current.MainWindow.Title;
                }
                catch (Exception)
                {
                    try
                    {
                        title = Assembly.GetEntryAssembly().GetName().Name;
                    }
                    catch (Exception)
                    {
                        title = String.Empty;
                    }
                }
            }

            var innerException = e.Exception;
            while (innerException.InnerException != null)
                innerException = innerException.InnerException;

            if (title.Length > 0)
                title += " - ";

            if (e.IsUnhandled)
                title += "Unhandled ";
            title += innerException.GetType().Name;

            MessageBox.Show(innerException.Message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
