using Jamiras.Components;

namespace Jamiras.DataModels.Metadata
{
    /// <summary>
    /// Converter for switching back and forth from 0 to null for fields that are nullable in one place (i.e. database) and not nullable in another (i.e. data model).
    /// </summary>
    public class ZeroToNullConverter : IConverter
    {
        /// <summary>
        /// Gets the instance.
        /// </summary>
        public static ZeroToNullConverter Instance
        {
            get { return _instance; }
        }
        private static readonly ZeroToNullConverter _instance = new ZeroToNullConverter();

        /// <summary>
        /// Attempts to convert an object from the source type to the target type.
        /// </summary>
        /// <param name="value">Object to convert.</param>
        /// <returns>
        ///   <c>null</c> if the conversion succeeded, or and error message indicating why it failed.
        /// </returns>
        public string Convert(ref object value)
        {
            if (value is int && (int)value == 0)
                value = null;

            return null;
        }

        /// <summary>
        /// Attempts to convert an object from the target type to the source type.
        /// </summary>
        /// <param name="value">Object to convert.</param>
        /// <returns>
        ///   <c>null</c> if the conversion succeeded, or and error message indicating why it failed.
        /// </returns>
        public string ConvertBack(ref object value)
        {
            if (value == null)
                value = 0;

            return null;
        }
    }
}
