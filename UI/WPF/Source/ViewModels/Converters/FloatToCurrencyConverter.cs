using Jamiras.Components;
using System;
using System.Windows.Data;

namespace Jamiras.ViewModels.Converters
{
    /// <summary>
    /// Converter for formating and parsing currency values.
    /// </summary>
    public class FloatToCurrencyConverter : IConverter, IValueConverter
    {
        /// <summary>
        /// Attempts to convert a numerical value to a currency value.
        /// </summary>
        /// <param name="value">Object to convert.</param>
        /// <returns>
        ///   <c>null</c> if the conversion succeeded, or and error message indicating why it failed.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="value"/> is not a numerical value.</exception>
        public string Convert(ref object value)
        {
            double dval = 0.0;

            if (value is float)
                dval = (double)(float)value;
            else if (value is double)
                dval = (double)value;
            else if (value is int)
                dval = (double)(int)value;
            else
                throw new ArgumentException(value.GetType().Name + " cannot be converted to a double", "value");

            value = String.Format("${0:F2}", dval);
            return null;
        }

        /// <summary>
        /// Attempts to convert a currency value to a double.
        /// </summary>
        /// <param name="value">Object to convert.</param>
        /// <returns>
        ///   <c>null</c> if the conversion succeeded, or and error message indicating why it failed.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="value"/>is not a string.</exception>
        public string ConvertBack(ref object value)
        {
            if (value == null)
                return null;

            var sVal = value as string;
            if (sVal == null)
                throw new ArgumentException(value.GetType().Name + " is not a string", "value");

            double dval;
            if (!Double.TryParse(sVal, out dval))
            {
                if (sVal[0] != '$' || !Double.TryParse(sVal.Substring(1), out dval))
                    return "{0} is not a valid currency value";
            }

            value = dval;
            return null;
        }

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
    }
}
