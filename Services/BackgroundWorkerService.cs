using System;
using System.ComponentModel;
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
    }
}
