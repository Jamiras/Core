namespace Jamiras.DataModels.Metadata
{
    public class EnumFieldMetadata<T> : FieldMetadata
    {
        public EnumFieldMetadata(string fieldName, FieldAttributes attributes = FieldAttributes.None)
            : base(fieldName, (InternalFieldAttributes)attributes)
        {
        }
    }
}
