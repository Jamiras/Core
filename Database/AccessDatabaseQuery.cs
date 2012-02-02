using System;
using System.Data.Common;
using System.Diagnostics;

namespace Jamiras.Database
{
    [DebuggerDisplay("{_command.CommandText}")]
    internal class AccessDatabaseQuery : IDatabaseQuery
    {
        public AccessDatabaseQuery(System.Data.OleDb.OleDbConnection connection, string query)
        {
            _command = connection.CreateCommand();
            _command.CommandText = query;
        }

        private readonly DbCommand _command;
        private DbDataReader _reader;

        #region IDatabaseQuery

        /// <summary>
        /// Fetches the next row of the query results.
        /// </summary>
        /// <returns>True if the next row was fetched, false if there are no more rows.</returns>
        public bool FetchRow()
        {
            if (_reader == null)
                _reader = _command.ExecuteReader();

            return _reader.Read();
        }

        /// <summary>
        /// Determines whether value of the column at the specified index is null.
        /// </summary>
        /// <param name="columnIndex">Index of column to examine.</param>
        /// <returns>True if the value of the column is null. False otherwise.</returns>
        public bool IsColumnNull(int columnIndex)
        {
            return _reader.IsDBNull(columnIndex);
        }

        /// <summary>
        /// Gets the value of the column at the specified index as an integer.
        /// </summary>
        /// <param name="columnIndex">Index of column to examine.</param>
        /// <returns>Value of the column as an integer.</returns>
        public int GetInt32(int columnIndex)
        {
            return _reader.GetInt32(columnIndex);
        }

        /// <summary>
        /// Gets the value of the column at the specified index as a long integer.
        /// </summary>
        /// <param name="columnIndex">Index of column to examine.</param>
        /// <returns>Value of the column as a long integer.</returns>
        public long GetInt64(int columnIndex)
        {
            return _reader.GetInt64(columnIndex);
        }

        /// <summary>
        /// Gets the value of the column at the specified index as a string.
        /// </summary>
        /// <param name="columnIndex">Index of column to examine.</param>
        /// <returns>Value of the column as a string.</returns>
        public string GetString(int columnIndex)
        {
            return _reader.GetString(columnIndex);
        }

        /// <summary>
        /// Gets the value of the column at the specified index as a DateTime.
        /// </summary>
        /// <param name="columnIndex">Index of column to examine.</param>
        /// <returns>Value of the column as a DateTime.</returns>
        public DateTime GetDateTime(int columnIndex)
        {
            return _reader.GetDateTime(columnIndex);
        }

        /// <summary>
        /// Gets the value of the column at the specified index as a boolean.
        /// </summary>
        /// <param name="columnIndex">Index of column to examine.</param>
        /// <returns>Value of the column as a boolean.</returns>
        public bool GetBool(int columnIndex)
        {
            return _reader.GetBoolean(columnIndex);
        }

        /// <summary>
        /// Gets the value of the column at the specified index as a float.
        /// </summary>
        /// <param name="columnIndex">Index of column to examine.</param>
        /// <returns>Value of the column as a float.</returns>
        public float GetFloat(int columnIndex)
        {
            Type columnType = _reader.GetFieldType(columnIndex);
            if (columnType == typeof(decimal))
                return (float)_reader.GetDecimal(columnIndex);
            
            if (columnType == typeof(double))
                return (float)_reader.GetDouble(columnIndex);
            
            return _reader.GetFloat(columnIndex);
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~AccessDatabaseQuery()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_reader != null)
            {
                _reader.Close();
                _reader.Dispose();
            }

            if (_command != null)
                _command.Dispose();
        }

        #endregion
    }
}
