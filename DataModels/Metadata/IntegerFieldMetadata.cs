using System;

namespace Jamiras.DataModels.Metadata
{
    /// <summary>
    /// Metadata about an integer field.
    /// </summary>
    public class IntegerFieldMetadata : FieldMetadata
    {
        internal IntegerFieldMetadata(string fieldName, InternalFieldAttributes attributes)
            : this(fieldName, 0, Int32.MaxValue, (FieldAttributes)attributes)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IntegerFieldMetadata"/> class.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="minValue">The minimum value.</param>
        /// <param name="maxValue">The maximum value.</param>
        /// <param name="attributes">Additional attributes of the field.</param>
        public IntegerFieldMetadata(string fieldName, int minValue, int maxValue, FieldAttributes attributes = FieldAttributes.None)
            : base(fieldName, (InternalFieldAttributes)attributes)
        {
            MinimumValue = minValue;
            MaximumValue = maxValue;
        }

        /// <summary>
        /// Gets the minimum value.
        /// </summary>
        public int MinimumValue { get; private set; }

        /// <summary>
        /// Gets the maximum value.
        /// </summary>
        public int MaximumValue { get; private set; }

        /// <summary>
        /// Determines whether or not a value is valid for a field.
        /// </summary>
        /// <param name="model">The model that would be affected.</param>
        /// <param name="value">The value that would be applied to the field.</param>
        /// <returns>
        ///   <c>String.Empty</c> if the value is value, or an error message indicating why the value is not valid.
        /// </returns>
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
