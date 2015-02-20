
namespace Jamiras.DataModels.Metadata
{
    public class StringFieldMetadata : FieldMetadata
    {
        public StringFieldMetadata(string fieldName, int maxLength, FieldAttributes attributes)
            : this(fieldName, maxLength)
        {
            Attributes = attributes;
        }

        public StringFieldMetadata(string fieldName, int maxLength)
            : base(fieldName, typeof(string))
        {
            MaxLength = maxLength;
        }

        public int MaxLength { get; private set; }

        public bool IsRequired { get; set; }

        public override string Validate(ModelBase model, object value)
        {
            string strValue = value as string;
            if (strValue == null)
            {
                if (IsRequired)
                    return "{0} is required.";
            }
            else
            {
                if (strValue.Length > MaxLength)
                    return "{0} cannot exceed " + MaxLength + " characters.";
            }

            return base.Validate(model, value);
        }
    }
}
