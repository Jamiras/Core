using Jamiras.DataModels.Metadata;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Jamiras.Database
{
    /// <summary>
    /// Base class for defining a database table.
    /// </summary>
    [DebuggerDisplay("{TableName} Schema")]
    public abstract class TableSchema
    {
        private IEnumerable<FieldMetadata> _columns = Enumerable.Empty<FieldMetadata>();
        private IEnumerable<JoinDefinition> _joins = Enumerable.Empty<JoinDefinition>();
        private string _tableName;

        private static string GetTableName(string tableFieldName)
        {
            int index = tableFieldName.IndexOf('.');
            if (index >= 0)
                return tableFieldName.Substring(0, index);

            return tableFieldName;
        }

        /// <summary>
        /// Gets the name of the table.
        /// </summary>
        public string TableName
        {
            get { return _tableName; }
        }

        /// <summary>
        /// Gets the columns in the table.
        /// </summary>
        public IEnumerable<FieldMetadata> Columns
        {
            get { return _columns; }
            protected set 
            { 
                _columns = value;
                _tableName = GetTableName(_columns.First().FieldName);
            }
        }
    }
}
