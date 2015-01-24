using System.Diagnostics;

namespace Jamiras.Database
{
    [DebuggerDisplay("{LocalKeyFieldName} => {RemoteKeyFieldName}")]
    public struct JoinDefinition
    {
        public JoinDefinition(string localKeyFieldName, string remoteKeyFieldName, JoinType joinType)
        {
            _localKeyFieldName = localKeyFieldName;
            _remoteKeyFieldName = remoteKeyFieldName;
            _joinType = joinType;
        }

        private readonly string _localKeyFieldName;
        private readonly string _remoteKeyFieldName;
        private readonly JoinType _joinType;

        /// <summary>
        /// Gets the table.column name of the field on the local table that is used in the join.
        /// </summary>
        public string LocalKeyFieldName
        {
            get { return _localKeyFieldName; }
        }

        /// <summary>
        /// Gets the table.column name of the field on the remote table that is used in the join.
        /// </summary>
        public string RemoteKeyFieldName
        {
            get { return _remoteKeyFieldName; }
        }

        /// <summary>
        /// Gets the type of join.
        /// </summary>
        public JoinType JoinType
        {
            get { return _joinType; }
        }
    }

    public enum JoinType
    {
        /// <summary>
        /// Undefined
        /// </summary>
        None = 0,

        /// <summary>
        /// Data must exist in the related table for a row to be returned.
        /// </summary>
        Inner,

        /// <summary>
        /// If data doesn't exist in the related table, NULLs will be returned for fields in that table.
        /// </summary>
        Outer,
    }
}
