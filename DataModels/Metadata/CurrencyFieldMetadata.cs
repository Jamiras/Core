namespace Jamiras.DataModels.Metadata
{
    /// <summary>
    /// Metadata about a currency field.
    /// </summary>
    public class CurrencyFieldMetadata : FloatFieldMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CurrencyFieldMetadata"/> class.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="attributes">Additional attributes of the field.</param>
        public CurrencyFieldMetadata(string fieldName, FieldAttributes attributes = FieldAttributes.None)
            : base(fieldName, attributes)
        {
        }
    }
}
