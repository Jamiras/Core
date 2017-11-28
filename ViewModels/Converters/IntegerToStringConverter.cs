using System;

namespace Jamiras.ViewModels.Converters
{
    /// <summary>
    /// Converts back and forth between an integer and a string.
    /// </summary>
    public class IntegerToStringConverter : IConverter
    {
        /// <summary>
        /// Attempts to convert an integer to a string.
        /// </summary>
        /// <param name="value">Object to convert.</param>
        /// <returns>
        ///   <c>null</c> if the conversion succeeded, or and error message indicating why it failed.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="value"/>is not an integer.</exception>
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

        /// <summary>
        /// Attempts to convert a string to an integer.
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
