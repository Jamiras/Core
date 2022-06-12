using System;
using System.ComponentModel;
using Jamiras.Components;
using Jamiras.Services;

namespace Jamiras.Core.Services.Impl
{
    [Export(typeof(IAsyncDispatcher))]
    internal class AsyncDispatcher : IAsyncDispatcher
    {
        [ImportingConstructor]
        public AsyncDispatcher(IExceptionDispatcher exceptionDispatcher)
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
