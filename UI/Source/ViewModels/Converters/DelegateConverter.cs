using Jamiras.Components;
using System;

namespace Jamiras.ViewModels.Converters
{
    /// <summary>
    /// A generic two-way converter where the implementation is local (or anonymous) methods.
    /// </summary>
    public class DelegateConverter : IConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DelegateConverter"/> class.
        /// </summary>
        /// <param name="convert">The method to convert values from the source type to the target type.</param>
        /// <param name="convertBack">The method to convert values from the target type to the source type.</param>
        public DelegateConverter(Func<object, object> convert, Func<object, object> convertBack)
        {
            _convert = convert;
            _convertBack = convertBack;
        }

        private readonly Func<object, object> _convert;
        private readonly Func<object, object> _convertBack;

        /// <summary>
        /// Attempts to convert an object from the source type to the target type.
        /// </summary>
        /// <param name="value">Object to convert.</param>
        /// <returns>
        ///   <c>null</c> if the conversion succeeded, or and error message indicating why it failed.
        /// </returns>
        /// <exception cref="NotSupportedException">No conversion method was supplied.</exception>
        public string Convert(ref object value)
        {
            if (_convert == null)
                throw new NotSupportedException();

            try
            {
                value = _convert(value);
                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        /// <summary>
        /// Attempts to convert an object from the target type to the source type.
        /// </summary>
        /// <param name="value">Object to convert.</param>
        /// <returns>
        ///   <c>null</c> if the conversion succeeded, or and error message indicating why it failed.
        /// </returns>
        /// <exception cref="NotSupportedException">No conversion method was supplied.</exception>
        public string ConvertBack(ref object value)
        {
            if (_convertBack == null)
                throw new NotSupportedException();

            try
            {
                value = _convertBack(value);
                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
