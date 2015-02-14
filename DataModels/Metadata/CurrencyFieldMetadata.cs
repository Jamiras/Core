
namespace Jamiras.DataModels.Metadata
{
    public class CurrencyFieldMetadata : FieldMetadata
    {
        public CurrencyFieldMetadata(string fieldName, FieldAttributes attributes = FieldAttributes.None)
            : base(fieldName, typeof(float), attributes)
        {
        }
    }
}
