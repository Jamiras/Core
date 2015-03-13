using Jamiras.Database;

namespace Jamiras.DataModels.Metadata
{
    internal interface IDatabaseModelMetadata
    {
        /// <summary>
        /// Gets the property for the primary key of the record.
        /// </summary>
        ModelProperty PrimaryKeyProperty { get; }

        /// <summary>
        /// Gets the primary key value of a model.
        /// </summary>
        /// <param name="model">The model to get the primary key for.</param>
        /// <returns>The primary key of the model.</returns>
        int GetKey(ModelBase model);

        /// <summary>
        /// Initializes default values for a new record.
        /// </summary>
        /// <param name="model">Model to initialize.</param>
        /// <param name="database">The database to populate from.</param>
        void InitializeNewRecord(ModelBase model, IDatabase database);

        /// <summary>
        /// Gets the metadata registered for a <see cref="ModelProperty"/>.
        /// </summary>
        /// <param name="property">The property to get the metadata for.</param>
        /// <returns>Requested metadata, <c>null</c> if not found.</returns>
        FieldMetadata GetFieldMetadata(ModelProperty property);

        /// <summary>
        /// Populates a model from a database.
        /// </summary>
        /// <param name="model">The uninitialized model to populate.</param>
        /// <param name="primaryKey">The primary key of the model to populate.</param>
        /// <param name="database">The database to populate from.</param>
        /// <returns><c>true</c> if the model was populated, <c>false</c> if not.</returns>
        bool Query(ModelBase model, object primaryKey, IDatabase database);

        /// <summary>
        /// Commits changes made to a model to a database.
        /// </summary>
        /// <param name="model">The model to commit.</param>
        /// <param name="database">The database to commit to.</param>
        /// <returns><c>true</c> if the model was committed, <c>false</c> if not.</returns>
        bool Commit(ModelBase model, IDatabase database);
    }
}
