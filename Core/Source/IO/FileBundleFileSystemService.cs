using Jamiras.Core.Services.Impl;
using Jamiras.Services;
using System;
using System.IO;

namespace Jamiras.IO
{
    /// <summary>
    /// Defines an <see cref="IFileSystemService"/> implementation that works with a <see cref="FileBundle"/>.
    /// </summary>
    public class FileBundleFileSystemService : IFileSystemService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileBundleFileSystemService"/> class.
        /// </summary>
        /// <param name="bundleFileName">Name of the bundle file.</param>
        public FileBundleFileSystemService(string bundleFileName)
        {
            _fileSystem = new FileSystemService();
            _bundleFileName = bundleFileName;
            _bundleFilePath = Path.GetDirectoryName(_bundleFileName);
        }

        private readonly string _bundleFileName;
        private readonly string _bundleFilePath;
        private FileBundle _bundle;
        private readonly IFileSystemService _fileSystem;

        private FileBundle Bundle
        {
            get { return _bundle ?? (_bundle = new FileBundle(_bundleFileName)); }
        }

        private string GetFullPath(string path)
        {
            if (path == null || path.Length < 2)
                return path;

            if (path[0] == '\\' || path[1] == ':')
                return path;

            return Path.Combine(_bundleFilePath, path);
        }

        #region IFileSystemService Members

        /// <summary>
        /// Creates an empty file at the specified location. Will overwrite any existing file.
        /// Caller is responsible for closing the <see cref="Stream" />.
        /// </summary>
        /// <param name="path">Path to file.</param>
        /// <returns>
        /// Stream that can be used to write to the file.
        /// </returns>
        public Stream CreateFile(string path)
        {
            return _fileSystem.CreateFile(path);
        }

        /// <summary>
        /// Opens the file at the specified location for read or read/write.
        /// </summary>
        /// <param name="path">Path to file.</param>
        /// <param name="mode">Whether to open the file for reading or read/write</param>
        /// <returns>
        /// Stream to read/write to the file, null if file not found.
        /// </returns>
        public Stream OpenFile(string path, OpenFileMode mode)
        {
            string fullPath = GetFullPath(path);
            if (_fileSystem.FileExists(fullPath))
                return _fileSystem.OpenFile(fullPath, mode);

            return Bundle.OpenFile(path, mode);
        }

        /// <summary>
        /// Determines whether or not a file exists at the specified location.
        /// </summary>
        /// <param name="path">Path to file.</param>
        /// <returns>
        ///   <c>true</c> if the file exists, <c>false</c> if not.
        /// </returns>
        public bool FileExists(string path)
        {
            string fullPath = GetFullPath(path);
            if (_fileSystem.FileExists(fullPath))
                return true;

            return Bundle.FileExists(path);
        }

        /// <summary>
        /// Determines whether or not a directory exists at the specified location.
        /// </summary>
        /// <param name="path">Path to directory.</param>
        /// <returns>
        ///   <c>true</c> if the file exists, <c>false</c> if not.
        /// </returns>
        public bool DirectoryExists(string path)
        {
            string fullPath = GetFullPath(path);
            if (_fileSystem.DirectoryExists(fullPath))
                return true;

            return Bundle.DirectoryExists(path);
        }

        /// <summary>
        /// Creates a directory exists at the specified location.
        /// </summary>
        /// <param name="path">Path to directory.</param>
        /// <returns>
        ///   <c>true</c> if the directory was created, <c>false</c> if not.
        /// </returns>
        public bool CreateDirectory(string path)
        {
            return _fileSystem.CreateDirectory(path);
        }

        /// <summary>
        /// Gets the size of the file
        /// </summary>
        /// <param name="path">Path to file.</param>
        /// <returns>
        /// The size of the file (in bytes)
        /// </returns>
        public long GetFileSize(string path)
        {
            if (_fileSystem.FileExists(path))
                return _fileSystem.GetFileSize(path);

            return Bundle.GetFileSize(path);
        }

        /// <summary>
        /// Gets the last time a file was modified.
        /// </summary>
        /// <param name="path">Path to file.</param>
        /// <returns>
        /// The <see cref="DateTime" /> the file was last modified.
        /// </returns>
        public DateTime GetFileLastModified(string path)
        {
            if (_fileSystem.FileExists(path))
                return _fileSystem.GetFileLastModified(path);

            return Bundle.GetModified(path);
        }

        #endregion
    }
}
