using System;

namespace Jamiras.DataModels.Metadata
{
    public class EnumFieldMetadata<T> : FieldMetadata
    {
        public EnumFieldMetadata(string fieldName)
            : this(fieldName, typeof(int))
        {
        }

        public EnumFieldMetadata(string fieldName, Type fieldType)
            : base(fieldName, fieldType)
        {
        }
    }
}
