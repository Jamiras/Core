using System;
using System.Diagnostics;
using Jamiras.ViewModels.Converters;

namespace Jamiras.DataModels.Metadata
{
    /// <summary>
    /// Provides information about a field.
    /// </summary>
    [DebuggerDisplay("{FieldName} {GetType().Name,nq}")] 
    public abstract class FieldMetadata
    {
        /// <summary>
        /// Constructs a new <see cref="FieldMetadata"/>.
        /// </summary>
        /// <param name="fieldName">Mapped field name.</param>
        protected FieldMetadata(string fieldName)
        {
            FieldName = fieldName;
        }

        /// <summary>
        /// Constructs a new <see cref="FieldMetadata"/>.
        /// </summary>
        /// <param name="fieldName">Mapped field name.</param>
        internal FieldMetadata(string fieldName, InternalFieldAttributes attributes)
            : this(fieldName)
        {
            Attributes = attributes;
        }

        /// <summary>
        /// Gets the name of the field.
        /// </summary>
        public string FieldName { get; private set; }

        /// <summary>
        /// Gets attributes of the field.
        /// </summary>
        internal InternalFieldAttributes Attributes { get; private set; }

        /// <summary>
        /// Gets or sets whether the field is required.
        /// </summary>
        public bool IsRequired 
        {
            get { return (Attributes & InternalFieldAttributes.Required) != 0; }
        }

        /// <summary>
        /// Determines whether or not a value is valid for a field.
        /// </summary>
        /// <param name="model">The model that would be affected.</param>
        /// <param name="value">The value that would be applied to the field.</param>
        /// <returns><c>String.Empty</c> if the value is value, or an error message indicating why the value is not valid.</returns>
        public virtual string Validate(ModelBase model, object value)
        {
            if (value == null && IsRequired)
                return "{0} is required.";

            return String.Empty;
        }
    }
}
