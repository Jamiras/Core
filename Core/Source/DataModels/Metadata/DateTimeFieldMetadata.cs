namespace Jamiras.DataModels.Metadata
{
    /// <summary>
    /// Metadata for a date/time field.
    /// </summary>
    public class DateTimeFieldMetadata : FieldMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DateTimeFieldMetadata"/> class.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="attributes">Additional attributes of the field.</param>
        public DateTimeFieldMetadata(string fieldName, FieldAttributes attributes = FieldAttributes.None)
            : base(fieldName, (InternalFieldAttributes)attributes)
        {
        }
    }
}
