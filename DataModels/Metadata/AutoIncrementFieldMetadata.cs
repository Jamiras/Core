using System;

namespace Jamiras.DataModels.Metadata
{
    /// <summary>
    /// Metadata about an integral numeric field that is assigned the next available value when the object is committed.
    /// </summary>
    public class AutoIncrementFieldMetadata : IntegerFieldMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AutoIncrementFieldMetadata"/> class.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        public AutoIncrementFieldMetadata(string fieldName)
            : base(fieldName, InternalFieldAttributes.GeneratedByCreate | InternalFieldAttributes.RefreshAfterCommit | InternalFieldAttributes.PrimaryKey)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoIncrementFieldMetadata"/> class.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="minValue">The minimum value.</param>
        public AutoIncrementFieldMetadata(string fieldName, int minValue)
            : base(fieldName, minValue, Int32.MaxValue, (FieldAttributes)(InternalFieldAttributes.GeneratedByCreate | InternalFieldAttributes.RefreshAfterCommit | InternalFieldAttributes.PrimaryKey))
        {
        }
    }
}
