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
        protected override void RegisterFieldMetadata(ModelProperty property, FieldMetadata metadata)
        {
            base.RegisterFieldMetadata(property, metadata);

            if ((metadata.Attributes & FieldAttributes.PrimaryKey) != 0 && PrimaryKeyProperty == null)
                PrimaryKeyProperty = property;

            if ((metadata.Attributes & FieldAttributes.RefreshAfterCommit) != 0)
                _refreshAfterCommit = true;

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
                _joins = new List<KeyValuePair<string, string>>();

            _joins.Add(new KeyValuePair<string, string>(localKeyFieldName, remoteKeyFieldName));
        }

        private string GetJoin(string primaryTableName, string relatedTableName, out ModelProperty joinProperty)
        {
            joinProperty = null;
            string joinFieldName = null;
            if (relatedTableName != primaryTableName && _joins != null)
            {
                foreach (var join in _joins)
                {
                    if (GetTableName(join.Value) == relatedTableName)
                    {
                        List<int> propertyKeys;
                        if (_tableMetadata.TryGetValue(primaryTableName, out propertyKeys))
                        {
                            foreach (var propertyKey in propertyKeys)
                            {
                                var property = ModelProperty.GetPropertyForKey(propertyKey);
                                var fieldMetadata = GetFieldMetadata(property);
                                if (fieldMetadata.FieldName == join.Key)
                                {
                                    joinProperty = property;
                                    break;
                                }
                            }
                        }

                        if (joinProperty != null)
                        {
                            joinFieldName = join.Value;
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
        public override void InitializeNewRecord(ModelBase model)
        {
            if (PrimaryKeyProperty != null)
                model.SetValue(PrimaryKeyProperty, _nextKey--);
        }

        /// <summary>
        /// Populates a model from a database.
        /// </summary>
        /// <param name="model">The uninitialized model to populate.</param>
        /// <param name="primaryKey">The primary key of the model to populate.</param>
        /// <param name="database">The database to populate from.</param>
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

            CustomizeQuery(queryExpression);

            return queryExpression;
        }

        /// <summary>
        /// Allows a subclass to modify the generated query before it is executed.
        /// </summary>
        protected virtual void CustomizeQuery(ModelQueryExpression queryExpression)
        {
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

            InitializeExistingRecord(model);
        }

        /// <summary>
        /// Initializes default values for a record populated from the database.
        /// </summary>
        /// <param name="model">Model to initialize.</param>
        protected virtual void InitializeExistingRecord(ModelBase model)
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
                Date date = (Date)modelValue;
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
                int iVal = (int)value;
                if (iVal == 0 && fieldMetadata is ForeignKeyFieldMetadata)
                    AppendQueryNull(builder);
                else
                    builder.Append(iVal);
            }
            else if (value is string)
            {
                string sVal = (string)value;
                if (sVal.Length == 0)
                    AppendQueryNull(builder);
                else
                    builder.AppendFormat("'{0}'", database.Escape(sVal));
            }
            else if (value is double || value is float)
            {
                double dVal = (double)value;
                builder.Append(dVal);
            }
            else if (value is DateTime)
            {
                DateTime dttm = (DateTime)value;
                builder.AppendFormat("'{0}'", dttm.ToShortDateString());
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
        /// Determines if the specified property is modified on a model.
        /// </summary>
        /// <param name="model">The model to examine.</param>
        /// <param name="property">The property to examine.</param>
        /// <returns><c>true</c> if the property has been modified on the model, <c>false</c> if not.</returns>
        protected static bool IsPropertyModified(DataModelBase model, ModelProperty property)
        {
            return (model.IsModified && model.UpdatedPropertyKeys.Contains(property.Key));
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
            }

            foreach (var kvp in _tableMetadata)
            {
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

                if (_refreshAfterCommit)
                    RefreshAfterCommit(model, database, tablePropertyKeys, properties);

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private void RefreshAfterCommit(ModelBase model, IDatabase database, IEnumerable<int> tablePropertyKeys, IEnumerable<ModelProperty> properties)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("SELECT ");

            string tableName = null;
            foreach (var propertyKey in tablePropertyKeys)
            {
                var property = ModelProperty.GetPropertyForKey(propertyKey);
                var fieldMetadata = GetFieldMetadata(property);
                if ((fieldMetadata.Attributes & FieldAttributes.GeneratedByCreate) != 0)
                {
                    builder.Append(fieldMetadata.FieldName);
                    builder.Append(", ");

                    if (tableName == null)
                        tableName = GetTableName(fieldMetadata.FieldName);
                }
            }

            if (builder.Length == 7)
                return;

            builder.Length -= 2;
            builder.Append(" FROM ");
            builder.Append(tableName);
            builder.Append(" WHERE ");

            foreach (var property in properties)
            {
                var fieldMetadata = GetFieldMetadata(property);
                var value = model.GetValue(property);
                value = CoerceValueToDatabase(property, fieldMetadata, value);
                builder.Append(fieldMetadata.FieldName);
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
                    foreach (var propertyKey in tablePropertyKeys)
                    {
                        var property = ModelProperty.GetPropertyForKey(propertyKey);
                        var fieldMetadata = GetFieldMetadata(property);
                        if ((fieldMetadata.Attributes & FieldAttributes.GeneratedByCreate) != 0)
                        {
                            var value = GetQueryValue(query, index, fieldMetadata);
                            value = CoerceValueFromDatabase(property, fieldMetadata, value);
                            model.SetValue(property, value);
                            index++;
                        }
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
            var dataModel = model as DataModelBase;
            if (dataModel != null && !dataModel.IsModified)
                return true;

            string primaryTable = null;
            string primaryKeyFieldName = null;
            if (PrimaryKeyProperty != null)
            {
                var metadata = GetFieldMetadata(PrimaryKeyProperty);
                primaryKeyFieldName = metadata.FieldName;
                primaryTable = GetTableName(primaryKeyFieldName);
            }

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

            if (whereFieldName != null)
            {
                builder.Append(" WHERE ");
                builder.Append(whereFieldName);
                builder.Append("=");

                var fieldMetadata = GetFieldMetadata(whereProperty);
                var value = model.GetValue(whereProperty);
                value = CoerceValueToDatabase(whereProperty, fieldMetadata, value);
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
