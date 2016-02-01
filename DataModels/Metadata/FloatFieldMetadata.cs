namespace Jamiras.DataModels.Metadata
{
    public class FloatFieldMetadata : FieldMetadata
    {
        public FloatFieldMetadata(string fieldName, FieldAttributes attributes = FieldAttributes.None)
            : base(fieldName, (InternalFieldAttributes)attributes)
        {
        }
    }
}
