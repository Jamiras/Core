using System;

namespace Jamiras.DataModels.Metadata
{
    public class AutoIncrementFieldMetadata : IntegerFieldMetadata
    {
        public AutoIncrementFieldMetadata(string fieldName)
            : base(fieldName, InternalFieldAttributes.GeneratedByCreate | InternalFieldAttributes.RefreshAfterCommit | InternalFieldAttributes.PrimaryKey)
        {
        }

        public AutoIncrementFieldMetadata(string fieldName, int minValue)
            : base(fieldName, minValue, Int32.MaxValue, (FieldAttributes)(InternalFieldAttributes.GeneratedByCreate | InternalFieldAttributes.RefreshAfterCommit | InternalFieldAttributes.PrimaryKey))
        {
        }
    }
}
