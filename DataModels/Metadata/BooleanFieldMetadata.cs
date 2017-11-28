namespace Jamiras.DataModels.Metadata
{
    /// <summary>
    /// Metadata about a true/false field.
    /// </summary>
    public class BooleanFieldMetadata : FieldMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BooleanFieldMetadata"/> class.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="attributes">Additional attributes of the field.</param>
        public BooleanFieldMetadata(string fieldName, FieldAttributes attributes = FieldAttributes.None)
            : base(fieldName, (InternalFieldAttributes)attributes)
        {
        }
    }
}
