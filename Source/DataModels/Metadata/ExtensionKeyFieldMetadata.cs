namespace Jamiras.DataModels.Metadata
{
    /// <summary>
    /// An ExtensionKeyField is a foreign key to a parent in a relationship where the extension object may or may not exist for 
    /// the parent. Only one extension object may exist per parent. This results in an Outer join.
    /// </summary>
    public class ExtensionKeyFieldMetadata : ForeignKeyFieldMetadata
    {
        /// <summary>
        /// Constructs a new parent key field metadata.
        /// </summary>
        /// <param name="fieldName">The foreign key field.</param>
        /// <param name="parentKeyField">The metadata describing the primary key of the parent record.</param>
        public ExtensionKeyFieldMetadata(string fieldName, FieldMetadata parentKeyField)
            : base(fieldName, parentKeyField, FieldAttributes.Required | (FieldAttributes)InternalFieldAttributes.PrimaryKey)
        {
        }
    }
}
