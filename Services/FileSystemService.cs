using System.IO;
using Jamiras.Components;

namespace Jamiras.Services
{
    [Export(typeof(IFileSystemService))]
    internal class FileSystemService : IFileSystemService
    {
        #region IFileSystemService Members

        public Stream CreateFile(string path)
        {
            return File.Create(path);
        }

        public Stream OpenFile(string path, OpenFileMode mode)
        {
            if (mode == OpenFileMode.Read)
                return File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);

            return File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }

        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        public bool CreateDirectory(string path)
        {
            return (Directory.CreateDirectory(path) != null);
        }

        public long GetFileSize(string path)
        {
            return new FileInfo(path).Length;
        }

        #endregion
    }
}
