using Jamiras.Components;
using Jamiras.Services;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Odbc;

namespace Jamiras.Database
{
    internal class AccessDatabaseCommand : IDatabaseCommand
    {
        public AccessDatabaseCommand(OdbcConnection connection, string query)
        {
            _command = connection.CreateCommand();
            _command.CommandText = query;
        }

        private readonly DbCommand _command;
        private Dictionary<string, string> _parameters;

        public int Execute()
        {
            if (_parameters != null)
            {
                var ordered = new List<KeyValuePair<int, string>>();
                var commandText = _command.CommandText;

                foreach (var kvp in _parameters)
                {
                    var index = commandText.IndexOf(kvp.Key);
                    while (index != -1)
                    {
                        ordered.Add(new KeyValuePair<int, string>(index, kvp.Key));
                        index = commandText.IndexOf(kvp.Key, index + 1);
                    }
                }

                ordered.Sort((l, r) => l.Key - r.Key);

                foreach (var kvp in ordered)
                {
                    DbParameter param = _command.CreateParameter();
                    param.DbType = System.Data.DbType.String;
                    param.ParameterName = kvp.Value;
                    param.Value = _parameters[kvp.Value];
                    _command.Parameters.Add(param);

                    commandText = commandText.Replace(kvp.Value, "?");
                }

                _command.CommandText = commandText;
            }

            try
            {
                return _command.ExecuteNonQuery();
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

        public void BindString(string token, string value)
        {
            if (_parameters == null)
                _parameters = new Dictionary<string, string>();
            _parameters[token] = value;
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
