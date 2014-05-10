
namespace Jamiras.DataModels.Metadata
{
    public class StringFieldMetadata : FieldMetadata
    {
        public StringFieldMetadata(string fieldName, int maxLength)
            : base(fieldName, typeof(string))
        {
            MaxLength = maxLength;
        }

        public int MaxLength { get; private set; }
    }
}
