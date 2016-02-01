namespace Jamiras.DataModels.Metadata
{
    /// <summary>
    /// A ParentKeyField is a foreign key to a parent in a 1-to-many relationship where the parent is the "1" and 0 or more of the objects containing the ParentKeyField may exist for the parent.
    /// </summary>
    public class ParentKeyFieldMetadata : ForeignKeyFieldMetadata
    {
        /// <summary>
        /// Constructs a new parent key field metadata.
        /// </summary>
        /// <param name="fieldName">The foreign key field.</param>
        /// <param name="relatedField">The metadata describing the primary key of the parent record.</param>
        public ParentKeyFieldMetadata(string fieldName, FieldMetadata parentKeyField)
            : base(fieldName, parentKeyField, FieldAttributes.Required)
        {
        }
    }
}
