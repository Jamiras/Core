
namespace Jamiras.DataModels.Metadata
{
    public class AutoIncrementFieldMetadata : IntegerFieldMetadata
    {
        public AutoIncrementFieldMetadata(string fieldName)
            : base(fieldName, FieldAttributes.GeneratedByCreate | FieldAttributes.RefreshAfterCommit)
        {
        }
    }
}
