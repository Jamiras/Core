using System;
using System.Collections.Generic;
using System.Linq;
using Jamiras.DataModels.Metadata;

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

        private static string GetTableName(string fieldName)
        {
            var idx = fieldName.IndexOf('.');
            return (idx > 0) ? fieldName.Substring(0, idx).ToLower() : String.Empty;
        }

        public JoinDefinition GetJoin(string sourceTableName, string joinedTableName)
        {
            var sourceSchema = GetTableSchema(sourceTableName);
            if (sourceSchema == null)
                throw new ArgumentException("No schema registered for " + sourceTableName, "sourceTableName");

            var relatedTableName = joinedTableName.ToLower();
            foreach (var column in sourceSchema.Columns.OfType<ForeignKeyFieldMetadata>())
            {
                if (column.IsRequired && GetTableName(column.RelatedField.FieldName) == relatedTableName)
                    return new JoinDefinition(column.FieldName, column.RelatedField.FieldName);
            }

            foreach (var column in sourceSchema.Columns.OfType<ForeignKeyFieldMetadata>())
            {
                if (!column.IsRequired && GetTableName(column.RelatedField.FieldName) == relatedTableName)
                    return new JoinDefinition(column.FieldName, column.RelatedField.FieldName, JoinType.Outer);
            }

            var joinedSchema = GetTableSchema(joinedTableName);
            if (joinedSchema == null)
                throw new ArgumentNullException("No schema registered for " + joinedTableName, "joinedTableName");

            relatedTableName = sourceTableName.ToLower();
            foreach (var column in joinedSchema.Columns.OfType<ForeignKeyFieldMetadata>())
            {
                if ((column is ExtensionKeyFieldMetadata || column is ParentKeyFieldMetadata) && GetTableName(column.RelatedField.FieldName) == relatedTableName)
                    return new JoinDefinition(column.RelatedField.FieldName, column.FieldName, JoinType.Outer);
            }

            foreach (var column in joinedSchema.Columns.OfType<ForeignKeyFieldMetadata>())
            {
                if (GetTableName(column.RelatedField.FieldName) == relatedTableName)
                    return new JoinDefinition(column.RelatedField.FieldName, column.FieldName, JoinType.Outer);
            }

            return new JoinDefinition();
        }
    }
}
