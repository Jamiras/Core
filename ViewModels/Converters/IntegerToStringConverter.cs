using System;

namespace Jamiras.ViewModels.Converters
{
    public class IntegerToStringConverter : IConverter
    {
        public string Convert(ref object value)
        {
            if (value is int)
            {
                value = ((int)value).ToString();
                return null;
            }
            
            if (value == null)
                return null;

            throw new ArgumentException(value.GetType().Name + " is not an integer", "value");
        }

        public string ConvertBack(ref object value)
        {
            if (value == null)
                return null;

            var sVal = value as string;
            if (sVal == null)
                throw new ArgumentException(value.GetType().Name + " is not a string", "value");

            int iVal;
            if (Int32.TryParse(sVal, out iVal))
            {
                value = iVal;
                return null;
            }

            return "{0} is not an integer";
        }
    }
}
