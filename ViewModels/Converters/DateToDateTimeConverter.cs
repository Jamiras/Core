using System;
using Jamiras.Components;

namespace Jamiras.ViewModels.Converters
{
    /// <summary>
    /// ViewModel converter to convert <see cref="Date"/>s to <see cref="DateTime"/>s.
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
            if (!(value is DateTime))
                return "Expecting DateTime, received " + ((value == null) ? "null" : value.GetType().FullName);

            var datetime = (DateTime)value;
            value = new Date(datetime.Month, datetime.Day, datetime.Year);
            return null;
        }
    }
}
