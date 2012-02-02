using System;

namespace Jamiras.Database
{
    public interface IDatabaseQuery : IDisposable
    {
        /// <summary>
        /// Fetches the next row of the query results.
        /// </summary>
        /// <returns>True if the next row was fetched, false if there are no more rows.</returns>
        bool FetchRow();

        /// <summary>
        /// Determines whether value of the column at the specified index is null.
        /// </summary>
        /// <param name="columnIndex">Index of column to examine.</param>
        /// <returns>True if the value of the column is null. False otherwise.</returns>
        bool IsColumnNull(int columnIndex);

        /// <summary>
        /// Gets the value of the column at the specified index as an integer.
        /// </summary>
        /// <param name="columnIndex">Index of column to examine.</param>
        /// <returns>Value of the column as an integer.</returns>
        int GetInt32(int columnIndex);

        /// <summary>
        /// Gets the value of the column at the specified index as a long integer.
        /// </summary>
        /// <param name="columnIndex">Index of column to examine.</param>
        /// <returns>Value of the column as a long integer.</returns>
        long GetInt64(int columnIndex);

        /// <summary>
        /// Gets the value of the column at the specified index as a string.
        /// </summary>
        /// <param name="columnIndex">Index of column to examine.</param>
        /// <returns>Value of the column as a string.</returns>
        string GetString(int columnIndex);

        /// <summary>
        /// Gets the value of the column at the specified index as a DateTime.
        /// </summary>
        /// <param name="columnIndex">Index of column to examine.</param>
        /// <returns>Value of the column as a DateTime.</returns>
        DateTime GetDateTime(int columnIndex);

        /// <summary>
        /// Gets the value of the column at the specified index as a boolean.
        /// </summary>
        /// <param name="columnIndex">Index of column to examine.</param>
        /// <returns>Value of the column as a boolean.</returns>
        bool GetBool(int columnIndex);

        /// <summary>
        /// Gets the value of the column at the specified index as a float.
        /// </summary>
        /// <param name="columnIndex">Index of column to examine.</param>
        /// <returns>Value of the column as a float.</returns>
        float GetFloat(int columnIndex);
    }
}
