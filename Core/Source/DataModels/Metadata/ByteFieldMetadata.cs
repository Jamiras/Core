
namespace Jamiras.DataModels.Metadata
{
    /// <summary>
    /// Metadata about an integral numeric field that only supports values 0-255.
    /// </summary>
    public class ByteFieldMetadata : IntegerFieldMetadata
    {
        internal ByteFieldMetadata(string fieldName, InternalFieldAttributes attributes)
            : this(fieldName, 0, 255, (FieldAttributes)attributes)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ByteFieldMetadata"/> class.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="minValue">The minimum value.</param>
        /// <param name="maxValue">The maximum value.</param>
        /// <param name="attributes">Additional attributes of the field.</param>
        public ByteFieldMetadata(string fieldName, byte minValue, byte maxValue, FieldAttributes attributes = FieldAttributes.None)
            : base(fieldName, minValue, maxValue, attributes)
        {
        }
    }
}
