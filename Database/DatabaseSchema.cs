using System;
using System.Collections.Generic;

namespace Jamiras.Database
{
    public class DatabaseSchema
    {
        public DatabaseSchema(IEnumerable<TableSchema> tables)
        {
            var schema = new List<TableSchema>(tables);
            schema.Sort((l, r) => String.Compare(l.TableName, r.TableName, StringComparison.OrdinalIgnoreCase));
            _tables = schema.ToArray();
        }

        private TableSchema[] _tables;

        public TableSchema GetTableSchema(string tableName)
        {
            int low = 0;
            int high = _tables.Length;

            while (low < high)
            {
                int mid = (low + high) / 2;

                int diff = String.Compare(tableName, _tables[mid].TableName, StringComparison.OrdinalIgnoreCase);
                if (diff == 0)
                    return _tables[mid];

                if (diff > 0)
                    low = mid + 1;
                else
                    high = mid;
            }

            return null;
        }
    }
}
