using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Jamiras.Components;
using Jamiras.Database;

namespace Jamiras.DataModels.Metadata
{
    /// <summary>
    /// Metadata for a database-based model.
    /// </summary>
    public abstract class DatabaseModelMetadata : ModelMetadata, IDatabaseModelMetadata
    {
        /// <summary>
        /// Constructs a new <see cref="DatabaseModelMetadata"/>
        /// </summary>
        protected DatabaseModelMetadata()
        {
            _tableMetadata = EmptyTinyDictionary<string, List<int>>.Instance;
        }

        private ITinyDictionary<string, List<int>> _tableMetadata;
        private List<JoinDefinition> _joins;
        private string _queryString;
        private static int _nextKey = -100;

        /// <summary>
        /// Gets the token to use when setting a filter value to the query key.
        /// </summary>
        protected const string FilterValueToken = "@filterValue";

        /// <summary>
        /// Gets the property for the primary key of the record.
        /// </summary>
        public ModelProperty PrimaryKeyProperty { get; protected set; }

        /// <summary>
        /// Gets or sets whether a new model should be created by Query() if existing data is not found (instead of returning false).
        /// </summary>
        protected bool CreateNewModelIfQueryFails { get; set; }

        /// <summary>
        /// Gets the primary key value of a model.
        /// </summary>
        /// <param name="model">The model to get the primary key for.</param>
        /// <returns>The primary key of the model.</returns>
        public virtual int GetKey(ModelBase model)
        {
            if (PrimaryKeyProperty == null)
                throw new InvalidOperationException("Could not determine primary key for " + GetType().Name);

            return (int)model.GetValue(PrimaryKeyProperty);
        }

        /// <summary>
        /// Registers metadata for a <see cref="ModelProperty"/>.
        /// </summary>
        /// <param name="property">Property to register metadata for.</param>
        /// <param name="metadata">Metadata for the field.</param>
        protected override sealed void RegisterFieldMetadata(ModelProperty property, FieldMetadata metadata)
        {
            base.RegisterFieldMetadata(property, metadata);

            if ((metadata.Attributes & FieldAttributes.PrimaryKey) != 0 && PrimaryKeyProperty == null)
                PrimaryKeyProperty = property;

            var tableName = GetTableName(metadata.FieldName);

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
                _joins = new List<JoinDefinition>();

            var joinType = JoinType.Inner;
            if (PrimaryKeyProperty != null)
            {
                var fieldMetadata = GetFieldMetadata(PrimaryKeyProperty);
                var primaryKeyFieldName = fieldMetadata.FieldName;
                if (localKeyFieldName == primaryKeyFieldName)
                    joinType = JoinType.Outer;
            }

            _joins.Add(new JoinDefinition(localKeyFieldName, remoteKeyFieldName, joinType));
        }

        private string GetJoin(string primaryTableName, string relatedTableName, out ModelProperty joinProperty)
        {
            joinProperty = null;
            string joinFieldName = null;
            if (relatedTableName != primaryTableName && _joins != null)
            {
                foreach (var join in _joins)
                {
                    if (GetTableName(join.RemoteKeyFieldName) == relatedTableName)
                    {
                        List<int> propertyKeys;
                        if (_tableMetadata.TryGetValue(primaryTableName, out propertyKeys))
                        {
                            foreach (var propertyKey in propertyKeys)
                            {
                                var property = ModelProperty.GetPropertyForKey(propertyKey);
                                var fieldMetadata = GetFieldMetadata(property);
                                if (fieldMetadata.FieldName == join.LocalKeyFieldName)
                                {
                                    joinProperty = property;
                                    break;
                                }
                            }
                        }

                        if (joinProperty != null)
                        {
                            joinFieldName = join.RemoteKeyFieldName;
                            break;
                        }
                    }
                }
            }

            return joinFieldName;
        }

        /// <summary>
        /// Initializes default values for a new record.
        /// </summary>
        /// <param name="model">Model to initialize.</param>
        /// <param name="database">The database to populate from.</param>
        void IDatabaseModelMetadata.InitializeNewRecord(ModelBase model, IDatabase database)
        {
            if (PrimaryKeyProperty != null)
                model.SetValue(PrimaryKeyProperty, _nextKey--);

            InitializeNewRecord(model, database);
        }

        /// <summary>
        /// Initializes default values for a new record.
        /// </summary>
        /// <param name="model">Model to initialize.</param>
        /// <param name="database">The database to populate from.</param>
        protected virtual void InitializeNewRecord(ModelBase model, IDatabase database)
        {
        }

        /// <summary>
        /// Populates a model from a database.
        /// </summary>
        /// <param name="model">The uninitialized model to populate.</param>
        /// <param name="primaryKey">The primary key of the model to populate.</param>
        /// <param name="database">The database to populate from.</param>
        /// <returns><c>true</c> if the model was populated, <c>false</c> if not.</returns>
        public bool Query(ModelBase model, object primaryKey, IDatabase database)
        {
            if (_tableMetadata.Count == 0)
                return HandleFailedQuery(model, primaryKey, database);

            if (_queryString == null)
                _queryString = BuildQueryString(database);

            using (var query = database.PrepareQuery(_queryString))
            {
                query.Bind(FilterValueToken, primaryKey);

                if (!query.FetchRow())
                    return HandleFailedQuery(model, primaryKey, database);

                PopulateItem(model, database, query);
            }

            return true;
        }

        private bool HandleFailedQuery(ModelBase model, object primaryKey, IDatabase database)
        {
            if (!CreateNewModelIfQueryFails)
                return false;

            if (PrimaryKeyProperty != null)
                model.SetValue(PrimaryKeyProperty, primaryKey);

            InitializeNewRecord(model, database);

            var dataModel = model as DataModelBase;
            if (dataModel != null && dataModel.IsModified)
                dataModel.AcceptChanges();

            return true;
        }

        private string BuildQueryString(IDatabase database)
        {
            var query = BuildQueryExpression();

            if (PrimaryKeyProperty != null)
            {
                var fieldMetadata = GetFieldMetadata(PrimaryKeyProperty);
                query.Filters.Add(new FilterDefinition(fieldMetadata.FieldName, FilterOperation.Equals, FilterValueToken));
            }

            return database.BuildQueryString(query);
        }

        // internal for access from DatabaseModelCollectionMetadata
        internal QueryBuilder BuildQueryExpression()
        {
            var query = new QueryBuilder();
            foreach (var metadata in AllFieldMetadata.Values)
                query.Fields.Add(metadata.FieldName);

            if (_joins != null)
            {
                foreach (var join in _joins)
                    query.Joins.Add(join);
            }

            CustomizeQuery(query);

            return query;
        }

        /// <summary>
        /// Allows a subclass to modify the generated query before it is executed.
        /// </summary>
        protected virtual void CustomizeQuery(QueryBuilder query)
        {
        }

        // internal for access from DatabaseModelCollectionMetadata
        internal void PopulateItem(ModelBase model, IDatabase database, IDatabaseQuery query)
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

            InitializeExistingRecord(model, database);

            var dataModel = model as DataModelBase;
            if (dataModel != null && dataModel.IsModified)
                dataModel.AcceptChanges();
        }

        /// <summary>
        /// Initializes default values for a record populated from the database.
        /// </summary>
        /// <param name="model">Model to initialize.</param>
        /// <param name="database">The database to populate from.</param>
        protected virtual void InitializeExistingRecord(ModelBase model, IDatabase database)
        {
        }

        private static object GetQueryValue(IDatabaseQuery query, int index, FieldMetadata fieldMetadata)
        {
            if (query.IsColumnNull(index))
                return null;

            if (fieldMetadata is IntegerFieldMetadata)
                return query.GetInt32(index);

            if (fieldMetadata is StringFieldMetadata || fieldMetadata.Type == typeof(string))
                return query.GetString(index);

            if (fieldMetadata.Type == typeof(int))
                return query.GetInt32(index);

            if (fieldMetadata.Type == typeof(double))
                return Convert.ToDouble(query.GetFloat(index));

            if (fieldMetadata.Type == typeof(float))
                return query.GetFloat(index);

            if (fieldMetadata.Type == typeof(DateTime))
                return query.GetDateTime(index);

            if (fieldMetadata.Type == typeof(bool))
                return query.GetBool(index);

            if (fieldMetadata.Type == typeof(byte))
                return query.GetByte(index);

            throw new NotSupportedException(fieldMetadata.GetType().Name);
        }

        /// <summary>
        /// Converts a database value to a model value.
        /// </summary>
        /// <param name="property">Property to populate from the database.</param>
        /// <param name="fieldMetadata">Additional information about the field.</param>
        /// <param name="databaseValue">Value read from the database.</param>
        /// <returns>Value to store in the model.</returns>
        protected virtual object CoerceValueFromDatabase(ModelProperty property, FieldMetadata fieldMetadata, object databaseValue)
        {
            if (fieldMetadata is ForeignKeyFieldMetadata && databaseValue == null && property.PropertyType == typeof(int))
                return 0;

            if (property.PropertyType == typeof(Date))
            {
                if (databaseValue is DateTime)
                {
                    var dateTime = (DateTime)databaseValue;
                    return new Date(dateTime.Month, dateTime.Day, dateTime.Year);
                }

                Date date;
                if (databaseValue != null && Date.TryParse((string)databaseValue, out date))
                    return date;
                
                return Date.Empty;
            }

            return databaseValue;
        }

        /// <summary>
        /// Converts a database value to a model value.
        /// </summary>
        /// <param name="property">Property to populate from the database.</param>
        /// <param name="fieldMetadata">Additional information about the field.</param>
        /// <param name="modelValue">Value from the model.</param>
        /// <returns>Value to store in the database.</returns>
        protected virtual object CoerceValueToDatabase(ModelProperty property, FieldMetadata fieldMetadata, object modelValue)
        {
            if (fieldMetadata is ForeignKeyFieldMetadata && (int)modelValue == 0 && property.PropertyType == typeof(int))
                return null;

            if (property.PropertyType == typeof(Date))
            {
                var date = (Date)modelValue;
                if (date.IsEmpty)
                    return null;

                if (fieldMetadata.Type == typeof(DateTime))
                    return new DateTime(date.Year, date.Month, date.Day);

                return date.ToDataString();
            }

            return modelValue;
        }

        private static void AppendQueryValue(StringBuilder builder, object value, FieldMetadata fieldMetadata, IDatabase database)
        {
            if (value == null)
            {
                AppendQueryNull(builder);
            }
            else if (value is int || value.GetType().IsEnum)
            {
                var iVal = (int)value;
                if (iVal == 0 && fieldMetadata is ForeignKeyFieldMetadata)
                    AppendQueryNull(builder);
                else
                    builder.Append(iVal);
            }
            else if (value is string)
            {
                var sVal = (string)value;
                if (sVal.Length == 0)
                    AppendQueryNull(builder);
                else
                    builder.AppendFormat("'{0}'", database.Escape(sVal));
            }
            else if (value is double)
            {
                var dVal = (double)value;
                builder.Append(dVal);
            }
            else if (value is float)
            {
                var dVal = (float)value;
                builder.Append(dVal);
            }
            else if (value is DateTime)
            {
                var dttm = (DateTime)value;
                builder.AppendFormat("#{0}#", dttm);
            }
            else if (value is bool)
            {
                if ((bool)value)
                    builder.Append("YES");
                else
                    builder.Append("NO");
            }
            else
            {
                throw new NotSupportedException(fieldMetadata.GetType().Name);
            }
        }

        private static void AppendQueryNull(StringBuilder builder)
        {

            if (builder[builder.Length - 1] == '=')
            {
                for (int i = builder.Length - 8; i >= 0; i--)
                {
                    if (builder[i] == ' ' && builder[i + 6] == ' ' &&
                        Char.ToUpper(builder[i + 1]) == 'W' &&
                        Char.ToUpper(builder[i + 2]) == 'H' &&
                        Char.ToUpper(builder[i + 3]) == 'E' &&
                        Char.ToUpper(builder[i + 4]) == 'R' &&
                        Char.ToUpper(builder[i + 5]) == 'E')
                    {
                        builder.Length--;
                        if (builder[builder.Length - 1] != ' ')
                            builder.Append(' ');

                        builder.Append("IS NULL");
                        return;
                    }
                }
            }

            builder.Append("NULL");
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

            return CreateRows(model, database);
        }

        private bool IsNew(ModelBase model)
        {
            return (GetKey(model) < 0);
        }

        private static string GetTableName(string fieldName)
        {
            var idx = fieldName.IndexOf('.');
            return (idx > 0) ? fieldName.Substring(0, idx).ToLower() : String.Empty;
        }

        private static string GetFieldName(string fieldName)
        {
            var idx = fieldName.IndexOf('.');
            return (idx > 0) ? fieldName.Substring(idx + 1) : fieldName;
        }

        /// <summary>
        /// Creates rows in the database for a new model instance.
        /// </summary>
        /// <param name="model">The model to commit.</param>
        /// <param name="database">The database to commit to.</param>
        /// <returns><c>true</c> if the model was committed, <c>false</c> if not.</returns>
        protected virtual bool CreateRows(ModelBase model, IDatabase database)
        {
            if (_tableMetadata.Count == 1)
            {
                var enumerator = _tableMetadata.GetEnumerator();
                enumerator.MoveNext();
                return CreateRow(model, database, enumerator.Current.Key, enumerator.Current.Value, null, null);
            }

            string primaryTable = null;
            if (PrimaryKeyProperty != null)
            {
                var fieldMetadata = GetFieldMetadata(PrimaryKeyProperty);
                primaryTable = GetTableName(fieldMetadata.FieldName);

                List<int> tablePropertyKeys;
                if (_tableMetadata.TryGetValue(primaryTable, out tablePropertyKeys))
                {
                    if (!CreateRow(model, database, primaryTable, tablePropertyKeys, null, null))
                        return false;
                }
            }

            foreach (var kvp in _tableMetadata)
            {
                if (kvp.Key == primaryTable)
                    continue;

                ModelProperty joinProperty;
                string joinFieldName = GetJoin(primaryTable, kvp.Key, out joinProperty);

                if (!CreateRow(model, database, kvp.Key, kvp.Value, joinProperty, joinFieldName))
                    return false;
            }

            return true;
        }

        private bool CreateRow(ModelBase model, IDatabase database, string tableName, IEnumerable<int> tablePropertyKeys, ModelProperty joinProperty, string joinFieldName)
        {
            bool onlyDefaults = (joinFieldName != null);
            var properties = new List<ModelProperty>();
            var refreshProperties = new List<ModelProperty>();
            foreach (var propertyKey in tablePropertyKeys)
            {
                var property = ModelProperty.GetPropertyForKey(propertyKey);
                var fieldMetadata = GetFieldMetadata(property);
                if ((fieldMetadata.Attributes & FieldAttributes.GeneratedByCreate) == 0)
                {
                    if (onlyDefaults && model.GetValue(property) != property.DefaultValue)
                        onlyDefaults = false;

                    properties.Add(property);
                }

                if ((fieldMetadata.Attributes & FieldAttributes.RefreshAfterCommit) != 0)
                    refreshProperties.Add(property);
            }

            if (properties.Count == 0 || onlyDefaults)
                return true;

            var builder = new StringBuilder();
            builder.Append("INSERT INTO ");
            builder.Append(tableName);
            builder.Append(" (");

            if (joinFieldName != null)
            {
                builder.Append('[');
                builder.Append(GetFieldName(joinFieldName));
                builder.Append("], ");
            }

            foreach (var property in properties)
            {
                var fieldMetadata = GetFieldMetadata(property);
                var fieldName = GetFieldName(fieldMetadata.FieldName);
                builder.Append('[');
                builder.Append(fieldName);
                builder.Append("], ");
            }

            builder.Length -= 2;
            builder.Append(") VALUES (");

            if (joinFieldName != null)
            {
                var fieldMetadata = GetFieldMetadata(joinProperty);
                var value = model.GetValue(joinProperty);
                value = CoerceValueToDatabase(joinProperty, fieldMetadata, value);
                AppendQueryValue(builder, value, GetFieldMetadata(joinProperty), database);
                builder.Append(", ");
            }

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

            try
            {
                if (database.ExecuteCommand(builder.ToString()) == 0)
                    return false;

                if (refreshProperties.Count > 0)
                    RefreshAfterCommit(model, database, refreshProperties, properties);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void RefreshAfterCommit(ModelBase model, IDatabase database, IEnumerable<ModelProperty> refreshProperties, IEnumerable<ModelProperty> propertiesToMatch)
        {
            var builder = new StringBuilder();
            builder.Append("SELECT ");

            string tableName = null;
            foreach (var property in refreshProperties)
            {
                var fieldMetadata = GetFieldMetadata(property);
                if (fieldMetadata is AutoIncrementFieldMetadata)
                {
                    builder.Append("MAX(");
                    builder.Append(fieldMetadata.FieldName);
                    builder.Append(")");
                }
                else
                {
                    builder.Append(fieldMetadata.FieldName);
                }

                builder.Append(", ");

                if (tableName == null)
                    tableName = GetTableName(fieldMetadata.FieldName);
            }

            if (tableName == null)
                return;

            builder.Length -= 2;
            builder.Append(" FROM ");
            builder.Append(tableName);
            builder.Append(" WHERE ");

            foreach (var property in propertiesToMatch)
            {
                var fieldMetadata = GetFieldMetadata(property);
                var value = model.GetValue(property);
                value = CoerceValueToDatabase(property, fieldMetadata, value);
                builder.Append('[');

                var foreignKeyMetadata = fieldMetadata as ForeignKeyFieldMetadata;
                if (foreignKeyMetadata != null)
                {
                    var fieldName = fieldMetadata.FieldName;
                    if (fieldName.Length < tableName.Length + 1 || fieldName[tableName.Length] != '.' || !fieldName.StartsWith(tableName))
                        fieldName = foreignKeyMetadata.RelatedFieldName;

                    builder.Append(fieldName);
                }
                else
                {
                    builder.Append(fieldMetadata.FieldName);                    
                }

                builder.Append(']');
                builder.Append('=');
                AppendQueryValue(builder, value, fieldMetadata, database);
                builder.Append(" AND ");
            }
            builder.Length -= 5;

            var queryString = builder.ToString();
            using (var query = database.PrepareQuery(queryString))
            {
                if (query.FetchRow())
                {
                    int index = 0;
                    foreach (var property in refreshProperties)
                    {
                        var fieldMetadata = GetFieldMetadata(property);
                        var value = GetQueryValue(query, index, fieldMetadata);
                        value = CoerceValueFromDatabase(property, fieldMetadata, value);
                        model.SetValue(property, value);
                        index++;
                    }
                }
            }
        }

        /// <summary>
        /// Updates rows in the database for an existing model instance.
        /// </summary>
        /// <param name="model">The model to commit.</param>
        /// <param name="database">The database to commit to.</param>
        /// <returns><c>true</c> if the model was committed, <c>false</c> if not.</returns>
        protected virtual bool UpdateRows(ModelBase model, IDatabase database)
        {
            if (PrimaryKeyProperty == null)
                throw new InvalidOperationException("Cannot update a record without a primary key. If the record is a member of a collection, commit the collection instead of the record.");

            var dataModel = model as DataModelBase;
            if (dataModel != null && !dataModel.IsModified)
                return true;

            var primaryKeyMetadata = GetFieldMetadata(PrimaryKeyProperty);
            var primaryKeyFieldName = primaryKeyMetadata.FieldName;
            var primaryTable = GetTableName(primaryKeyFieldName);

            foreach (var kvp in _tableMetadata)
            {
                if (kvp.Key == primaryTable)
                {
                    if (!UpdateRow(model, database, kvp.Key, kvp.Value, PrimaryKeyProperty, primaryKeyFieldName))
                    {
                        var metadata = GetFieldMetadata(PrimaryKeyProperty);
                        if (!(metadata is ForeignKeyFieldMetadata))
                            return false;

                        if (!CreateRow(model, database, kvp.Key, kvp.Value, null, null))
                            return false;
                    }
                }
                else
                {
                    ModelProperty joinProperty;
                    string joinFieldName = GetJoin(primaryTable, kvp.Key, out joinProperty);
                    if (joinFieldName == null)
                        throw new InvalidOperationException("Cannot determine relationship between " + primaryTable + " and " + kvp.Key);

                    if (!UpdateRow(model, database, kvp.Key, kvp.Value, joinProperty, joinFieldName) &&
                        !CreateRow(model, database, kvp.Key, kvp.Value, joinProperty, joinFieldName))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool UpdateRow(ModelBase model, IDatabase database, string tableName, IEnumerable<int> tablePropertyKeys, ModelProperty whereProperty, string whereFieldName)
        {
            Debug.Assert(whereFieldName != null);

            var dataModel = model as DataModelBase;
            var modifiedProperties = new List<ModelProperty>();
            foreach (var propertyKey in tablePropertyKeys)
            {
                var property = ModelProperty.GetPropertyForKey(propertyKey);
                if (dataModel == null || dataModel.UpdatedPropertyKeys.Contains(propertyKey))
                    modifiedProperties.Add(property);
            }

            if (modifiedProperties.Count > 0)
            {
                var builder = new StringBuilder();
                builder.Append("UPDATE ");
                builder.Append(tableName);
                builder.Append(" SET ");

                foreach (var property in modifiedProperties)
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

                builder.Append(" WHERE ");
                builder.Append(whereFieldName);
                builder.Append("=");

                var whereFieldMetadata = GetFieldMetadata(whereProperty);
                var whereValue = model.GetValue(whereProperty);
                whereValue = CoerceValueToDatabase(whereProperty, whereFieldMetadata, whereValue);
                AppendQueryValue(builder, whereValue, whereFieldMetadata, database);

                try
                {
                    if (database.ExecuteCommand(builder.ToString()) != 1)
                        return false;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            var refreshProperties = new List<ModelProperty>();
            foreach (var propertyKey in tablePropertyKeys)
            {
                var property = ModelProperty.GetPropertyForKey(propertyKey);
                var fieldMetadata = GetFieldMetadata(property);
                if ((fieldMetadata.Attributes & FieldAttributes.RefreshAfterCommit) != 0)
                {
                    refreshProperties.Add(property);
                    break;
                }
            }

            if (refreshProperties.Count > 0)
                RefreshAfterCommit(model, database, refreshProperties, new[] { whereProperty });

            return true;
        }
    }
}
