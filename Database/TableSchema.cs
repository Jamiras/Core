using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Jamiras.DataModels.Metadata;

namespace Jamiras.Database
{
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

        public string TableName
        {
            get { return _tableName; }
        }

        public IEnumerable<FieldMetadata> Columns
        {
            get { return _columns; }
            protected set 
            { 
                _columns = value;
                _tableName = GetTableName(_columns.First().FieldName);
            }
        }

        public IEnumerable<JoinDefinition> Joins
        {
            get { return _joins; }
            protected set { _joins = value; }
        }
    }
}
