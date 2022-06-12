using System;

namespace Jamiras.Database
{
    /// <summary>
    /// Defines a command to be executed against a database. See <see cref="IDatabase.PrepareCommand(string)"/>
    /// </summary>
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
