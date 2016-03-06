using Jamiras.ViewModels.Converters;

namespace Jamiras.DataModels.Metadata
{
    public class ZeroToNullConverter : IConverter
    {
        public static ZeroToNullConverter Instance
        {
            get { return _instance; }
        }
        private static readonly ZeroToNullConverter _instance = new ZeroToNullConverter();

        public string Convert(ref object value)
        {
            if (value is int && (int)value == 0)
                value = null;

            return null;
        }

        public string ConvertBack(ref object value)
        {
            if (value == null)
                value = 0;

            return null;
        }
    }
}
