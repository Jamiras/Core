namespace Jamiras.DataModels.Metadata
{
    public class DateTimeFieldMetadata : FieldMetadata
    {
        public DateTimeFieldMetadata(string fieldName, FieldAttributes attributes = FieldAttributes.None)
            : base(fieldName, (InternalFieldAttributes)attributes)
        {
        }
    }
}
