using System;
using Jamiras.Components;

namespace Jamiras.ViewModels.Converters
{
    /// <summary>
    /// ViewModel converter to convert a <see cref="Date"/>s to a nullable <see cref="DateTime"/>.
    /// </summary>
    public class DateToDateTimeConverter : IConverter
    {
        /// <summary>
        /// Attempts to convert an object from the source type to the target type.
        /// </summary>
        /// <param name="value">Object to convert.</param>
        /// <returns><c>null</c> if the conversion succeeded, or and error message indicating why it failed.</returns>
        public string Convert(ref object value)
        {
            if (!(value is Date))
                return "Expecting Date, received " + ((value == null) ? "null" : value.GetType().FullName);

            var date = (Date)value;
            if (date.IsEmpty)
                value = null;
            else
                value = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Local);

            return null;
        }

        /// <summary>
        /// Attempts to convert an object from the target type to the source type.
        /// </summary>
        /// <param name="value">Object to convert.</param>
        /// <returns><c>null</c> if the conversion succeeded, or and error message indicating why it failed.</returns>
        public string ConvertBack(ref object value)
        {
            if (value is DateTime)
            {
                var datetime = (DateTime)value;
                value = new Date(datetime.Month, datetime.Day, datetime.Year);
                return null;                
            }

            if (value == null)
            {
                value = Date.Empty;
                return null;
            }

            return "Expecting DateTime, received " + value.GetType().FullName;
        }
    }
}
