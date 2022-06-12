using Jamiras.Components;
using Jamiras.Core.Services.Impl;
using Jamiras.DataModels.Metadata;
using Jamiras.UI.WPF.Services.Impl;
using System;
using System.Reflection;
using System.Windows;

namespace Jamiras.Services
{
    /// <summary>
    /// Helper class for defining default service implementations.
    /// </summary>
    public static class UIServices
    {
        /// <summary>
        /// Registers default implementations for services defined in the UI dll.
        /// </summary>
        public static void RegisterServices()
        {
            var repository = ServiceRepository.Instance;
            if (repository == null)
            {
                ServiceRepository.Reset();
                repository = ServiceRepository.Instance;

                CoreServices.RegisterServices();
            }

            Application.Current.Dispatcher.ShutdownStarted += DispatcherShutdownStarted;

            // explicitly instantiate and register the ExceptionDispatcher to hook up the UnhandledException handler
            var dispatcher = new ExceptionDispatcher();
            repository.RegisterInstance<IExceptionDispatcher>(dispatcher);
            dispatcher.SetExceptionHandler(DefaultExceptionHandler);

            repository.RegisterService(typeof(BackgroundWorkerService));
            repository.RegisterService(typeof(ClipboardService));
            repository.RegisterService(typeof(DialogService));
            repository.RegisterService(typeof(SoundPlayer));
            repository.RegisterService(typeof(WindowSettingsRepository));
        }

        private static void DispatcherShutdownStarted(object sender, EventArgs e)
        {
            ServiceRepository.Instance.Shutdown();
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

            if (title.Length > 0)
                title += " - ";

            if (e.IsUnhandled)
                title += "Unhandled ";
            title += innerException.GetType().Name;

            MessageBox.Show(innerException.Message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
