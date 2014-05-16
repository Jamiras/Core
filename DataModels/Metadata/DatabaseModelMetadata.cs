using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jamiras.Components;
using Jamiras.Database;

namespace Jamiras.DataModels.Metadata
{
    /// <summary>
    /// Base class for database-based metadata
    /// </summary>
    public abstract class DatabaseModelMetadata : ModelMetadata
    {
        /// <summary>
        /// Constructs a new <see cref="DatabaseModelMetadata"/>
        /// </summary>
        protected DatabaseModelMetadata()
        {
            _tableMetadata = EmptyTinyDictionary<string, List<int>>.Instance;
        }

        private ITinyDictionary<string, List<int>> _tableMetadata;
        private List<KeyValuePair<string, string>> _joins;
        private string _queryString;
        private static int _nextKey = -100;
        private bool _refreshAfterCommit;

        /// <summary>
        /// Gets the property for the primary key of the record.
        /// </summary>
        public ModelProperty PrimaryKeyProperty { get; protected set; }

        /// <summary>
        /// Gets the primary key value of a model.
        /// </summary>
        /// <param name="model">The model to get the primary key for.</param>
        /// <returns>The primary key of the model.</returns>
        public int GetKey(ModelBase model)
        {
            if (PrimaryKeyProperty == null)
                return 0;

            return (int)model.GetValue(PrimaryKeyProperty);
        }

        /// <summary>
        /// Registers metadata for a <see cref="ModelProperty"/>.
        /// </summary>
        /// <param name="property">Property to register metadata for.</param>
        /// <param name="metadata">Metadata for the field.</param>
        protected override void RegisterFieldMetadata(ModelProperty property, FieldMetadata metadata)
        {
            base.RegisterFieldMetadata(property, metadata);

            if ((metadata.Attributes & FieldAttributes.PrimaryKey) != 0 && PrimaryKeyProperty == null)
                PrimaryKeyProperty = property;

            if ((metadata.Attributes & FieldAttributes.RefreshAfterCommit) != 0)
                _refreshAfterCommit = true;

            var idx = metadata.FieldName.IndexOf('.');
            var tableName = (idx > 0) ? metadata.FieldName.Substring(0, idx).ToLower() : String.Empty;

            List<int> tableMetadata;
            if (!_tableMetadata.TryGetValue(tableName, out tableMetadata))
            {
                tableMetadata = new List<int>();
                _tableMetadata = _tableMetadata.AddOrUpdate(tableName, tableMetadata);
            }
            tableMetadata.Add(property.Key);

            _queryString = null;
        }

        /// <summary>
        /// Registers a join between two tables.
        /// </summary>
        /// <param name="localKeyFieldName">The key field on the primary table.</param>
        /// <param name="remoteKeyFieldName">The key field on the remote table.</param>
        protected void RegisterJoin(string localKeyFieldName, string remoteKeyFieldName)
        {
            if (_joins == null)
                _joins = new List<KeyValuePair<string, string>>();

            _joins.Add(new KeyValuePair<string, string>(localKeyFieldName, remoteKeyFieldName));
        }

        /// <summary>
        /// Initializes default values for a new record.
        /// </summary>
        /// <param name="model">Model to initialize.</param>
        protected override void InitializeNewRecord(ModelBase model)
        {
            if (PrimaryKeyProperty != null)
                model.SetValue(PrimaryKeyProperty, _nextKey--);
        }

        /// <summary>
        /// Populates a model from a database.
        /// </summary>
        /// <param name="model">The uninitialized model to populate.</param>
        /// <param name="primaryKeyValue">The primary key of the model to populate.</param>
        /// <param name="database">The database to populate from.</param>
        /// <param name="checkCache">Function to call to use already available models. May be <c>null</c>.</param>
        /// <returns><c>true</c> if the model was populated, <c>false</c> if not.</returns>
        public virtual bool Query(ModelBase model, object primaryKey, IDatabase database)
        {
            if (_queryString == null)
                _queryString = BuildQueryString();

            using (var query = database.PrepareQuery(_queryString))
            {
                query.Bind("@filterValue", primaryKey);

                if (!query.FetchRow())
                    return false;

                PopulateItem(model, query);
            }

            return true;
        }

        private string BuildQueryString()
        {
            var queryExpression = BuildQueryExpression();

            if (PrimaryKeyProperty != null)
            {
                var fieldMetadata = GetFieldMetadata(PrimaryKeyProperty);
                queryExpression.AddFilter(fieldMetadata.FieldName, "@filterValue");
            }

            return queryExpression.BuildQueryString();
        }

        internal ModelQueryExpression BuildQueryExpression()
        {
            var queryExpression = new ModelQueryExpression();
            foreach (var metadata in AllFieldMetadata.Values)
                queryExpression.AddQueryField(metadata.FieldName);

            if (_joins != null)
            {
                string primaryKeyFieldName = null;

                if (PrimaryKeyProperty != null)
                {
                    var fieldMetadata = GetFieldMetadata(PrimaryKeyProperty);
                    primaryKeyFieldName = fieldMetadata.FieldName;
                }

                foreach (var join in _joins)
                    queryExpression.AddJoin(join.Key, join.Value, join.Key == primaryKeyFieldName);
            }

            return queryExpression;
        }

        internal void PopulateItem(ModelBase model, IDatabaseQuery query)
        {
            int index = 0;
            foreach (var kvp in AllFieldMetadata)
            {
                var property = ModelProperty.GetPropertyForKey(kvp.Key);
                var value = GetQueryValue(query, index, kvp.Value);
                value = CoerceValueFromDatabase(property, kvp.Value, value);
                model.SetValueCore(property, value);
                index++;
            }
        }

        private object GetQueryValue(IDatabaseQuery query, int index, FieldMetadata fieldMetadata)
        {
            if (query.IsColumnNull(index))
                return null;

            if (fieldMetadata is IntegerFieldMetadata)
                return query.GetInt32(index);

            if (fieldMetadata is StringFieldMetadata || fieldMetadata.Type == typeof(string))
                return query.GetString(index);

            if (fieldMetadata.Type == typeof(int))
                return query.GetInt32(index);

            throw new NotSupportedException(fieldMetadata.GetType().Name);
        }

        /// <summary>
        /// Converts a database value to a model value.
        /// </summary>
        /// <param name="property">Property to populate from the database.</param>
        /// <param name="databaseValue">Value read from the database.</param>
        /// <returns>Value to store in the model.</returns>
        protected virtual object CoerceValueFromDatabase(ModelProperty property, FieldMetadata fieldMetadata, object databaseValue)
        {
            if (fieldMetadata is ForeignKeyFieldMetadata && databaseValue == null && property.PropertyType == typeof(int))
                return 0;

            return databaseValue;
        }

        /// <summary>
        /// Converts a database value to a model value.
        /// </summary>
        /// <param name="property">Property to populate from the database.</param>
        /// <param name="modelValue">Value from the model.</param>
        /// <returns>Value to store in the database.</returns>
        protected virtual object CoerceValueToDatabase(ModelProperty property, FieldMetadata fieldMetadata, object modelValue)
        {
            if (fieldMetadata is ForeignKeyFieldMetadata && (int)modelValue == 0 && property.PropertyType == typeof(int))
                return null;

            return modelValue;
        }

        private void AppendQueryValue(StringBuilder builder, object value, FieldMetadata fieldMetadata, IDatabase database)
        {
            if (value == null)
            {
                builder.Append("NULL");
            }
            else if (value is int || value.GetType().IsEnum)
            {
                int iVal = (int)value;
                if (iVal == 0 && fieldMetadata is ForeignKeyFieldMetadata)
                    builder.Append("NULL");
                else
                    builder.Append(iVal);
            }
            else if (value is string)
            {
                string sVal = (string)value;
                if (sVal.Length == 0)
                    builder.Append("NULL");
                else
                    builder.AppendFormat("'{0}'", database.Escape(sVal));
            }
            else
            {
                throw new NotSupportedException(fieldMetadata.GetType().Name);
            }
        }

        /// <summary>
        /// Commits changes made to a model to a database.
        /// </summary>
        /// <param name="model">The model to commit.</param>
        /// <param name="database">The database to commit to.</param>
        /// <returns><c>true</c> if the model was committed, <c>false</c> if not.</returns>
        public bool Commit(ModelBase model, IDatabase database)
        {
            if (!IsNew(model))
                return UpdateRows(model, database);

            if (!CreateRows(model, database))
                return false;

            if (_refreshAfterCommit)
                RefreshAfterCommit(model, database);

            return true;
        }

        private bool IsNew(ModelBase model)
        {
            return (GetKey(model) < 0);
        }

        /// <summary>
        /// Creates rows in the database for a new model instance.
        /// </summary>
        /// <param name="model">The model to commit.</param>
        /// <param name="database">The database to commit to.</param>
        /// <returns><c>true</c> if the model was committed, <c>false</c> if not.</returns>
        protected virtual bool CreateRows(ModelBase model, IDatabase database)
        {
            foreach (var kvp in _tableMetadata)
            {
                if (!CreateRow(model, database, kvp.Key, kvp.Value))
                    return false;
            }

            return true;
        }

        private bool CreateRow(ModelBase model, IDatabase database, string tableName, IEnumerable<int> tablePropertyKeys)
        {
            var properties = new List<ModelProperty>();
            foreach (var propertyKey in tablePropertyKeys)
            {
                var property = ModelProperty.GetPropertyForKey(propertyKey);
                var fieldMetadata = GetFieldMetadata(property);
                if ((fieldMetadata.Attributes & FieldAttributes.GeneratedByCreate) == 0)
                    properties.Add(property);
            }

            if (properties.Count == 0)
                return true;

            var builder = new StringBuilder();
            builder.Append("INSERT INTO ");
            builder.Append(tableName);
            builder.Append(" (");

            foreach (var property in properties)
            {
                var fieldMetadata = GetFieldMetadata(property);
                var idx = fieldMetadata.FieldName.IndexOf('.');
                var fieldName = (idx > 0) ? fieldMetadata.FieldName.Substring(idx + 1) : fieldMetadata.FieldName;

                builder.Append('[');
                builder.Append(fieldName);
                builder.Append("], ");
            }

            builder.Length -= 2;
            builder.Append(") VALUES (");

            foreach (var property in properties)
            {
                var fieldMetadata = GetFieldMetadata(property);
                var value = model.GetValue(property);
                value = CoerceValueToDatabase(property, fieldMetadata, value);
                AppendQueryValue(builder, value, GetFieldMetadata(property), database);
                builder.Append(", ");
            }

            builder.Length -= 2;
            builder.Append(')');

            return (database.ExecuteCommand(builder.ToString()) == 1);
        }

        private void RefreshAfterCommit(ModelBase model, IDatabase database)
        {

        }

        /// <summary>
        /// Updates rows in the database for an existing model instance.
        /// </summary>
        /// <param name="model">The model to commit.</param>
        /// <param name="database">The database to commit to.</param>
        /// <returns><c>true</c> if the model was committed, <c>false</c> if not.</returns>
        protected virtual bool UpdateRows(ModelBase model, IDatabase database)
        {
            var dataModel = model as DataModelBase;
            if (dataModel != null && !dataModel.IsModified)
                return true;

            string primaryTable = null;
            var foreignKeys = new List<ModelProperty>();
            if (PrimaryKeyProperty != null)
            {
                var fieldMetadata = GetFieldMetadata(PrimaryKeyProperty);
                var idx = fieldMetadata.FieldName.IndexOf('.');
                primaryTable = (idx > 0) ? fieldMetadata.FieldName.Substring(0, idx).ToLower() : String.Empty;

                List<int> keys;
                if (_tableMetadata.TryGetValue(primaryTable, out keys))
                {
                    foreach (int propertyKey in keys)
                    {
                        var property = ModelProperty.GetPropertyForKey(propertyKey);
                        var foreignKeyFieldMetadata = GetFieldMetadata(property) as ForeignKeyFieldMetadata;
                        if (foreignKeyFieldMetadata != null)
                            foreignKeys.Add(property);
                    }
                }
            }

            foreach (var kvp in _tableMetadata)
            {
                ModelProperty tableKeyProperty = null;
                if (kvp.Key == primaryTable)
                {
                    tableKeyProperty = PrimaryKeyProperty;
                }
                else
                {
                    foreach (var property in foreignKeys)
                    {
                        var fieldMetadata = (ForeignKeyFieldMetadata)GetFieldMetadata(property);
                        if (fieldMetadata.RelatedFieldName.StartsWith(kvp.Key) && fieldMetadata.RelatedFieldName[kvp.Key.Length] == '.')
                        {
                            tableKeyProperty = property;
                            break;
                        }
                    }
                }

                if (!UpdateRow(model, database, kvp.Key, kvp.Value, tableKeyProperty))
                    return false;
            }

            return true;
        }

        private bool UpdateRow(ModelBase model, IDatabase database, string tableName, IEnumerable<int> tablePropertyKeys, ModelProperty tableKeyProperty)
        {
            var dataModel = model as DataModelBase;
            var properties = new List<ModelProperty>();
            foreach (var propertyKey in tablePropertyKeys)
            {
                var property = ModelProperty.GetPropertyForKey(propertyKey);
                if (dataModel == null || dataModel.UpdatedPropertyKeys.Contains(propertyKey))
                    properties.Add(property);
            }

            if (properties.Count == 0)
                return true;

            var builder = new StringBuilder();
            builder.Append("UPDATE ");
            builder.Append(tableName);
            builder.Append(" SET ");

            foreach (var property in properties)
            {
                var fieldMetadata = GetFieldMetadata(property);

                builder.Append(fieldMetadata.FieldName);
                builder.Append('=');

                var value = model.GetValue(property);
                value = CoerceValueToDatabase(property, fieldMetadata, value);
                AppendQueryValue(builder, value, fieldMetadata, database);

                builder.Append(", ");
            }

            builder.Length -= 2;

            if (tableKeyProperty != null)
            {
                var fieldMetadata = GetFieldMetadata(tableKeyProperty);

                builder.Append(" WHERE ");
                builder.Append(fieldMetadata.FieldName);
                builder.Append("=");

                var value = model.GetValue(tableKeyProperty);
                value = CoerceValueToDatabase(tableKeyProperty, fieldMetadata, value);
                AppendQueryValue(builder, value, fieldMetadata, database);
            }

            try
            {
                return (database.ExecuteCommand(builder.ToString()) == 1);
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
