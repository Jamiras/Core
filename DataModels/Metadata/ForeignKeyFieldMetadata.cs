using System;

namespace Jamiras.DataModels.Metadata
{
    /// <summary>
    /// Metadata about a foriegn key field.
    /// </summary>
    public class ForeignKeyFieldMetadata : IntegerFieldMetadata
    {
        /// <summary>
        /// Constructs a new foreign key field metadata.
        /// </summary>
        /// <param name="fieldName">The foreign key field.</param>
        /// <param name="relatedField">The metadata describing the related field.</param>
        /// <param name="attributes">Additional attributes of the field.</param>
        public ForeignKeyFieldMetadata(string fieldName, FieldMetadata relatedField, FieldAttributes attributes = FieldAttributes.None)
            : base(fieldName, (relatedField is IntegerFieldMetadata) ? ((IntegerFieldMetadata)relatedField).MinimumValue : 0, 
                              (relatedField is IntegerFieldMetadata) ? ((IntegerFieldMetadata)relatedField).MaximumValue : Int32.MaxValue, attributes)
        {
            RelatedField = relatedField;
        }

        /// <summary>
        /// Gets the metadata for the related field.
        /// </summary>
        public FieldMetadata RelatedField { get; private set; }
    }
}
