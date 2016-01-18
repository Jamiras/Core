using Jamiras.ViewModels.Converters;

namespace Jamiras.DataModels.Metadata
{
    public class ForeignKeyFieldMetadata : IntegerFieldMetadata
    {
        /// <summary>
        /// Constructs a new foreign key field metadata.
        /// </summary>
        /// <param name="fieldName">The foreign key field.</param>
        /// <param name="relatedFieldName">The field on the related record that matches the foreign key value.</param>
        /// <param name="cardinality"></param>
        public ForeignKeyFieldMetadata(string fieldName, string relatedFieldName)
            : base(fieldName)
        {
            RelatedFieldName = relatedFieldName;

            Converter = ZeroToNullConverter.Instance;
        }

        public string RelatedFieldName { get; private set; }
    }
}
