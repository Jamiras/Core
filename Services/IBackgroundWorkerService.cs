using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jamiras.Services
{
    public interface IBackgroundWorkerService
    {
        void RunAsync(Action action);
    }
}
