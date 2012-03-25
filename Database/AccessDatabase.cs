using System.Diagnostics;
using System.Text;

namespace Jamiras.Database
{
    [DebuggerDisplay("{_connection.DataSource}")]
    public class AccessDatabase : IDatabase
    {
        private AccessDatabase(System.Data.OleDb.OleDbConnection connection)
        {
            _connection = connection;
        }

        private System.Data.OleDb.OleDbConnection _connection;

        #region IDatabase Members

        /// <summary>
        /// Disconnects from the database.
        /// </summary>
        public void Disconnect()
        {
            if (_connection != null)
            {
                _connection.Close();
                _connection.Dispose();
                _connection = null;
            }
        }

        /// <summary>
        /// Executes a query.
        /// </summary>
        /// <param name="query">The query to execute.</param>
        /// <returns>A query result row enumerator.</returns>
        public IDatabaseQuery PrepareQuery(string query)
        {
            return new AccessDatabaseQuery(_connection, query);
        }

        /// <summary>
        /// Prepares a command that has bound values.
        /// </summary>
        /// <param name="command">Command to execute.</param>
        /// <returns>Helper object for binding tokens and executing the command.</returns>
        public IDatabaseCommand PrepareCommand(string command)
        {
            return new AccessDatabaseCommand(_connection, command);
        }

        /// <summary>
        /// Executes an update or insert command.
        /// </summary>
        /// <param name="command">Command to execute.</param>
        /// <returns>Number of affected rows.</returns>
        public int ExecuteCommand(string command)
        {
            using (System.Data.Common.DbCommand cmd = _connection.CreateCommand())
            {
                cmd.CommandText = command;
                return cmd.ExecuteNonQuery();
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

            int idx = value.IndexOf('\'');
            if (idx == -1)
                idx = value.IndexOf('[');
            if (idx == -1)
                return value;

            StringBuilder builder = new StringBuilder();
            foreach (char c in value)
            {
                if (c == '\'')
                    builder.Append("''");
                else if (c == '[')
                    builder.Append("[[]");
                else
                    builder.Append(c);
            }

            return builder.ToString();
        }

        #endregion

        /// <summary>
        /// Attempts to open an Access database.
        /// </summary>
        /// <param name="fileName">Path to the Access database.</param>
        /// <returns>An IDatabase handle to the database if successful, null if not.</returns>
        public static IDatabase Connect(string fileName)
        {
            string connectionString;
            if (fileName.EndsWith(".accdb", System.StringComparison.OrdinalIgnoreCase))
                connectionString = "Provider=Microsoft.ACE.OLEDB.12.0; Data Source=" + fileName;
            else
                connectionString = "Provider=Microsoft.Jet.OLEDB.4.0; Data Source=" + fileName;

            var connection = new System.Data.OleDb.OleDbConnection(connectionString);
            connection.Open();

            while (connection.State == System.Data.ConnectionState.Connecting)
                System.Threading.Thread.Sleep(100);

            if (connection.State == System.Data.ConnectionState.Open)
                return new AccessDatabase(connection);

            return null;
        }
    }
}
