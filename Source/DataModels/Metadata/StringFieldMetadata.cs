using System;

namespace Jamiras.DataModels.Metadata
{
    /// <summary>
    /// Metadata about a string field.
    /// </summary>
    public class StringFieldMetadata : FieldMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StringFieldMetadata"/> class.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="maxLength">The maximum length.</param>
        /// <param name="attributes">Additional attributes of the field.</param>
        public StringFieldMetadata(string fieldName, int maxLength, StringFieldAttributes attributes = StringFieldAttributes.None)
            : base(fieldName, (InternalFieldAttributes)attributes)
        {
            MaxLength = maxLength;
        }

        /// <summary>
        /// Gets the maximum length of data in the field.
        /// </summary>
        public int MaxLength { get; private set; }

        /// <summary>
        /// Gets whether or not the field supports newlines.
        /// </summary>
        public bool IsMultiline
        {
            get { return (((int)Attributes & (int)StringFieldAttributes.Multiline) != 0); }
        }

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
            string strValue = value as string;
            if (strValue != null && strValue.Length > MaxLength)
                return "{0} cannot exceed " + MaxLength + " characters.";

            return base.Validate(model, value);
        }
    }

    /// <summary>
    /// Attributes about a string field.
    /// </summary>
    [Flags]
    public enum StringFieldAttributes
    {
        /// <summary>
        /// No special attributes.
        /// </summary>
        None = 0,

        /// <summary>
        /// The field is required.
        /// </summary>
        Required = (int)InternalFieldAttributes.Required,

        /// <summary>
        /// The field supports newlines.
        /// </summary>
        Multiline = (int)InternalFieldAttributes.Custom1,
    }
}
