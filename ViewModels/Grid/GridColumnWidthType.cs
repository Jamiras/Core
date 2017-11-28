
namespace Jamiras.ViewModels.Grid
{
    /// <summary>
    /// Defines how a grid column will size itself.
    /// </summary>
    public enum GridColumnWidthType
    {
        /// <summary>
        /// Unspecified
        /// </summary>
        None = 0,

        /// <summary>
        /// Column will only take up as much space as needed.
        /// </summary>
        Auto,

        /// <summary>
        /// Column will take up as much space as is left.
        /// </summary>
        Fill,

        /// <summary>
        /// Column will take up exactly as many pixels as specified in the Width property of the ColumnDefinition.
        /// </summary>
        Fixed,
    }
}
