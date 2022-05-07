using System;
using System.ComponentModel;
using System.Windows.Threading;
using Jamiras.Components;

namespace Jamiras.Services
{
    [Export(typeof(IBackgroundWorkerService))]
    internal class BackgroundWorkerService: IBackgroundWorkerService
    {
        [ImportingConstructor]
        public BackgroundWorkerService(IExceptionDispatcher exceptionDispatcher)
        {
            _exceptionDispatcher = exceptionDispatcher;
        }

        private readonly IExceptionDispatcher _exceptionDispatcher;
        private Dispatcher _uiThreadDispatcher;

        public void RunAsync(Action action)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler((o, e) => action());
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler((o, e) =>
            {
                if (e.Error != null)
                    _exceptionDispatcher.TryHandleException(e.Error);
            });
            worker.RunWorkerAsync();
        }

        public void InvokeOnUiThread(Action action)
        {
            if (_uiThreadDispatcher == null)
                _uiThreadDispatcher = ServiceRepository.Instance.FindService<IDialogService>().MainWindow.Dispatcher;

            if (_uiThreadDispatcher.CheckAccess())
                action();
            else
                _uiThreadDispatcher.Invoke(action);
        }
    }
}
