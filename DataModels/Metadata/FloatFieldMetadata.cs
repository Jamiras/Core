namespace Jamiras.DataModels.Metadata
{
    /// <summary>
    /// Metadata about a non-integral numeric field.
    /// </summary>
    public class FloatFieldMetadata : FieldMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FloatFieldMetadata"/> class.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="attributes">Additional attributes of the field.</param>
        public FloatFieldMetadata(string fieldName, FieldAttributes attributes = FieldAttributes.None)
            : base(fieldName, (InternalFieldAttributes)attributes)
        {
        }
    }
}
