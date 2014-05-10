using System;

namespace Jamiras.DataModels.Metadata
{
    /// <summary>
    /// Provides information about a field.
    /// </summary>
    public class FieldMetadata
    {
        /// <summary>
        /// Constructs a new <see cref="FieldMetadata"/>.
        /// </summary>
        /// <param name="fieldName">Mapped field name.</param>
        /// <param name="type">Mapped field type.</param>
        public FieldMetadata(string fieldName, Type type)
        {
            FieldName = fieldName;
            Type = type;
        }

        /// <summary>
        /// Constructs a new <see cref="FieldMetadata"/>.
        /// </summary>
        /// <param name="fieldName">Mapped field name.</param>
        /// <param name="type">Mapped field type.</param>
        public FieldMetadata(string fieldName, Type type, FieldAttributes attributes)
            : this(fieldName, type)
        {
            Attributes = attributes;
        }

        /// <summary>
        /// Gets the name of the field.
        /// </summary>
        public string FieldName { get; private set; }

        /// <summary>
        /// Gets the type of the field.
        /// </summary>
        public Type Type { get; private set; }

        /// <summary>
        /// Gets attributes of the field.
        /// </summary>
        public FieldAttributes Attributes { get; private set; }

        /// <summary>
        /// Determines whether or not a value is valid for a field.
        /// </summary>
        /// <param name="model">The model that would be affected.</param>
        /// <param name="value">The value that would be applied to the field.</param>
        /// <returns><c>String.Empty</c> if the value is value, or an error message indicating why the value is not valid.</returns>
        public virtual string Validate(ModelBase model, object value)
        {
            return String.Empty;
        }
    }
}
