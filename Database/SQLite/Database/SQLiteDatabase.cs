using System;
using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System.Text;
using Jamiras.Components;
using Jamiras.Services;

namespace Jamiras.Database
{
    /// <summary>
    /// <see cref="IDatabase"/> implementation for Sqlite databases.
    /// </summary>
    [Export(typeof(IDatabase))]
    [DebuggerDisplay("{_connection.DataSource}")]
    public class SQLiteDatabase : IDatabase
    {
        private readonly ILogger _logger = Logger.GetLogger("SQLiteDatabase");
        private SqliteConnection _connection;

        /// <summary>
        /// Disconnects from the database.
        /// </summary>
        public void Disconnect()
        {
            if (_connection != null)
            {
                _logger.Write("Closing database: {0}", _connection.DataSource);

                _connection.Close();
                _connection.Dispose();
                _connection = null;

                _logger.WriteVerbose("Database closed");
            }
        }

        /// <summary>
        /// Executes a query.
        /// </summary>
        /// <param name="query">The query to execute.</param>
        /// <returns>A query result row enumerator.</returns>
        public IDatabaseQuery PrepareQuery(string query)
        {
            _logger.WriteVerbose("Preparing query: {0}", query);
            return new SQLiteDatabaseQuery(_connection, query);
        }

        /// <summary>
        /// Executes a query.
        /// </summary>
        /// <param name="query">The query to execute.</param>
        /// <returns>A query result row enumerator.</returns>
        public IDatabaseQuery PrepareQuery(QueryBuilder query)
        {
            return PrepareQuery(query.BuildQueryString());
        }

        /// <summary>
        /// Prepares a command that has bound values.
        /// </summary>
        /// <param name="command">Command to execute.</param>
        /// <returns>Helper object for binding tokens and executing the command.</returns>
        public IDatabaseCommand PrepareCommand(string command)
        {
            _logger.WriteVerbose("Preparing query: {0}", command);
            return new SQLiteDatabaseCommand(_connection, command);
        }

        /// <summary>
        /// Executes an update or insert command.
        /// </summary>
        /// <param name="command">Command to execute.</param>
        /// <returns>Number of affected rows.</returns>
        public int ExecuteCommand(string command)
        {
            _logger.WriteVerbose("Executing query: {0}", command);

            try
            {
                using (System.Data.Common.DbCommand cmd = _connection.CreateCommand())
                {
                    cmd.CommandText = command;
                    return cmd.ExecuteNonQuery();
                }
            }
            catch (SqliteException ex)
            {
                var dispatcher = ServiceRepository.Instance.FindService<IExceptionDispatcher>();
                if (dispatcher == null)
                    throw;

                if (!dispatcher.TryHandleException(ex))
                    throw;

                return 0;
            }
        }

        /// <summary>
        /// Executes an update or insert command.
        /// </summary>
        /// <param name="query">Command to execute.</param>
        /// <returns>Number of affected rows.</returns>
        public int ExecuteCommand(QueryBuilder query)
        {
            var command = query.Filters.Count == 0 ? query.BuildInsertString() : query.BuildUpdateString();
            _logger.WriteVerbose("Executing query: {0}", command);

            try
            {
                var cmd = new SQLiteDatabaseCommand(_connection, command);
                query.Bind(cmd);
                return cmd.Execute();
            }
            catch (SqliteException ex)
            {
                var dispatcher = ServiceRepository.Instance.FindService<IExceptionDispatcher>();
                if (dispatcher == null)
                    throw;

                if (!dispatcher.TryHandleException(ex))
                    throw;

                return 0;
            }
        }

        /// <summary>
        /// Escapes a value for a query string.
        /// </summary>
        /// <param name="value">Value to escape.</param>
        /// <returns>Escaped value.</returns>
        public string Escape(string value)
        {
            if (value == null)
                return string.Empty;

            return EscapeString(value);
        }

        internal static string EscapeString(string value)
        {

            int idx = value.IndexOf('\'');
            if (idx == -1)
                return value;

            var builder = new StringBuilder();
            foreach (char c in value)
            {
                if (c == '\'')
                    builder.Append("''");
                else
                    builder.Append(c);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Prepares a Date for a query string.
        /// </summary>
        /// <param name="date">Value to escape.</param>
        /// <returns>Escaped value.</returns>
        public string Escape(DateTime date)
        {
            return String.Format("#{0}#", date.ToShortDateString());
        }

        /// <summary>
        /// Attempts to open an Sqlite database.
        /// </summary>
        /// <param name="fileName">Path to the Sqlite database.</param>
        public bool Connect(string fileName)
        {
            _logger.Write("Opening database: {0}", fileName);

            string connectionString = "Data Source=" + fileName;
            var connection = new SqliteConnection(connectionString);
            try
            {
                connection.Open();
            }
            catch (SqliteException ex)
            {
                _logger.Write("Failed to open database: " + ex.Message);

                return false;
            }

            while (connection.State == System.Data.ConnectionState.Connecting)
                System.Threading.Thread.Sleep(100);

            if (connection.State == System.Data.ConnectionState.Open)
            {
                _logger.Write("Database opened");
                _connection = connection;
            }
            else
            {
                _logger.Write("Failed to open database: " + connection.State);
            }

            return (connection.State == System.Data.ConnectionState.Open);
        }

        /// <summary>
        /// Constructs a <see cref="QueryBuilder"/> for making a database-specific query.
        /// </summary>
        /// <returns>The <see cref="QueryBuilder"/> to build the query string from.</param>
        public QueryBuilder CreateQueryBuilder()
        {
            return new SQLiteQueryBuilder(Schema);
        }

        /// <summary>
        /// Gets or sets the schema for the database.
        /// </summary>
        public DatabaseSchema Schema { get; set; }
    }
}
