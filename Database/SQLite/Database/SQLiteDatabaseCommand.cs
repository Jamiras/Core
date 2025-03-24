using Jamiras.Components;
using Jamiras.Services;
using Microsoft.Data.Sqlite;
using System;

namespace Jamiras.Database
{
    internal class SQLiteDatabaseCommand : IDatabaseCommand
    {
        public SQLiteDatabaseCommand(SqliteConnection connection, string query)
        {
            _command = connection.CreateCommand();
            _command.CommandText = query;
        }

        private readonly SqliteCommand _command;

        public int Execute()
        {
            try
            {
                return _command.ExecuteNonQuery();
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

        public void BindString(string token, string value)
        {
            _command.Parameters.AddWithValue(token, value);
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~SQLiteDatabaseCommand()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_command != null)
                _command.Dispose();
        }

        #endregion
    }
}
