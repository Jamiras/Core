
namespace Jamiras.ViewModels.Converters
{
    public interface IConverter
    {
        object Convert(object value);

        object ConvertBack(object value);
    }
}
