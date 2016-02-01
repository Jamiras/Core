using System;

namespace Jamiras.DataModels.Metadata
{
    public class StringFieldMetadata : FieldMetadata
    {
        public StringFieldMetadata(string fieldName, int maxLength, StringFieldAttributes attributes = StringFieldAttributes.None)
            : base(fieldName, (InternalFieldAttributes)attributes)
        {
            MaxLength = maxLength;
        }

        public int MaxLength { get; private set; }

        public bool IsMultiline
        {
            get { return (((int)Attributes & (int)StringFieldAttributes.Multiline) != 0); }
        }

        public override string Validate(ModelBase model, object value)
        {
            string strValue = value as string;
            if (strValue != null && strValue.Length > MaxLength)
                return "{0} cannot exceed " + MaxLength + " characters.";

            return base.Validate(model, value);
        }
    }

    [Flags]
    public enum StringFieldAttributes
    {
        None = 0,
        Required = (int)InternalFieldAttributes.Required,
        Multiline = (int)InternalFieldAttributes.Custom1,
    }
}
