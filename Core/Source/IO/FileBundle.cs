using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Jamiras.Core.Services.Impl;
using Jamiras.Services;

namespace Jamiras.IO
{
    /// <summary>
    /// Manages a file containing multiple files.
    /// </summary>
    /// <remarks>
    /// Implements <see cref="IFileSystemService"/> because it acts like one.
    /// </remarks>
    public class FileBundle : IFileSystemService
    {
        private const byte Version = 1;
        private readonly string _fileName;
        private int _numBuckets;
        private int[] _bucketOffset;
        private int _freeSpaceOffset;
        private readonly IFileSystemService _fileSystem;
        private readonly FileInfo[] _recentFiles;
        private int _recentFilesIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileBundle"/> class.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        public FileBundle(string fileName)
            : this(fileName, 17)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileBundle"/> class.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="numBuckets">The number buckets to use for hashings paths. Should be a prime number roughly the square root of the expected number of items in the file.</param>
        protected FileBundle(string fileName, int numBuckets)
            : this(fileName, numBuckets, new FileSystemService())
        {
        }

        internal FileBundle(string fileName, IFileSystemService fileSystem)
            : this(fileName, 17, fileSystem)
        {            
        }

        internal FileBundle(string fileName, int numBuckets, IFileSystemService fileSystem)
        {
            _fileSystem = fileSystem;
            if (_fileSystem.FileExists(fileName))
                OpenBundle(fileName);
            else
                CreateBundle(fileName, numBuckets);

            _fileName = fileName;
            _recentFiles = new FileInfo[16];
        }

        // File structure:
        //  - Header Block
        //     JBD# - 4 byte header - 4th byte is format version indicator
        //     #### - 4 byte integer - number of buckets in hash table
        //    x#### - num buckets x 4 byte integer - address of first element for bucket
        //     #### - 4 byte integer - address of first deleted element
        //  - File Block
        //     #### - 4 byte integer - address of next element in bucket
        //     #### - 4 byte integer - size of file
        // ######## - 8 byte long - binary form of DateTime representing the last modified time of the file
        //        # - 1 byte - number of characters in file name
        //       x# - name length bytes - characters in file name
        //       x# - file size bytes - contents of file

        private void OpenBundle(string fileName)
        {
            var stream = _fileSystem.OpenFile(fileName, OpenFileMode.Read);
            var signature = new byte[4];
            stream.Read(signature, 0, 4);
            if (signature[0] != 'J' || signature[1] != 'B' || signature[2] != 'D')
                throw new InvalidOperationException(fileName + " is not a Jamiras Bundle");
            if (signature[3] > Version)
                throw new InvalidOperationException(fileName + " is version " + signature[3] + ", but only versions through " + Version + " are supported");

            var reader = new BinaryReader(stream);
            _numBuckets = reader.ReadInt32();
            _bucketOffset = new int[_numBuckets];
            for (int i = 0; i < _numBuckets; i++)
                _bucketOffset[i] = reader.ReadInt32();

            _freeSpaceOffset = reader.ReadInt32();

            stream.Close();
        }

        private void CreateBundle(string fileName, int numBuckets)
        {
            var stream = _fileSystem.CreateFile(fileName);
            stream.Write(new[] { (byte)'J', (byte)'B', (byte)'D', Version }, 0, 4);
            var writer = new BinaryWriter(stream);

            _numBuckets = numBuckets;
            _bucketOffset = new int[_numBuckets];
            writer.Write(_numBuckets);
            for (int i = 0; i < _numBuckets; i++)
                writer.Write(0);

            writer.Write(0);

            stream.Close();
        }

        /// <summary>
        /// Information about a single file in the bundle.
        /// </summary>
        [DebuggerDisplay("{FileName} {Size}@{Offset}")]
        protected class FileInfo
        {
            /// <summary>
            /// Gets or sets the name of the file.
            /// </summary>
            public string FileName { get; set; }

            /// <summary>
            /// Gets or sets the time the file was last modified.
            /// </summary>
            public DateTime Modified { get; set; }

            /// <summary>
            /// Gets or sets the size of the file.
            /// </summary>
            public int Size { get; set; }

            /// <summary>
            /// Gets or sets the offset of the file within the bundle.
            /// </summary>
            public int Offset { get; set; }

            internal Stream Stream { get; set; }

            /// <summary>
            /// Gets a value indicating whether this instance is directory.
            /// </summary>
            /// <value>
            ///   <c>true</c> if this instance is directory; otherwise, <c>false</c>.
            /// </value>
            public bool IsDirectory
            {
                get { return Modified == DateTime.MinValue; }
            }
        }

        private FileInfo GetFileInfo(string path)
        {
            for (int i = _recentFilesIndex - 1; i >= 0; i--)
            {
                if (_recentFiles[i] != null && String.Compare(_recentFiles[i].FileName, path, StringComparison.OrdinalIgnoreCase) == 0)
                    return _recentFiles[i];
            }

            for (int i = _recentFiles.Length - 1; i >= _recentFilesIndex; i--)
            {
                if (_recentFiles[i] != null && String.Compare(_recentFiles[i].FileName, path, StringComparison.OrdinalIgnoreCase) == 0)
                    return _recentFiles[i];
            }

            int bucket = GetBucket(path);
            int bucketOffset = _bucketOffset[bucket];
            if (bucketOffset != 0)
            {
                foreach (var info in EnumerateFiles(bucketOffset))
                {
                    if (String.Compare(info.FileName, path, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        MakeRecent(info);
                        return info;
                    }
                }
            }

            return null;
        }

        private void MakeRecent(FileInfo info)
        {
            _recentFiles[_recentFilesIndex++] = info;
            if (_recentFilesIndex == _recentFiles.Length)
                _recentFilesIndex = 0;
        }

        /// <summary>
        /// Gets the bucket that should contain the file specified by <paramref name="path"/>.
        /// </summary>
        public int GetBucket(string path)
        {
            uint hash = 0x3BAD84E1;
            foreach (var c in path)
            {
                long l = hash;
                l *= (int)Char.ToLower(c) * ((hash & 0xFF) + 1);
                hash = (uint)(l & 0xFFFFFFFF) ^ (uint)(l >> 32);
            }

            int bucket = (int)hash % _numBuckets;
            if (bucket < 0)
                bucket += _numBuckets;

            return bucket;
        }

        /// <summary>
        /// Gets the name of the bundle file.
        /// </summary>
        public string FileName
        {
            get { return _fileName; }
        }

        /// <summary>
        /// Gets a list of all files in the bundle.
        /// </summary>
        public IEnumerable<string> GetFiles()
        {
            foreach (var info in EnumerateFiles())
            {
                if (!info.IsDirectory)
                    yield return info.FileName;
            }
        }

        /// <summary>
        /// Gets a list of all files in the bundle under the specified directory.
        /// </summary>
        public IEnumerable<string> GetFiles(string path)
        {
            foreach (var info in EnumerateFiles())
            {
                if (!info.IsDirectory && InFolder(info, path))
                    yield return info.FileName;
            }
        }

        /// <summary>
        /// Gets a list of all directories in the bundle.
        /// </summary>
        public IEnumerable<string> GetDirectories()
        {
            foreach (var info in EnumerateFiles())
            {
                if (info.IsDirectory)
                    yield return info.FileName;
            }
        }

        /// <summary>
        /// Gets a list of all directories in the bundle under the specified directory.
        /// </summary>
        public IEnumerable<string> GetDirectories(string path)
        {
            foreach (FileInfo info in EnumerateFiles())
            {
                if (info.IsDirectory && InFolder(info, path))
                    yield return info.FileName;
            }
        }

        /// <summary>
        /// Determines if the file refered to by <paramref name="info"/> is in the specified directory.
        /// </summary>
        protected static bool InFolder(FileInfo info, string path)
        {
            var index = info.FileName.LastIndexOf('\\');
            if (index == -1)
                return String.IsNullOrEmpty(path);

            return String.Compare(path, 0, info.FileName, 0, index) == 0;
        }

        /// <summary>
        /// Enumerates the files in the bundle.
        /// </summary>
        protected IEnumerable<FileInfo> EnumerateFiles()
        {
            BinaryReader reader = null;

            foreach (var bucketOffset in _bucketOffset)
            {
                if (bucketOffset != 0)
                {
                    if (reader == null)
                        reader = new BinaryReader(_fileSystem.OpenFile(_fileName, OpenFileMode.Read));

                    foreach (var info in EnumerateFiles(reader, bucketOffset))
                        yield return info;
                }
            }

            if (reader != null)
                reader.Close();
        }

        private IEnumerable<FileInfo> EnumerateFiles(int bucketOffset)
        {
            using (var reader = new BinaryReader(_fileSystem.OpenFile(_fileName, OpenFileMode.Read)))
            {
                foreach (var info in EnumerateFiles(reader, bucketOffset))
                    yield return info;
            }
        }

        private IEnumerable<FileInfo> EnumerateFiles(BinaryReader reader, int offset)
        {
            do
            {
                reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                offset = reader.ReadInt32();
                
                var info = new FileInfo();
                info.Size = reader.ReadInt32();
                info.Modified = DateTime.FromBinary(reader.ReadInt64());

                var nameLength = (int)reader.ReadByte();
                var builder = new StringBuilder();
                for (int i = 0; i < nameLength; i++)
                    builder.Append((char)reader.ReadByte());
                info.FileName = builder.ToString();

                info.Offset = (int)reader.BaseStream.Position;

                yield return info;
            } while (offset != 0);
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
            var info = new FileInfo();
            info.FileName = path;
            info.Stream = new BundleWriteStream(this);

            MakeRecent(info);
            return info.Stream;
        }

        private class BundleWriteStream : MemoryStream
        {
            public BundleWriteStream(FileBundle bundle)
            {
                _bundle = bundle;
            }

            private readonly FileBundle _bundle;

            public override void Close()
            {
                Flush();
                base.Close();
            }

            public override void Flush()
            {
                base.Flush();

                _bundle.Commit(this);
            }
        }

        private void Commit(BundleWriteStream stream)
        {
            foreach (FileInfo info in _recentFiles)
            {
                if (info != null && ReferenceEquals(info.Stream, stream))
                {
                    if (info.Modified == DateTime.MinValue)
                        info.Modified = DateTime.UtcNow;

                    Commit(info);
                    break;
                }
            }
        }

        /// <summary>
        /// Updates the bundle file with a modified <see cref="FileInfo"/> object.
        /// </summary>
        protected virtual void Commit(FileInfo info)
        {
            using (var fileStream = _fileSystem.OpenFile(_fileName, OpenFileMode.ReadWrite))
            {
                var headerOffset = GetAvailableSpaceOffset(info, fileStream);

                var writer = new BinaryWriter(fileStream);

                var bucket = GetBucket(info.FileName);
                var offset = _bucketOffset[bucket];
                if (offset == 0)
                {
                    _bucketOffset[bucket] = headerOffset;
                    writer.BaseStream.Seek(GetBucketOffset(bucket), SeekOrigin.Begin);
                    writer.Write(headerOffset);
                }
                else
                {
                    var reader = new BinaryReader(fileStream);
                    do
                    {
                        reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                        offset = reader.ReadInt32();
                    } while (offset != 0);

                    reader.BaseStream.Seek(-4, SeekOrigin.Current);
                    writer.Write(headerOffset);
                }

                WriteFile(info, headerOffset, writer);

                writer.Flush();
            }
        }

        /// <summary>
        /// Helper method for <see cref="Commit(FileInfo)"/> that writes the <paramref name="info"/> at the specified <paramref name="headerOffset"/>.
        /// </summary>
        protected static void WriteFile(FileInfo info, int headerOffset, BinaryWriter writer)
        {
            writer.Seek(headerOffset, SeekOrigin.Begin);
            writer.Write(0); // no next

            if (info.Stream != null)
                info.Size = (int)info.Stream.Position;
            writer.Write(info.Size);

            writer.Write(info.Modified.ToBinary());

            writer.Write((byte)info.FileName.Length);
            foreach (var c in info.FileName)
                writer.Write((byte)c);

            info.Offset = (int)writer.BaseStream.Position;

            if (info.Stream != null)
            {
                var buffer = new byte[8192];
                info.Stream.Seek(0, SeekOrigin.Begin);
                do
                {
                    int read = info.Stream.Read(buffer, 0, buffer.Length);
                    if (read == 0)
                        break;

                    writer.Write(buffer, 0, read);
                } while (true);

                info.Stream = null;
            }
        }

        private int GetAvailableSpaceOffset(FileInfo info, Stream fileStream)
        {
            if (_freeSpaceOffset == 0)
                return (int)fileStream.Length;

            var bestFitOffset = -1;
            var bestFitSize = Int32.MaxValue;
            var bestFitOffsetPointer = -1;
            var bestFitNextOffset = -1;
            var neededSize = (info.Stream != null) ? (int)info.Stream.Position : 0;

            var lastOffset = GetBucketOffset(_numBuckets);
            var offset = _freeSpaceOffset;
            var reader = new BinaryReader(fileStream);
            do
            {
                reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                var nextOffset = reader.ReadInt32();
                var size = reader.ReadInt32();
                if (size >= neededSize)
                {
                    reader.ReadInt64();
                    int nameLength = reader.ReadByte();
                    if (nameLength + size <= neededSize + info.FileName.Length)
                    {
                        if (nameLength + size < bestFitSize)
                        {
                            bestFitSize = nameLength + size;
                            bestFitOffset = offset;
                            bestFitOffsetPointer = lastOffset;
                            bestFitNextOffset = nextOffset;
                        }
                    }
                }

                lastOffset = offset;
                offset = nextOffset;
            } while (offset != 0);

            if (bestFitOffset == -1)
                return (int)fileStream.Length;

            var writer = new BinaryWriter(fileStream);

            int remaining = bestFitSize - neededSize - info.FileName.Length - (4 + 8 + 1);
            if (remaining > 64)
            {
                bestFitNextOffset = bestFitOffset + neededSize + info.FileName.Length;
                writer.BaseStream.Seek(bestFitNextOffset, SeekOrigin.Begin);
                writer.Write(remaining);
                writer.Write((long)0);
                writer.Write((byte)0);
            }

            writer.BaseStream.Seek(bestFitOffsetPointer, SeekOrigin.Begin);
            writer.Write(bestFitNextOffset);

            if (bestFitOffsetPointer == GetBucketOffset(_numBuckets))
                _freeSpaceOffset = bestFitNextOffset;

            return bestFitOffset;
        }

        /// <summary>
        /// Calculates the offset of the bucket pointer within the file.
        /// </summary>
        protected static int GetBucketOffset(int bucket)
        {
            return bucket * 4 + 8;
        }

        /// <summary>
        /// Gets the last time a file was modified.
        /// </summary>
        public DateTime GetModified(string path)
        {
            var info = GetFileInfo(path);
            return (info != null) ? info.Modified.ToLocalTime() : DateTime.MinValue;
        }

        /// <summary>
        /// Sets the last time a file was modified.
        /// </summary>
        public void SetModified(string path, DateTime modified)
        {
            var info = GetFileInfo(path);
            if (info != null)
                info.Modified = modified;
        }

        /// <summary>
        /// Gets the size.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public int GetSize(string path)
        {
            var info = GetFileInfo(path);
            return (info != null) ? info.Size : 0;
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
            var info = GetFileInfo(path);
            if (info == null || info.IsDirectory)
                return null;

            return new BundleReadStream(_fileName, info.Offset, info.Size, _fileSystem);
        }

        private class BundleReadStream : Stream
        {
            public BundleReadStream(string fileName, int offset, int length, IFileSystemService fileSystem)
            {
                _baseStream = fileSystem.OpenFile(fileName, OpenFileMode.Read);
                _baseStream.Seek(offset, SeekOrigin.Begin);
                _offset = offset;
                _length = length;
            }

            private readonly Stream _baseStream;
            private readonly int _length;
            private readonly int _offset;

            public override void Flush()
            {
            }

            public override long Length
            {
                get { return _length; }
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override bool CanRead
            {
                get { return true; }
            }

            public override bool CanWrite
            {
                get { return false; }
            }

            public override bool CanSeek
            {
                get { return true; }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int remaining = _offset + _length - (int)_baseStream.Position;
                if (count > remaining)
                    count = remaining;

                return _baseStream.Read(buffer, offset, count);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }

            public override long Position
            {
                get { return _baseStream.Position - _offset; }
                set { _baseStream.Position = value + _offset; }
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                switch (origin)
                {
                    default:
                        return _baseStream.Seek(offset, origin);

                    case SeekOrigin.Begin:
                        return _baseStream.Seek(offset + _offset, SeekOrigin.Begin);

                    case SeekOrigin.End:
                        return _baseStream.Seek(_offset + _length - _offset, SeekOrigin.Begin);
                }
            }

            public override void Close()
            {
                _baseStream.Close();
                base.Close();
            }
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
            var info = GetFileInfo(path);
            return (info != null && !info.IsDirectory);
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
            var info = GetFileInfo(path);
            return (info != null && info.IsDirectory);
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
            var info = GetFileInfo(path);
            if (info != null)
                return false;

            info = new FileInfo();
            info.FileName = path;
            Commit(info);
            return true;
        }

        /// <summary>
        /// Deletes the file.
        /// </summary>
        /// <param name="path">The path.</param>
        public bool DeleteFile(string path)
        {
            var info = GetFileInfo(path);
            if (info == null)
                return false;

            var bucket = GetBucket(path);
            var lastOffset = GetBucketOffset(bucket);

            var offset = _bucketOffset[bucket];

            using (var fileStream = _fileSystem.OpenFile(_fileName, OpenFileMode.ReadWrite))
            {
                var reader = new BinaryReader(fileStream);
                do
                {
                    reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                    var nextOffset = reader.ReadInt32();

                    reader.ReadInt32(); // size
                    reader.ReadInt64(); // modified

                    var nameLength = (int)reader.ReadByte();
                    if (nameLength == path.Length)
                    {
                        int i = 0;
                        while (i < nameLength && Char.ToLower((char)reader.ReadByte()) == Char.ToLower(path[i]))
                            i++;

                        if (i == nameLength)
                        {
                            var writer = new BinaryWriter(fileStream);
                            writer.BaseStream.Seek(lastOffset, SeekOrigin.Begin);
                            writer.Write(nextOffset);

                            if (lastOffset == GetBucketOffset(bucket))
                                _bucketOffset[bucket] = nextOffset;

                            if (_freeSpaceOffset == 0)
                            {
                                writer.BaseStream.Seek(GetBucketOffset(_numBuckets), SeekOrigin.Begin);
                                writer.Write(offset);
                                _freeSpaceOffset = offset;
                            }
                            else
                            {
                                var scan = _freeSpaceOffset;
                                do
                                {
                                    reader.BaseStream.Seek(scan, SeekOrigin.Begin);
                                    nextOffset = reader.ReadInt32();
                                    if (nextOffset == 0)
                                        break;

                                    scan = nextOffset;
                                } while (true);

                                writer.BaseStream.Seek(-4, SeekOrigin.Current);
                                writer.Write(offset);
                            }

                            // update node.next to null
                            writer.BaseStream.Seek(offset, SeekOrigin.Begin);
                            writer.Write(0);

                            for (int j = 0; j < _recentFiles.Length; j++)
                            {
                                if (_recentFiles[j] != null && String.Compare(_recentFiles[j].FileName, path, StringComparison.OrdinalIgnoreCase) == 0)
                                    _recentFiles[j] = null;
                            }

                            return true;
                        }
                    }

                    lastOffset = offset;
                    offset = nextOffset;
                } while (offset != 0);
            }

            return false;
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
            var info = GetFileInfo(path);
            if (info == null)
                return 0;

            return info.Size;
        }

        /// <summary>
        /// Gets the last time a file was modified.
        /// </summary>
        /// <param name="path">Path to file.</param>
        /// <returns>
        /// The <see cref="DateTime" /> the file was last modified.
        /// </returns>
        DateTime IFileSystemService.GetFileLastModified(string path)
        {
            return GetModified(path);
        }

        #endregion
    }
}
