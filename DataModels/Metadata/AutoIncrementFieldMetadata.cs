namespace Jamiras.DataModels.Metadata
{
    public class AutoIncrementFieldMetadata : IntegerFieldMetadata
    {
        public AutoIncrementFieldMetadata(string fieldName)
            : base(fieldName, InternalFieldAttributes.GeneratedByCreate | InternalFieldAttributes.RefreshAfterCommit | InternalFieldAttributes.PrimaryKey)
        {
        }
    }
}
