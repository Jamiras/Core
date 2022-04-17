using System;
using System.Windows.Data;
using Jamiras.Components;

namespace Jamiras.ViewModels.Converters
{
    /// <summary>
    /// Converts between a date and a string.
    /// </summary>
    [ValueConversion(typeof(Date), typeof(string))]
    public class DateToStringConverter : IValueConverter, IConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DateToStringConverter"/> class.
        /// </summary>
        public DateToStringConverter()
        {
            Format = "M/dd/yyyy";
        }

        /// <summary>
        /// Gets or sets the string format. Default is 'M/dd/yyyy'
        /// </summary>
        public string Format { get; set; }

        object IValueConverter.Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Convert(ref value);
            return value;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            ConvertBack(ref value);
            return value;
        }

        /// <summary>
        /// Attempts to convert an object from a <see cref="Date"/> to a <see cref="string"/>.
        /// </summary>
        /// <param name="value">Object to convert.</param>
        /// <returns>
        ///   <c>null</c> if the conversion succeeded, or and error message indicating why it failed.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="value"/> is not a <see cref="Date"/></exception>
        public string Convert(ref object value)
        {
            if (value is Date)
            {
                var date = (Date)value;
                if (date.IsEmpty)
                    value = null;
                else
                    value = date.ToString(Format);

                return null;
            }

            throw new ArgumentException(value.GetType().Name + " is not a date", "value");
        }

        /// <summary>
        /// Attempts to convert an object from a <see cref="string"/> to a <see cref="Date"/>.
        /// </summary>
        /// <param name="value">Object to convert.</param>
        /// <returns>
        ///   <c>null</c> if the conversion succeeded, or and error message indicating why it failed.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="value"/> iss not a string</exception>
        public string ConvertBack(ref object value)
        {
            if (value == null)
            {
                value = Date.Empty;
                return null;
            }

            var sVal = value as string;
            if (sVal == null)
                throw new ArgumentException(value.GetType().Name + " is not a string", "value");

            if (String.IsNullOrEmpty(sVal))
            {
                value = Date.Empty;
                return null;
            }

            Date date;
            if (Date.TryParse(sVal, out date))
            {
                if (date.Year == 0)
                    date = new Date(date.Month, date.Day, Date.Today.Year);

                value = date;
                return null;
            }

            return "{0} is not a date";
        }
    }
}
