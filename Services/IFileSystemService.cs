using System;
using System.IO;

namespace Jamiras.Services
{
    /// <summary>
    /// Service for interacting with the file system.
    /// </summary>
    public interface IFileSystemService
    {
        /// <summary>
        /// Creates an empty file at the specified location. Will overwrite any existing file. 
        /// Caller is responsible for closing the <see cref="Stream"/>.
        /// </summary>
        /// <param name="path">Path to file.</param>
        /// <returns>Stream that can be used to write to the file.</returns>
        Stream CreateFile(string path);

        /// <summary>
        /// Opens the file at the specified location for read or read/write.
        /// </summary>
        /// <param name="path">Path to file.</param>
        /// <param name="mode">Whether to open the file for reading or read/write</param>
        /// <returns>Stream to read/write to the file, null if file not found.</returns>
        Stream OpenFile(string path, OpenFileMode mode);

        /// <summary>
        /// Determines whether or not a file exists at the specified location.
        /// </summary>
        /// <param name="path">Path to file.</param>
        /// <returns><c>true</c> if the file exists, <c>false</c> if not.</returns>
        bool FileExists(string path);

        /// <summary>
        /// Determines whether or not a directory exists at the specified location.
        /// </summary>
        /// <param name="path">Path to directory.</param>
        /// <returns><c>true</c> if the file exists, <c>false</c> if not.</returns>
        bool DirectoryExists(string path);

        /// <summary>
        /// Creates a directory exists at the specified location.
        /// </summary>
        /// <param name="path">Path to directory.</param>
        /// <returns><c>true</c> if the directory was created, <c>false</c> if not.</returns>
        bool CreateDirectory(string path);

        /// <summary>
        /// Gets the size of the file
        /// </summary>
        /// <param name="path">Path to file.</param>
        /// <returns>The size of the file (in bytes)</returns>
        long GetFileSize(string path);

        /// <summary>
        /// Gets the last time a file was modified.
        /// </summary>
        /// <param name="path">Path to file.</param>
        /// <returns>The <see cref="DateTime"/> the file was last modified.</returns>
        DateTime GetFileLastModified(string path);
    }

    /// <summary>
    /// Intended use of opened file.
    /// </summary>
    public enum OpenFileMode
    {
        /// <summary>
        /// Unspecified
        /// </summary>
        None = 0,

        /// <summary>
        /// Read only
        /// </summary>
        Read,

        /// <summary>
        /// Read/write
        /// </summary>
        ReadWrite,
    }
}
