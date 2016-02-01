namespace Jamiras.DataModels.Metadata
{
    public class DoubleFieldMetadata : FieldMetadata
    {
        public DoubleFieldMetadata(string fieldName, FieldAttributes attributes = FieldAttributes.None)
            : base(fieldName, (InternalFieldAttributes)attributes)
        {
        }
    }
}
