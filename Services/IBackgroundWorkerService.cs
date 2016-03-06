using System;

namespace Jamiras.Services
{
    public interface IBackgroundWorkerService
    {
        void RunAsync(Action action);

        void InvokeOnUiThread(Action action);
    }
}
