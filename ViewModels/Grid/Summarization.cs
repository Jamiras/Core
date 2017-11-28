
namespace Jamiras.ViewModels.Grid
{
    /// <summary>
    /// Defines summarization operations that can be applied to some types of columns.
    /// </summary>
    public enum Summarization
    {
        /// <summary>
        /// No summarization.
        /// </summary>
        None = 0,

        /// <summary>
        /// The total value of items in the column.
        /// </summary>
        Total,
    }
}
