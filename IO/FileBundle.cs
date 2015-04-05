using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Jamiras.Services;

namespace Jamiras.IO
{
    public class FileBundle : IFileSystemService
    {
        public FileBundle(string fileName)
        {
            if (File.Exists(fileName))
                OpenBundle(fileName);
            else
                CreateBundle(fileName);

            _fileName = fileName;
            _recentFiles = new FileInfo[16];
        }

        private void OpenBundle(string fileName)
        {
            var stream = File.OpenRead(fileName);
            byte[] signature = new byte[4];
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

        private void CreateBundle(string fileName)
        {
            var stream = File.Create(fileName);
            stream.Write(new byte[] { (byte)'J', (byte)'B', (byte)'D', Version }, 0, 4);
            var writer = new BinaryWriter(stream);

            _numBuckets = 719;
            _bucketOffset = new int[_numBuckets];
            writer.Write(_numBuckets);
            for (int i = 0; i < _numBuckets; i++)
                writer.Write((int)0);

            writer.Write((int)0);

            stream.Close();
        }

        private const byte Version = 1;
        private string _fileName;
        private int _numBuckets;
        private int[] _bucketOffset;
        private int _freeSpaceOffset;

        [DebuggerDisplay("{FileName} {Size}@{Offset}")]
        private class FileInfo
        {
            public string FileName { get; set; }
            public DateTime Modified { get; set; }
            public int Size { get; set; }
            public int Offset { get; set; }
            public BundleWriteStream Stream { get; set; }

            public bool IsDirectory
            {
                get { return Modified == DateTime.MinValue; }
            }
        }

        private FileInfo[] _recentFiles;
        private int _recentFilesIndex;

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

        public int GetBucket(string path)
        {
            uint hash = 0x3BAD84E1;
            foreach (var c in path)
            {
                long l = (long)hash;
                l *= (int)Char.ToLower(c) * ((hash & 0xFF) + 1);
                hash = (uint)(l & 0xFFFFFFFF) ^ (uint)(l >> 32);
            }

            int bucket = (int)hash % _numBuckets;
            if (bucket < 0)
                bucket += _numBuckets;

            return bucket;
        }

        public string FileName
        {
            get { return _fileName; }
        }

        public IEnumerable<string> GetFiles()
        {
            foreach (var info in EnumerateFiles())
            {
                if (!info.IsDirectory)
                    yield return info.FileName;
            }
        }

        public IEnumerable<string> GetFiles(string path)
        {
            foreach (var info in EnumerateFiles())
            {
                if (!info.IsDirectory && InFolder(info, path))
                    yield return info.FileName;
            }
        }

        public IEnumerable<string> GetDirectories()
        {
            foreach (var info in EnumerateFiles())
            {
                if (info.IsDirectory)
                    yield return info.FileName;
            }
        }

        public IEnumerable<string> GetDirectories(string path)
        {
            foreach (FileInfo info in EnumerateFiles())
            {
                if (info.IsDirectory && InFolder(info, path))
                    yield return info.FileName;
            }
        }

        private static bool InFolder(FileInfo info, string path)
        {
            int index = info.FileName.LastIndexOf('\\');
            if (index == -1)
                return String.IsNullOrEmpty(path);

            return String.Compare(path, 0, info.FileName, 0, index) == 0;
        }

        private IEnumerable<FileInfo> EnumerateFiles()
        {
            BinaryReader reader = null;

            foreach (var bucketOffset in _bucketOffset)
            {
                if (bucketOffset != 0)
                {
                    if (reader == null)
                        reader = new BinaryReader(File.OpenRead(_fileName));

                    foreach (var info in EnumerateFiles(reader, bucketOffset))
                        yield return info;
                }
            }

            if (reader != null)
                reader.Close();
        }

        private IEnumerable<FileInfo> EnumerateFiles(int bucketOffset)
        {
            using (var reader = new BinaryReader(File.OpenRead(_fileName)))
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
            for (int i = 0; i < _recentFiles.Length; i++)
            {
                if (_recentFiles[i] != null && ReferenceEquals(_recentFiles[i].Stream, stream))
                {
                    Commit(_recentFiles[i]);
                    break;
                }
            }
        }

        private void Commit(FileInfo info)
        {
            using (var fileStream = File.Open(_fileName, FileMode.Open, FileAccess.ReadWrite))
            {
                int headerOffset = GetAvailableSpaceOffset(info, fileStream);

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

                writer.Seek(headerOffset, SeekOrigin.Begin);
                writer.Write((int)0); // no next

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
                    byte[] buffer = new byte[8192];
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

                writer.Flush();
            }
        }

        private int GetAvailableSpaceOffset(FileInfo info, FileStream fileStream)
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

        private static int GetBucketOffset(int bucket)
        {
            return bucket * 4 + 8;
        }

        public DateTime GetModified(string path)
        {
            var info = GetFileInfo(path);
            return (info != null) ? info.Modified.ToLocalTime() : DateTime.MinValue;
        }
        
        public void SetModified(string path, DateTime modified)
        {
            var info = GetFileInfo(path);
            if (info != null)
                info.Modified = modified;
        }

        public int GetSize(string path)
        {
            var info = GetFileInfo(path);
            return (info != null) ? info.Size : 0;
        }

        public Stream OpenFile(string path, OpenFileMode mode)
        {
            var info = GetFileInfo(path);
            if (info == null || info.IsDirectory)
                return null;

            return new BundleReadStream(_fileName, info.Offset, info.Size);
        }

        private class BundleReadStream : Stream
        {
            public BundleReadStream(string fileName, int offset, int length)
            {
                _baseStream = File.OpenRead(fileName);
                _baseStream.Seek(offset, SeekOrigin.Begin);
                _offset = offset;
                _length = length;
            }

            private Stream _baseStream;
            private int _length;
            private int _offset;

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

        public bool FileExists(string path)
        {
            var info = GetFileInfo(path);
            return (info != null && info.Offset != 0);
        }

        public bool DirectoryExists(string path)
        {
            var info = GetFileInfo(path);
            return (info != null && info.Offset == 0);
        }

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

        public bool DeleteFile(string path)
        {
            var info = GetFileInfo(path);
            if (info == null)
                return false;

            var bucket = GetBucket(path);
            var lastOffset = GetBucketOffset(bucket);

            var offset = _bucketOffset[bucket];

            using (var fileStream = File.Open(_fileName, FileMode.Open, FileAccess.ReadWrite))
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
                            writer.Write((int)0);

                            return true;
                        }
                    }

                    lastOffset = offset;
                    offset = nextOffset;
                } while (offset != 0);
            }

            return false;
        }

        #endregion
    }
}
