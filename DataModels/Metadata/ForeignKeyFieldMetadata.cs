using System;
using Jamiras.ViewModels.Converters;

namespace Jamiras.DataModels.Metadata
{
    public class ForeignKeyFieldMetadata : IntegerFieldMetadata
    {
        /// <summary>
        /// Constructs a new foreign key field metadata.
        /// </summary>
        /// <param name="fieldName">The foreign key field.</param>
        /// <param name="relatedField">The metadata describing the related field</param>
        public ForeignKeyFieldMetadata(string fieldName, FieldMetadata relatedField, FieldAttributes attributes = FieldAttributes.None)
            : base(fieldName, (relatedField is IntegerFieldMetadata) ? ((IntegerFieldMetadata)relatedField).MinimumValue : 0, 
                              (relatedField is IntegerFieldMetadata) ? ((IntegerFieldMetadata)relatedField).MaximumValue : Int32.MaxValue, attributes)
        {
            RelatedField = relatedField;
        }

        public FieldMetadata RelatedField { get; private set; }
    }
}
