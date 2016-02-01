namespace Jamiras.DataModels.Metadata
{
    public class BooleanFieldMetadata : FieldMetadata
    {
        public BooleanFieldMetadata(string fieldName, FieldAttributes attributes = FieldAttributes.None)
            : base(fieldName, (InternalFieldAttributes)attributes)
        {
        }
    }
}
