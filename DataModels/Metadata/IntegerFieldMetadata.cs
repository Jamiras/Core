using System;

namespace Jamiras.DataModels.Metadata
{
    public class IntegerFieldMetadata : FieldMetadata
    {
        protected IntegerFieldMetadata(string fieldName, FieldAttributes attributes = FieldAttributes.None)
            : this(fieldName, 0, Int32.MaxValue, attributes)
        {
        }

        public IntegerFieldMetadata(string fieldName, int minValue, int maxValue, FieldAttributes attributes = FieldAttributes.None)
            : base(fieldName, typeof(int), attributes)
        {
            MinimumValue = minValue;
            MaximumValue = maxValue;
        }

        public int MinimumValue { get; private set; }
        public int MaximumValue { get; private set; }
    }
}
