using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Jamiras.Components;

namespace Jamiras.IO.MP4
{
    public class Mp4TagWriter : Mp4Reader
    {
        private readonly IDictionary<Mp4Tag, string> _originalTags;
        private readonly string _path;
        private const uint FREE_TAG = 0x66726565;
        private const uint DATA_TAG = 0x64617461;
        private const uint XTRA_TAG = 0x58747261;
        private const uint META_TAG = 0x6D657461;
        private const uint TAGS_TAG = 0x74616773;

        public Mp4TagWriter(string filePath)
            : base(filePath)
        {
            _path = filePath;

            _originalTags = new TinyDictionary<Mp4Tag, string>();
            foreach (var kvp in Tags)
                _originalTags[kvp.Key] = kvp.Value;
        }

        private static bool ContainsNonDigit(string value)
        {
            foreach (var c in value)
            {
                if (!Char.IsDigit(c))
                    return true;
            }

            return false;
        }

        private int CalculateSpaceNeeded()
        {
            int spaceNeeded = 0;
            bool hasWmBlock = _blocks.Find(b => b.Tag == "moov.udta.Xtra").Size > 0;
            bool hasExBlock = _blocks.Find(b => b.Tag == "moov.udta.tags.meta").Size > 0;

            foreach (var kvp in Tags)
            {
                var wmKey = GetWMPropertyKey(kvp.Key);
                if (!String.IsNullOrEmpty(wmKey))
                {
                    if (!hasWmBlock)
                    {
                        spaceNeeded += 8;
                        hasWmBlock = true;
                    }

                    spaceNeeded += 8 + Encoding.UTF8.GetByteCount(wmKey) + 10 + Encoding.Unicode.GetByteCount(kvp.Value) + 2;
                }
                else
                {
                    var exTag = GetExtendedTag(kvp.Key);
                    if (!String.IsNullOrEmpty(exTag) && ContainsNonDigit(kvp.Value))
                    {
                        if (!hasExBlock)
                        {
                            spaceNeeded += 20;
                            hasExBlock = true;
                        }

                        spaceNeeded += 3 + Encoding.UTF8.GetByteCount(exTag) + 6 + Encoding.UTF8.GetByteCount(kvp.Value);
                    }
                    else
                    {
                        spaceNeeded += 24;
                        switch (GetDataType(kvp.Key))
                        {
                            case ItemListDataType.Utf8String:
                                spaceNeeded += Encoding.UTF8.GetByteCount(kvp.Value);
                                break;
                            case ItemListDataType.Int32:
                                spaceNeeded += 4;
                                break;
                            case ItemListDataType.Image:
                                spaceNeeded += kvp.Value.Length * 3 / 4;
                                break;
                        }
                    }
                }
            }

            return spaceNeeded;
        }

        /// <summary>
        /// Commits any changes made to the Tags collection.
        /// </summary>
        public void Commit(Action<int> progressHandler = null)
        {
            if (!TagsModified)
                return;

            var spaceNeeded = CalculateSpaceNeeded();

            var xtraIndex = FindBlock(_blocks, "moov.udta.Xtra");
            if (xtraIndex >= 0)
                spaceNeeded -= (int)_blocks[xtraIndex].Size;

            var tagsIndex = FindBlock(_blocks, "moov.udta.tags");
            if (tagsIndex >= 0)
                spaceNeeded -= (int)_blocks[tagsIndex].Size;

            var ilstIndex = FindBlock(_blocks, "moov.udta.meta.ilst");
            spaceNeeded -= (int)_blocks[ilstIndex].Size;

            var freeIndex = ilstIndex + 1;
            while (freeIndex < _blocks.Count && _blocks[freeIndex].Tag.Length > 19)
                freeIndex++;

            if (freeIndex == tagsIndex)
            {
                while (_blocks[freeIndex].Tag.StartsWith("moov.udta.tags"))
                    freeIndex++;

                if (_blocks[freeIndex].Tag == "free")
                {
                    var freeBlock = _blocks[freeIndex];
                    freeBlock.Address = _blocks[tagsIndex].Address;
                    freeBlock.Size += _blocks[tagsIndex].Size;
                    _blocks[freeIndex] = freeBlock;

                    while (freeIndex > tagsIndex)
                    {
                        freeIndex--;
                        _blocks.RemoveAt(freeIndex);

                        if (xtraIndex > freeIndex)
                            xtraIndex--;
                    }
                }
            }

            if (freeIndex == xtraIndex)
            {
                freeIndex++;
            }
            else if (xtraIndex < ilstIndex && _blocks[xtraIndex + 1].Tag == "moov.udta.meta")
            {
                MoveMetaBlock(_blocks[xtraIndex + 1], _blocks[xtraIndex].Address);
                _blocks.RemoveAt(xtraIndex);
                ilstIndex--;
                freeIndex--;
            }

            if (freeIndex < _blocks.Count && _blocks[freeIndex].Tag == "free" && _blocks[freeIndex].Size - 8 > spaceNeeded)
                UpdateMetadataInPlace(ilstIndex, freeIndex);
            else
                UpdateMetadataInNewFile(ilstIndex, freeIndex, progressHandler);

            if (progressHandler != null)
                progressHandler(100);
        }

        private void MoveMetaBlock(Mp4Block block, long newAddress)
        {
            var adjustment = newAddress - block.Address;

            using (var stream = File.Open(_path, FileMode.Open, FileAccess.ReadWrite))
            {
                var reader = new BinaryReader(stream);
                var writer = new BinaryWriter(stream);

                var buffer = new byte[block.Size];
                stream.Position = block.Address;
                reader.Read(buffer, 0, (int)block.Size);

                stream.Position = newAddress;
                writer.Write(buffer, 0, (int)block.Size);

                var nextBlockAddress = block.Address + block.Size;
                for (int i = 0; i < _blocks.Count; i++)
                {
                    if (_blocks[i].Address == nextBlockAddress)
                    {
                        if (_blocks[i].Tag == "free")
                        {
                            var block2 = _blocks[i];
                            block2.Address += adjustment;
                            block2.Size -= adjustment;
                            _blocks[i] = block2;

                            stream.Position = block2.Address;
                            WriteUInt32(writer, (uint)block2.Size);
                        }
                        break;
                    }
                }
            }

            for (int i = 0; i < _blocks.Count; i++)
            {
                if (_blocks[i].Address >= block.Address && _blocks[i].Address < block.Address + block.Size + adjustment)
                {
                    var block2 = _blocks[i];
                    block2.Address += adjustment;
                    _blocks[i] = block2;
                }
            }
        }

        private bool TagsModified
        {
            get
            {
                if (Tags.Count != _originalTags.Count)
                    return true;

                foreach (var kvp in Tags)
                {
                    string value;
                    if (!_originalTags.TryGetValue(kvp.Key, out value) || value != kvp.Value)
                        return true;
                }

                return false;
            }
        }

        private static int FindBlock(List<Mp4Block> blocks, string tag)
        {
            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i].Tag == tag)
                    return i;
            }

            return -1;
        }

        private void UpdateBlockSize(BinaryWriter writer, string tag, long blockEnd)
        {
            var index = FindBlock(_blocks, tag);
            var block = _blocks[index];
            block.Size = blockEnd - block.Address;
            _blocks[index] = block;

            writer.BaseStream.Seek(block.Address, SeekOrigin.Begin);
            WriteUInt32(writer, (uint)block.Size);
        }

        private static void WriteUInt32(BinaryWriter writer, uint value)
        {
            var buffer = new byte[4];    
            buffer[0] = (byte)(value >> 24);
            buffer[1] = (byte)(value >> 16);
            buffer[2] = (byte)(value >> 8);
            buffer[3] = (byte)value;
            writer.Write(buffer);
        }

        private static uint GetClass(Mp4Tag type)
        {
            switch (type)
            {
                case Mp4Tag.DiskNumber:
                case Mp4Tag.TrackNumber:
                    return 0;

                case Mp4Tag.Album:
                case Mp4Tag.Artist:
                case Mp4Tag.Comment:
                case Mp4Tag.Description:
                case Mp4Tag.Encoder:
                case Mp4Tag.Genre:
                case Mp4Tag.LongDescription:
                case Mp4Tag.ReleaseDate:
                case Mp4Tag.SortAlbum:
                case Mp4Tag.SortArtist:
                case Mp4Tag.SortTitle:
                case Mp4Tag.SortTvShowName:
                case Mp4Tag.Title:
                case Mp4Tag.TvNetwork:
                case Mp4Tag.TvShowName:
                    return 1;

                case Mp4Tag.CoverArt:
                    return 13; // 14 for png

                case Mp4Tag.Rating:
                case Mp4Tag.TvEpisode:
                case Mp4Tag.TvSeason:
                    return 21;

                default:
                    throw new NotImplementedException("No class for " + type);
            }
        }

        private void WriteTags(BinaryWriter writer, IDictionary<string, string> exTags)
        {
            foreach (var kvp in Tags)
            {
                if (GetWMPropertyKey(kvp.Key) != null)
                    continue;

                byte[] bytes;
                switch (GetDataType(kvp.Key))
                {
                    case ItemListDataType.Int32:
                        var exTag = GetExtendedTag(kvp.Key);
                        if (!String.IsNullOrEmpty(exTag) && ContainsNonDigit(kvp.Value))
                        {
                            exTags[exTag] = kvp.Value;
                            bytes = new byte[4];
                        }
                        else
                        {
                            bytes = new byte[4];
                            uint intval;
                            if (UInt32.TryParse(kvp.Value, out intval))
                            {
                                bytes[0] = (byte)(intval >> 24);
                                bytes[1] = (byte)(intval >> 16);
                                bytes[2] = (byte)(intval >> 8);
                                bytes[3] = (byte)intval;
                            }
                        }
                        break;

                    case ItemListDataType.Utf8String:
                        bytes = Encoding.UTF8.GetBytes(kvp.Value);
                        break;

                    case ItemListDataType.Image:
                        bytes = Convert.FromBase64String(kvp.Value);
                        break;

                    default:
                        continue;
                }

                WriteUInt32(writer, (uint)bytes.Length + 24);
                WriteUInt32(writer, (uint)kvp.Key);
                WriteUInt32(writer, (uint)bytes.Length + 16);
                WriteUInt32(writer, DATA_TAG);
                WriteUInt32(writer, GetClass(kvp.Key));
                WriteUInt32(writer, 0);
                writer.Write(bytes);
            }
        }

        private void WriteWmTags(BinaryWriter writer)
        {
            long headerStart = 0;
            
            foreach (var kvp in Tags)
            {
                var key = GetWMPropertyKey(kvp.Key);
                if (key == null)
                    continue;

                if (headerStart == 0)
                {
                    headerStart = writer.BaseStream.Position;

                    WriteUInt32(writer, 0); // size, we'll come back for it
                    WriteUInt32(writer, XTRA_TAG);
                    WriteUInt32(writer, 0); // content_size, we'll come back for it too.
                }

                var bytes = Encoding.UTF8.GetBytes(key);
                WriteUInt32(writer, (uint)bytes.Length);
                writer.Write(bytes, 0, bytes.Length);

                bytes = Encoding.Unicode.GetBytes(kvp.Value);
                WriteUInt32(writer, 1);
                WriteUInt32(writer, (uint)bytes.Length + 2 + 2 + 4); // length in UCS-2 chars + 2 for terminator + 2 for 08 + 4 for this length
                writer.Write((byte)0);
                writer.Write((byte)8);

                writer.Write(bytes, 0, bytes.Length);
                writer.Write((byte)0);
                writer.Write((byte)0);
            }

            if (headerStart != 0)
            {
                var position = writer.BaseStream.Position;
                var length = position - headerStart;
                writer.BaseStream.Position = headerStart;
                WriteUInt32(writer, (uint)length);
                WriteUInt32(writer, XTRA_TAG);
                WriteUInt32(writer, (uint)length - 8);
                writer.BaseStream.Position = position;
            }
        }

        private void CopyTagToExTags(Mp4Tag tag, string exTag, IDictionary<string, string> exTags)
        {
            string value;
            if (!exTags.ContainsKey(exTag) && Tags.TryGetValue(tag, out value) && !String.IsNullOrEmpty(value))
                exTags[exTag] = value;
        }

        private void WriteExTags(BinaryWriter writer, IDictionary<string, string> exTags)
        {
            if (exTags.Count == 0)
                return;

            CopyTagToExTags(Mp4Tag.Title, "title", exTags);
            CopyTagToExTags(Mp4Tag.TvShowName, "tvshow", exTags);
            CopyTagToExTags(Mp4Tag.TvSeason, "tvseason", exTags);
            CopyTagToExTags(Mp4Tag.TvEpisode, "tvepisode", exTags);
            CopyTagToExTags(Mp4Tag.ReleaseDate, "year", exTags);

            long headerStart = writer.BaseStream.Position;
            WriteUInt32(writer, 0);
            WriteUInt32(writer, TAGS_TAG);
            WriteUInt32(writer, 0);
            WriteUInt32(writer, META_TAG);

            WriteUInt32(writer, (uint)exTags.Count);
            foreach (var kvp in exTags)
            {
                var bytes = Encoding.UTF8.GetBytes(kvp.Key);
                writer.Write((byte)0x80);
                writer.Write((byte)(bytes.Length >> 8));
                writer.Write((byte)bytes.Length);
                writer.Write(bytes);

                bytes = Encoding.UTF8.GetBytes(kvp.Value);
                writer.Write((byte)0);
                writer.Write((byte)1);
                WriteUInt32(writer, (uint)bytes.Length);
                writer.Write(bytes);
            }

            uint metaSize = (uint)(writer.BaseStream.Position - headerStart - 8);

           //writer.Write(new byte[] { 0, 0, 0, 20, 0x74, 0x73, 0x65, 0x67, 0, 0, 0, 12, 0x74, 0x73, 0x68, 0x64, 0, 0, 0, 0 });

            uint blockSize = (uint)(writer.BaseStream.Position - headerStart);
            writer.BaseStream.Position = headerStart;
            WriteUInt32(writer, blockSize);
            WriteUInt32(writer, TAGS_TAG);
            WriteUInt32(writer, metaSize);
            writer.BaseStream.Position = headerStart + blockSize;
        }

        private void UpdateMetadataInPlace(int ilstIndex, int freeIndex)
        {
            using (var stream = File.Open(_path, FileMode.Open, FileAccess.ReadWrite))
            {
                var writer = new BinaryWriter(stream);
                var exTags = new TinyDictionary<string, string>();

                // update the tags
                stream.Seek(_blocks[ilstIndex].Address + 8, SeekOrigin.Begin);
                WriteTags(writer, exTags);

                // update the meta pointers
                var writerPosition = stream.Position;
                UpdateBlockSize(writer, "moov.udta.meta.ilst", writerPosition);
                UpdateBlockSize(writer, "moov.udta.meta", writerPosition);

                // update the WM tags
                stream.Position = writerPosition;
                WriteWmTags(writer);

                // update the ex tags
                WriteExTags(writer, exTags);

                writerPosition = stream.Position;

                // update the free block size and position
                var freeBlock = _blocks[freeIndex];
                freeBlock.Size = freeBlock.Address + freeBlock.Size - writerPosition;
                freeBlock.Address = writerPosition;
                _blocks[ilstIndex + 1] = freeBlock;
                WriteUInt32(writer, (uint)freeBlock.Size);
                WriteUInt32(writer, FREE_TAG);

                UpdateBlockSize(writer, "moov.udta", writerPosition);
                UpdateBlockSize(writer, "moov", writerPosition);

                // adjust the blocks for the new file
                var blockReader = new BinaryReader(stream);
                blockReader.BaseStream.Seek(_blocks[0].Address, SeekOrigin.Begin);
                GetAllBlocks(blockReader);
            }
        }

        private void Copy(BinaryReader reader, BinaryWriter writer, long bytesToCopy, Action<int> progressHandler)
        {
            long length = 0;
            int progress = 0;
            if (progressHandler != null)
            {
                length = reader.BaseStream.Length / 100;
                if (length == 0)
                    length = 1;

                progress = (int)(reader.BaseStream.Position / length);
                progressHandler(progress);
            }

            var buffer = new byte[65536];
            while (bytesToCopy > buffer.Length)
            {
                reader.Read(buffer, 0, buffer.Length);
                writer.Write(buffer, 0, buffer.Length);
                bytesToCopy -= buffer.Length;

                if (progressHandler != null)
                {
                    var newProgress = (int)(reader.BaseStream.Position / length);
                    if (newProgress != progress)
                    {
                        progress = newProgress;
                        progressHandler(progress);
                    }
                }
            }

            if (bytesToCopy > 0)
            {
                reader.Read(buffer, 0, (int)bytesToCopy);
                writer.Write(buffer, 0, (int)bytesToCopy);
            }
        }

        private void UpdateMetadataInNewFile(int ilstIndex, int freeIndex, Action<int> progressHandler)
        {
            var copyPath = _path.Substring(0, _path.Length - 4) + ".tmp" + _path.Substring(_path.Length - 4);
            using (var readStream = File.Open(_path, FileMode.Open, FileAccess.Read))
            {
                var reader = new BinaryReader(readStream);

                using (var writeStream = File.Open(copyPath, FileMode.Create, FileAccess.ReadWrite))
                {
                    var writer = new BinaryWriter(writeStream);
                    var exTags = new TinyDictionary<string, string>();

                    // copy everything up to the ilist block header
                    Copy(reader, writer, _blocks[ilstIndex].Address + 8, progressHandler);

                    // generate a new ilist block
                    WriteTags(writer, exTags);

                    // update all the meta pointers
                    var writePosition = writeStream.Position;
                    UpdateBlockSize(writer, "moov.udta.meta.ilst", writePosition);
                    UpdateBlockSize(writer, "moov.udta.meta", writePosition);

                    // update the WM tags
                    writeStream.Position = writePosition;
                    WriteWmTags(writer);

                    // update the ex tags
                    WriteExTags(writer, exTags);

                    // update the outer container pointers
                    writePosition = writeStream.Position;
                    UpdateBlockSize(writer, "moov.udta", writePosition);
                    UpdateBlockSize(writer, "moov", writePosition);
                    writer.BaseStream.Position = writePosition;

                    // generate a new free block in the writer
                    const uint freeBlockSize = 520;
                    WriteUInt32(writer, freeBlockSize);
                    WriteUInt32(writer, FREE_TAG);
                    writer.Write(new byte[freeBlockSize - 8]);

                    // adjust for the new free block
                    var additionalSpaceUsed = writePosition - _blocks[freeIndex].Address + freeBlockSize;
                    writePosition = writer.BaseStream.Position;

                    // if the block following the ilst block is a free block, discard it in favor of 
                    // our new one. otherwise, insert our new one in the blocks collection
                    var readPosition = _blocks[freeIndex].Address;
                    if (_blocks[freeIndex].Tag == "free")
                    {
                        additionalSpaceUsed -= _blocks[freeIndex].Size;
                        readPosition += _blocks[freeIndex].Size;
                    }

                    // update all the pointers in the stco (sample table chunk offset)
                    // there can be multiple tracks, so look for all stco's.
                    foreach (var block in _blocks)
                    {
                        if (block.Tag == "moov.trak.mdia.minf.stbl.stco")
                        {
                            writer.BaseStream.Position = block.Address + 16;
                            reader.BaseStream.Position = block.Address + 12;
                            uint entries = ReadUInt32(reader);
                            for (int i = 0; i < entries; i++)
                            {
                                uint offset = ReadUInt32(reader);
                                offset += (uint)additionalSpaceUsed;
                                WriteUInt32(writer, offset);
                            }
                        }
                    }

                    // copy the remaining portion of the file
                    writer.BaseStream.Position = writePosition;
                    reader.BaseStream.Position = readPosition;
                    Copy(reader, writer, reader.BaseStream.Length - reader.BaseStream.Position, progressHandler);

                    // adjust the blocks for the new file
                    var blockReader = new BinaryReader(writeStream);
                    blockReader.BaseStream.Seek(_blocks[0].Address, SeekOrigin.Begin);
                    GetAllBlocks(blockReader);
                }
            }

            // replace the existing file with the new file
            File.Delete(_path);
            File.Move(copyPath, _path);

            // reset the original tags collection
            _originalTags.Clear();
            foreach (var kvp in Tags)
                _originalTags[kvp.Key] = kvp.Value;
        }
    }
}
