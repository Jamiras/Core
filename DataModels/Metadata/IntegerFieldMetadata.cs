using System;

namespace Jamiras.DataModels.Metadata
{
    public class IntegerFieldMetadata : FieldMetadata
    {
        internal IntegerFieldMetadata(string fieldName, InternalFieldAttributes attributes)
            : this(fieldName, 0, Int32.MaxValue, (FieldAttributes)attributes)
        {
        }

        public IntegerFieldMetadata(string fieldName, int minValue, int maxValue, FieldAttributes attributes = FieldAttributes.None)
            : base(fieldName, (InternalFieldAttributes)attributes)
        {
            MinimumValue = minValue;
            MaximumValue = maxValue;
        }

        public int MinimumValue { get; private set; }
        public int MaximumValue { get; private set; }

        public override string Validate(ModelBase model, object value)
        {
            if (value is int)
            {
                int iValue = (int)value;
                if (iValue < MinimumValue || iValue > MaximumValue)
                {
                    if (MaximumValue == Int32.MaxValue)
                        return "{0} must be greater than " + MinimumValue + '.';

                    if (MinimumValue == 0)
                        return "{0} must be less than " + MaximumValue + '.';

                    return "{0} must be between " + MinimumValue + " and " + MaximumValue + '.';
                }
            }

            return base.Validate(model, value);
        }
    }
}
