using System.IO;
using Jamiras.Services;

namespace Jamiras.IO
{
    public class FileBundleFileSystemService : IFileSystemService
    {
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

        public Stream CreateFile(string path)
        {
            return _fileSystem.CreateFile(path);
        }

        public Stream OpenFile(string path, OpenFileMode mode)
        {
            string fullPath = GetFullPath(path);
            if (_fileSystem.FileExists(fullPath))
                return _fileSystem.OpenFile(fullPath, mode);

            return Bundle.OpenFile(path, mode);
        }

        public bool FileExists(string path)
        {
            string fullPath = GetFullPath(path);
            if (_fileSystem.FileExists(fullPath))
                return true;

            return Bundle.FileExists(path);
        }

        public bool DirectoryExists(string path)
        {
            string fullPath = GetFullPath(path);
            if (_fileSystem.DirectoryExists(fullPath))
                return true;

            return Bundle.DirectoryExists(path);
        }

        public bool CreateDirectory(string path)
        {
            return _fileSystem.CreateDirectory(path);
        }

        public long GetFileSize(string path)
        {
            if (_fileSystem.FileExists(path))
                return _fileSystem.GetFileSize(path);

            return Bundle.GetFileSize(path);
        }

        #endregion
    }
}
