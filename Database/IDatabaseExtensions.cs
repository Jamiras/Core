using System;

namespace Jamiras.Database
{
    public static class IDatabaseExtensions
    {
        #region PrepareQuery

        /// <summary>
        /// Executes a query.
        /// </summary>
        /// <param name="query">The query to execute.</param>
        /// <returns>A query result row enumerator.</returns>
        public static IDatabaseQuery PrepareQuery(this IDatabase database, string query, object arg0)
        {
            return database.PrepareQuery(String.Format(query, arg0));
        }

        /// <summary>
        /// Executes a query.
        /// </summary>
        /// <param name="query">The query to execute.</param>
        /// <returns>A query result row enumerator.</returns>
        public static IDatabaseQuery PrepareQuery(this IDatabase database, string query, object arg0, object arg1)
        {
            return database.PrepareQuery(String.Format(query, arg0, arg1));
        }

        /// <summary>
        /// Executes a query.
        /// </summary>
        /// <param name="query">The query to execute.</param>
        /// <returns>A query result row enumerator.</returns>
        public static IDatabaseQuery PrepareQuery(this IDatabase database, string query, object arg0, object arg1, object arg2)
        {
            return database.PrepareQuery(String.Format(query, arg0, arg1, arg2));
        }

        /// <summary>
        /// Executes a query.
        /// </summary>
        /// <param name="query">The query to execute.</param>
        /// <returns>A query result row enumerator.</returns>
        public static IDatabaseQuery PrepareQuery(this IDatabase database, string query, params object[] args)
        {
            return database.PrepareQuery(String.Format(query, args));
        }

        #endregion

        #region PrepareCommand

        /// <summary>
        /// Prepares a command that has bound values.
        /// </summary>
        /// <param name="query">Command to execute.</param>
        /// <returns>Helper object for binding tokens and executing the command.</returns>
        public static IDatabaseCommand PrepareCommand(this IDatabase database, string command, object arg0)
        {
            return database.PrepareCommand(String.Format(command, arg0));
        }

        /// <summary>
        /// Prepares a command that has bound values.
        /// </summary>
        /// <param name="query">Command to execute.</param>
        /// <returns>Helper object for binding tokens and executing the command.</returns>
        public static IDatabaseCommand PrepareCommand(this IDatabase database, string command, object arg0, object arg1)
        {
            return database.PrepareCommand(String.Format(command, arg0, arg1));
        }

        /// <summary>
        /// Prepares a command that has bound values.
        /// </summary>
        /// <param name="query">Command to execute.</param>
        /// <returns>Helper object for binding tokens and executing the command.</returns>
        public static IDatabaseCommand PrepareCommand(this IDatabase database, string command, object arg0, object arg1, object arg2)
        {
            return database.PrepareCommand(String.Format(command, arg0, arg1, arg2));
        }

        /// <summary>
        /// Prepares a command that has bound values.
        /// </summary>
        /// <param name="query">Command to execute.</param>
        /// <returns>Helper object for binding tokens and executing the command.</returns>
        public static IDatabaseCommand PrepareCommand(this IDatabase database, string command, params object[] args)
        {
            return database.PrepareCommand(String.Format(command, args));
        }

        #endregion

        #region ExecuteCommand

        /// <summary>
        /// Executes an update or insert command.
        /// </summary>
        /// <param name="command">Command to execute.</param>
        /// <returns>Number of affected rows.</returns>
        public static int ExecuteCommand(this IDatabase database, string command, object arg0)
        {
            return database.ExecuteCommand(String.Format(command, arg0));
        }

        /// <summary>
        /// Executes an update or insert command.
        /// </summary>
        /// <param name="command">Command to execute.</param>
        /// <returns>Number of affected rows.</returns>
        public static int ExecuteCommand(this IDatabase database, string command, object arg0, object arg1)
        {
            return database.ExecuteCommand(String.Format(command, arg0, arg1));
        }

        /// <summary>
        /// Executes an update or insert command.
        /// </summary>
        /// <param name="command">Command to execute.</param>
        /// <returns>Number of affected rows.</returns>
        public static int ExecuteCommand(this IDatabase database, string command, object arg0, object arg1, object arg2)
        {
            return database.ExecuteCommand(String.Format(command, arg0, arg1, arg2));
        }

        /// <summary>
        /// Executes an update or insert command.
        /// </summary>
        /// <param name="command">Command to execute.</param>
        /// <returns>Number of affected rows.</returns>
        public static int ExecuteCommand(this IDatabase database, string command, params object[] args)
        {
            return database.ExecuteCommand(String.Format(command, args));
        }

        #endregion
    }
}
