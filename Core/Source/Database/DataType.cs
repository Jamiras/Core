namespace Jamiras.Database
{
    /// <summary>
    /// Database data types
    /// </summary>
    public enum DataType
    {
        /// <summary>
        /// Unspecified.
        /// </summary>
        None = 0,

        /// <summary>
        /// A string.
        /// </summary>
        String,

        /// <summary>
        /// An integral number.
        /// </summary>
        Integer,

        /// <summary>
        /// A true/false.
        /// </summary>
        Boolean,

        /// <summary>
        /// A date.
        /// </summary>
        Date,

        /// <summary>
        /// A date and time.
        /// </summary>
        DateTime,

        /// <summary>
        /// A binding token.
        /// </summary>
        BindVariable,
    }
}
