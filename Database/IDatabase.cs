namespace Jamiras.Database
{
    public interface IDatabase
    {
        /// <summary>
        /// Disconnects from the database.
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Executes a query.
        /// </summary>
        /// <param name="query">The query to execute.</param>
        /// <returns>A query result row enumerator.</returns>
        IDatabaseQuery PrepareQuery(string query);

        /// <summary>
        /// Executes an update or insert command.
        /// </summary>
        /// <param name="command">Command to execute.</param>
        /// <returns>Number of affected rows.</returns>
        int ExecuteCommand(string command);

        /// <summary>
        /// Escapes a value for a query string.
        /// </summary>
        /// <param name="value">Value to escape.</param>
        /// <returns>Escaped value.</returns>
        string Escape(string value);
    }
}
