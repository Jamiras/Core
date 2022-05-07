using System.Diagnostics;

namespace Jamiras.Database
{
    /// <summary>
    /// Defines an alias for a table in a query.
    /// </summary>
    [DebuggerDisplay("{TableName} as {Alias}")]
    public struct AliasDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AliasDefinition"/> struct.
        /// </summary>
        /// <param name="alias">The alias for the table.</param>
        /// <param name="tableName">The actual name of the table.</param>
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
