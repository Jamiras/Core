namespace Jamiras.DataModels.Metadata
{
    /// <summary>
    /// Metadata about a ParentKey field.
    /// </summary>
    /// <remarks>
    /// A ParentKeyField is a foreign key to a parent in a 1-to-many relationship where the parent is the "1" and 0 or more of the objects containing the ParentKeyField may exist for the parent.
    /// </remarks>
    public class ParentKeyFieldMetadata : ForeignKeyFieldMetadata
    {
        /// <summary>
        /// Constructs a new parent key field metadata.
        /// </summary>
        /// <param name="fieldName">The foreign key field.</param>
        /// <param name="parentKeyField">The metadata describing the primary key of the parent record.</param>
        public ParentKeyFieldMetadata(string fieldName, FieldMetadata parentKeyField)
            : base(fieldName, parentKeyField, FieldAttributes.Required)
        {
        }
    }
}
