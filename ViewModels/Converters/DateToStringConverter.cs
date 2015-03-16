using System;
using System.Windows.Data;
using Jamiras.Components;

namespace Jamiras.ViewModels.Converters
{
    [ValueConversion(typeof(Date), typeof(string))]
    public class DateToStringConverter : IValueConverter, IConverter
    {
        public DateToStringConverter()
        {
            Format = "M/dd/yyyy";
        }

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
