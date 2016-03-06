
namespace Jamiras.DataModels.Metadata
{
    public class ByteFieldMetadata : IntegerFieldMetadata
    {
        internal ByteFieldMetadata(string fieldName, InternalFieldAttributes attributes)
            : this(fieldName, 0, 255, (FieldAttributes)attributes)
        {
        }

        public ByteFieldMetadata(string fieldName, byte minValue, byte maxValue, FieldAttributes attributes = FieldAttributes.None)
            : base(fieldName, minValue, maxValue, attributes)
        {
        }
    }
}
