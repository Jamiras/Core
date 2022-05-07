using System;

namespace Jamiras.DataModels.Metadata
{
    /// <summary>
    /// Attributes about a field.
    /// </summary>
    [Flags]
    public enum FieldAttributes
    {
        /// <summary>
        /// No special attributes.
        /// </summary>
        None = 0,

        /// <summary>
        /// The field is required.
        /// </summary>
        Required = (int)InternalFieldAttributes.Required,
    }

    /// <summary>
    /// Non-public attributes about a field.
    /// </summary>
    [Flags]
    internal enum InternalFieldAttributes
    {
        /// <summary>
        /// No special attributes.
        /// </summary>
        None = 0,

        /// <summary>
        /// The field uniquely identifies the object.
        /// </summary>
        PrimaryKey = 0x01,

        /// <summary>
        /// The field is required.
        /// </summary>
        Required = 0x02,

        /// <summary>
        /// Varies by field. See field-specific enum for mapping.
        /// </summary>
        Custom1 = 0x10,

        /// <summary>
        /// The field will not have a value until the object is committed.
        /// </summary>
        GeneratedByCreate = 0x40,

        /// <summary>
        /// The field is updated when the object is committed.
        /// </summary>
        RefreshAfterCommit = 0x80,
    }
}
