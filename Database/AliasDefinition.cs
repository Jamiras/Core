using System.Diagnostics;

namespace Jamiras.Database
{
    [DebuggerDisplay("{TableName} as {Alias}")]
    public struct AliasDefinition
    {
        public AliasDefinition(string alias, string tableName)
        {
            _alias = alias;
            _tableName = tableName;
        }

        private readonly string _alias;
        private readonly string _tableName;

        /// <summary>
        /// Gets the alias for the table.
        /// </summary>
        public string Alias
        {
            get { return _alias; }
        }

        /// <summary>
        /// Gets the real name of the table.
        /// </summary>
        public string TableName
        {
            get { return _tableName; }
        }
    }
}
