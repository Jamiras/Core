
namespace Jamiras.ViewModels.Converters
{
    public interface IConverter
    {
        /// <summary>
        /// Attempts to convert an object from the source type to the target type.
        /// </summary>
        /// <param name="value">Object to convert.</param>
        /// <returns><c>null</c> if the conversion succeeded, or and error message indicating why it failed.</returns>
        string Convert(ref object value);

        /// <summary>
        /// Attempts to convert an object from the target type to the source type.
        /// </summary>
        /// <param name="value">Object to convert.</param>
        /// <returns><c>null</c> if the conversion succeeded, or and error message indicating why it failed.</returns>
        string ConvertBack(ref object value);
    }
}
