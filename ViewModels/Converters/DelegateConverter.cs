using System;

namespace Jamiras.ViewModels.Converters
{
    public class DelegateConverter : IConverter
    {
        public DelegateConverter(Func<object, object> convert, Func<object, object> convertBack)
        {
            _convert = convert;
            _convertBack = convertBack;
        }

        private readonly Func<object, object> _convert;
        private readonly Func<object, object> _convertBack;

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
