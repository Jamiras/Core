using System;

namespace Jamiras.Database
{
    /// <summary>
    /// Defines an interface for interacting with a database.
    /// </summary>
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
        /// Executes a query.
        /// </summary>
        /// <param name="query">The query to execute.</param>
        /// <returns>A query result row enumerator.</returns>
        IDatabaseQuery PrepareQuery(QueryBuilder query);

        /// <summary>
        /// Prepares a command that has bound values.
        /// </summary>
        /// <param name="command">Command to execute.</param>
        /// <returns>Helper object for binding tokens and executing the command.</returns>
        IDatabaseCommand PrepareCommand(string command);

        /// <summary>
        /// Executes an update or insert command.
        /// </summary>
        /// <param name="command">Command to execute.</param>
        /// <returns>Number of affected rows.</returns>
        int ExecuteCommand(string command);

        /// <summary>
        /// Executes an update or insert command.
        /// </summary>
        /// <param name="query">Command to execute.</param>
        /// <returns>Number of affected rows.</returns>
        int ExecuteCommand(QueryBuilder query);

        /// <summary>
        /// Escapes a value for a query string.
        /// </summary>
        /// <param name="value">Value to escape.</param>
        /// <returns>Escaped value.</returns>
        string Escape(string value);

        /// <summary>
        /// Prepares a Date for a query string.
        /// </summary>
        /// <param name="date">Value to escape.</param>
        /// <returns>Escaped value.</returns>
        string Escape(DateTime date);

        /// <summary>
        /// Constructs a <see cref="QueryBuilder"/> for making a database-specific query.
        /// </summary>
        /// <returns>The <see cref="QueryBuilder"/> to build the query string from.</param>
        QueryBuilder CreateQueryBuilder();

        /// <summary>
        /// Gets the schema for the database.
        /// </summary>
        DatabaseSchema Schema { get; }
    }
}
