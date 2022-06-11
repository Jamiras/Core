using System;
using System.Data.Odbc;
using System.Diagnostics;
using System.IO;
using System.Text;
using Jamiras.Components;
using Jamiras.Services;
using Jamiras.ViewModels;

namespace Jamiras.Database
{
    /// <summary>
    /// <see cref="IDatabase"/> implementation for Microsoft Access databases.
    /// </summary>
    [Export(typeof(IDatabase))]
    [DebuggerDisplay("{_connection.DataSource}")]
    public class AccessDatabase : IDatabase
    {
        private readonly ILogger _logger = Logger.GetLogger("AccessDatabase");
        private OdbcConnection _connection;

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
            return new AccessDatabaseQuery(_connection, query);
        }

        /// <summary>
        /// Executes a query.
        /// </summary>
        /// <param name="query">The query to execute.</param>
        /// <returns>A query result row enumerator.</returns>
        public IDatabaseQuery PrepareQuery(QueryBuilder query)
        {
            return PrepareQuery(BuildQueryString(query));
        }

        /// <summary>
        /// Prepares a command that has bound values.
        /// </summary>
        /// <param name="command">Command to execute.</param>
        /// <returns>Helper object for binding tokens and executing the command.</returns>
        public IDatabaseCommand PrepareCommand(string command)
        {
            _logger.WriteVerbose("Preparing query: {0}", command);
            return new AccessDatabaseCommand(_connection, command);
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
            catch (OdbcException ex)
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
        /// Attempts to open an Access database.
        /// </summary>
        /// <param name="fileName">Path to the Access database.</param>
        public bool Connect(string fileName)
        {
            _logger.Write("Opening database: {0}", fileName);

            // try newer driver first
            string connectionString = "Driver={Microsoft Access Driver (*.mdb, *.accdb)}; DBQ=" + fileName;
            var connection = new OdbcConnection(connectionString);
            try
            {
                connection.Open();
            }
            catch (OdbcException ex)
            {
                _logger.Write("Failed to open database: " + ex.Message);

                if (ex.Message.Contains("[IM002]"))
                {
                    if (IntPtr.Size != 4 && File.Exists(fileName))
                        MessageBoxViewModel.ShowMessage("Access driver not found - assuming 64-bit access driver not installed");

                    // https://knowledge.autodesk.com/support/autocad/learn-explore/caas/sfdcarticles/sfdcarticles/How-to-install-64-bit-Microsoft-Database-Drivers-alongside-32-bit-Microsoft-Office.html
                    // * download AccessDatabaseEngine_X64.exe from https://www.microsoft.com/en-us/download/details.aspx?displaylang=en&id=13255
                    // * run it with the /quiet option: > AccessDatabaseEngine_X64.exe /quiet
                    // * delete or rename the HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Office\14.0\Common\FilesPaths\mso.dll registry key
                }

                return false;
            }
            catch (InvalidOperationException ex)
            {
                _logger.Write("Failed to open database: " + ex.Message);

                // then try older driver
                connectionString = "Provider=Microsoft.Jet.OLEDB.4.0; Data Source=" + fileName;
                connection = new OdbcConnection(connectionString);
                connection.Open();
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
        /// Constructs a database-specific query string from a <see cref="QueryBuilder"/>.
        /// </summary>
        /// <param name="query">The <see cref="QueryBuilder"/> to build the query string from.</param>
        /// <returns>The query string.</returns>
        public string BuildQueryString(QueryBuilder query)
        {
            return QueryBuilder.BuildQueryString(query, Schema);
        }

        /// <summary>
        /// Gets or sets the schema for the database.
        /// </summary>
        public DatabaseSchema Schema { get; set; }
    }
}
