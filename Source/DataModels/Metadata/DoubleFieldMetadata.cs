namespace Jamiras.DataModels.Metadata
{
    /// <summary>
    /// Metadata about a high-precision non-integral numeric field.
    /// </summary>
    public class DoubleFieldMetadata : FieldMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DoubleFieldMetadata"/> class.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="attributes">Additional attributes of the field.</param>
        public DoubleFieldMetadata(string fieldName, FieldAttributes attributes = FieldAttributes.None)
            : base(fieldName, (InternalFieldAttributes)attributes)
        {
        }
    }
}
