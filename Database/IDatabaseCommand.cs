using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jamiras.Database
{
    public interface IDatabaseCommand : IDisposable
    {
        /// <summary>
        /// Binds a large string to a token in the command
        /// </summary>
        /// <param name="token">Token to bind "@token"</param>
        /// <param name="value">Value to bind to token</param>
        void BindString(string token, string value);

        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <returns>Number of rows affected.</returns>
        int Execute();
    }
}
