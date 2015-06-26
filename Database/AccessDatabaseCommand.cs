using System;
using System.Data.Common;

namespace Jamiras.Database
{
    internal class AccessDatabaseCommand : IDatabaseCommand
    {
        public AccessDatabaseCommand(System.Data.OleDb.OleDbConnection connection, string query)
        {
            _command = connection.CreateCommand();
            _command.CommandText = query;
        }

        private readonly DbCommand _command;

        public int Execute()
        {
            return _command.ExecuteNonQuery();
        }

        public void BindString(string token, string value)
        {
            DbParameter param = _command.CreateParameter();
            param.DbType = System.Data.DbType.String;
            param.ParameterName = token;
            param.Value = value;

            _command.Parameters.Add(param);
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~AccessDatabaseCommand()
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
