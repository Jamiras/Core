using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Jamiras.Components;

namespace Jamiras.IO.MP4
{
    /// <summary>
    /// A class for reading metadata content from an MP4 file.
    /// </summary>
    public class Mp4Reader
    {
        const int CONTAINER_IDENTIFIER = 0x66747970;  // ftyp
        const int BRAND_MP42 = 0x6D703432; // mp42
        const int BRAND_ISOM = 0x69736F6D; // isom

        /// <summary>
        /// Initializes a new instance of the <see cref="Mp4Reader"/> class.
        /// </summary>
        /// <param name="filePath">The path to the MP4 file.</param>
        /// <exception cref="ArgumentException">
        /// The file at <paramref name="filePath"/> is not a valid MP4 file or not a supported MP4 version.
        /// </exception>
        public Mp4Reader(string filePath)
        {
            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
            {
                var reader = new BinaryReader(stream);
                uint next = ReadUInt32(reader);
                uint identifier = ReadUInt32(reader);
                if (identifier != CONTAINER_IDENTIFIER)
                    throw new ArgumentException("Stream does not appear to be an MP4 container", "stream");

                uint brand = ReadUInt32(reader);
                if (brand != BRAND_MP42 && brand != BRAND_ISOM)
                    throw new ArgumentException("Stream is not an MP4.2 container", "stream");

                reader.BaseStream.Position = next;
                _blocks = new List<Mp4Block>();
                GetAllBlocks(reader);

                _tags = new TinyDictionary<Mp4Tag, string>();
                GetTags(reader);
            }
        }

        internal readonly List<Mp4Block> _blocks;
        private static byte[] _buffer = new byte[4];
        private TinyDictionary<Mp4Tag, string> _tags;

        [DebuggerDisplay("{Tag} ({Size}@{Address})")]
        internal struct Mp4Block
        {
            public long Address { get; set; }
            public long Size { get; set; }
            public string Tag { get; set; }
        }

        internal static uint ReadUInt32(BinaryReader reader)
        {
            if (reader.Read(_buffer, 0, 4) == 0)
                return 0;

            return ((uint)_buffer[0] << 24) | ((uint)_buffer[1] << 16) | ((uint)_buffer[2] << 8) | (uint)_buffer[3];
        }

        /// <summary>
        /// Gets the Tags associated to the MP4 file
        /// </summary>
        public IDictionary<Mp4Tag, string> Tags 
        {
            get { return _tags; }
        }

        private void GetTags(BinaryReader reader)
        {
            var block = _blocks.Find(b => b.Tag == "moov.udta.meta.ilst");
            reader.BaseStream.Seek(block.Address + 8, SeekOrigin.Begin);

            var remaining = block.Size - 8;
            while (remaining > 0)
            {
                var size = ReadUInt32(reader);
                var type = (Mp4Tag)ReadUInt32(reader);

                var datasize = ReadUInt32(reader) - 16;
                reader.BaseStream.Seek(12, SeekOrigin.Current); // skip 'data' tag and data flags

                switch (GetDataType(type))
                {
                    case ItemListDataType.Utf8String:
                        var bytes = new byte[datasize];
                        reader.Read(bytes, 0, (int)datasize);
                        var strval = Encoding.UTF8.GetString(bytes, 0, (int)datasize);
                        _tags.Add(type, strval);
                        break;

                    case ItemListDataType.Int32:
                        var intval = ReadUInt32(reader);
                        _tags.Add(type, intval.ToString());
                        break;

                    case ItemListDataType.Image:
                        var image = new byte[datasize];
                        reader.Read(image, 0, (int)datasize);
                        var imageval = Convert.ToBase64String(image);
                        _tags.Add(type, imageval);
                        break;
                }

                remaining -= size;
            }

            block = _blocks.Find(b => b.Tag == "moov.udta.tags.meta");
            if (block.Size > 0)
            {
                reader.BaseStream.Seek(block.Address + 8, SeekOrigin.Begin);
                var count = ReadUInt32(reader);
                while (count-- > 0)
                {
                    reader.ReadByte(); // flag?
                    var keysize = (reader.ReadByte() << 8) | reader.ReadByte();
                    var buffer = new byte[keysize];
                    reader.Read(buffer, 0, keysize);
                    var key = Encoding.UTF8.GetString(buffer);
                    var tag = GetExtendedTag(key);

                    reader.ReadByte(); // flag?
                    reader.ReadByte(); // flag?
                    var valuesize = ReadUInt32(reader);
                    buffer = new byte[valuesize];
                    reader.Read(buffer, 0, buffer.Length);
                    var value = Encoding.UTF8.GetString(buffer, 0, buffer.Length);

                    if (tag != Mp4Tag.None)
                        _tags[tag] = value;
                }
            }

            block = _blocks.Find(b => b.Tag == "moov.udta.Xtra");
            if (block.Size > 0)
            {
                reader.BaseStream.Seek(block.Address + 8, SeekOrigin.Begin);
                remaining = block.Size - 8;
                while (remaining > 0)
                {
                    var tagsize = ReadUInt32(reader);
                    var keysize = ReadUInt32(reader);
                    var buffer = new byte[keysize];
                    reader.Read(buffer, 0, (int)keysize);
                    var key = Encoding.UTF8.GetString(buffer);
                    var tag = GetWMTag(key);

                    reader.BaseStream.Seek(10, SeekOrigin.Current); // skip flags
                    buffer = new byte[tagsize - keysize - 8 - 10];
                    reader.Read(buffer, 0, buffer.Length);
                    var value = Encoding.Unicode.GetString(buffer, 0, buffer.Length - 2); // expect null terminated

                    if (tag != Mp4Tag.None)
                        _tags[tag] = value;

                    remaining -= tagsize;
                }
            }
        }

        internal static ItemListDataType GetDataType(Mp4Tag type)
        {
            switch (type)
            {
                case Mp4Tag.Album:
                case Mp4Tag.Artist:
                case Mp4Tag.Comment:
                case Mp4Tag.Description:
                case Mp4Tag.Encoder:
                case Mp4Tag.LongDescription:
                case Mp4Tag.Title:
                case Mp4Tag.ReleaseDate:
                case Mp4Tag.SortAlbum:
                case Mp4Tag.SortArtist:
                case Mp4Tag.SortTitle:
                case Mp4Tag.SortTvShowName:
                case Mp4Tag.TvNetwork:
                case Mp4Tag.TvShowName:
                    return ItemListDataType.Utf8String;

                case Mp4Tag.CoverArt:
                    return ItemListDataType.Image;

                case Mp4Tag.DiskNumber: // DiskNumber and TrackNumber may be bytes instead of Int32s
                case Mp4Tag.Genre:
                case Mp4Tag.Rating:
                case Mp4Tag.TrackNumber:
                case Mp4Tag.TvEpisode:
                case Mp4Tag.TvSeason:
                    return ItemListDataType.Int32;

                default:
                    throw new NotImplementedException("No mapping for " + type);
            }
        }

        private static Mp4Tag GetExtendedTag(string key)
        {
            foreach (Mp4Tag tag in Enum.GetValues(typeof(Mp4Tag)))
            {
                if (GetExtendedTag(tag) == key)
                    return tag;
            }

            return Mp4Tag.None;
        }

        internal static string GetExtendedTag(Mp4Tag type)
        {
            switch (type)
            {
                case Mp4Tag.TvEpisode:
                    return "tvepisode";
                case Mp4Tag.TvSeason:
                    return "tvseason";
                default: 
                    return null;
            }
        }

        private static Mp4Tag GetWMTag(string key)
        {
            foreach (Mp4Tag tag in Enum.GetValues(typeof(Mp4Tag)))
            {
                if (GetWMPropertyKey(tag) == key)
                    return tag;
            }

            return Mp4Tag.None;
        }

        internal static string GetWMPropertyKey(Mp4Tag type)
        {
            switch (type)
            {
                case Mp4Tag.ParentalRating:
                    return "WM/ParentalRating";

                default:
                    return null;
            }
        }

        internal enum ItemListDataType
        {
            None,
            Utf8String,
            Int32,
            Image,
        }

        internal void GetAllBlocks(BinaryReader reader)
        {
            _blocks.Clear();
            StringBuilder builder = new StringBuilder();
            ReadBlocks(reader, builder, reader.BaseStream.Length);
        }

        private void ReadBlocks(BinaryReader reader, StringBuilder builder, long stopAddress)
        {
            while (reader.BaseStream.Position < stopAddress)
            {
                Mp4Block block = new Mp4Block();
                block.Address = reader.BaseStream.Position;
                block.Size = ReadUInt32(reader);
                if (block.Size == 0)
                    break;

                reader.Read(_buffer, 0, 4);
                if (_buffer[0] < 'A' || (_buffer[0] > 'z' && _buffer[0] != 0xA9) || 
                    _buffer[1] < 'A' || _buffer[1] > 'z' || 
                    _buffer[2] < 'A' || _buffer[2] > 'z' || 
                    _buffer[3] < 'A' || _buffer[3] > 'z')
                {
                    return;
                }

                if (builder.Length > 0)
                    builder.Append('.');

                builder.Append((char)_buffer[0]);
                builder.Append((char)_buffer[1]);
                builder.Append((char)_buffer[2]);
                builder.Append((char)_buffer[3]);

                block.Tag = builder.ToString();
                _blocks.Add(block);

                if (block.Tag == "moov.udta.meta")
                    reader.BaseStream.Seek(4, SeekOrigin.Current);

                ReadBlocks(reader, builder, block.Address + block.Size);

                builder.Length -= 4;
                if (builder.Length > 0)
                    builder.Length--;

                reader.BaseStream.Seek(block.Address + block.Size, SeekOrigin.Begin);
            }
        }
    }

    /// <summary>
    /// Defines a block within the MP4 file.
    /// </summary>
    public enum Mp4Tag : uint
    {
        /// <summary>
        /// Unspecified
        /// </summary>
        None = 0,

        /// <summary>
        /// The title of the content.
        /// </summary>
        Title = 0xA96E616D, // nam

        /// <summary>
        /// A modified form of the title used for sorting. Typically the Title value without any leading articles (A/AN/THE).
        /// </summary>
        SortTitle = 0x736F6E6D, // sonm

        /// <summary>
        /// A description of the content.
        /// </summary>
        Description = 0x64657363, // desc (limited to 255 bytes)

        /// <summary>
        /// A long description of the content.
        /// </summary>
        LongDescription = 0x6C646573, // ldes (not limited to 255 bytes)

        /// <summary>
        /// The genre of the content.
        /// </summary>
        Genre = 0x676E7265, // gnre

        /// <summary>
        /// An image to show to help identify the content.
        /// </summary>
        CoverArt = 0x636F7672, // covr

        /// <summary>
        /// The name of the television show associated to the content.
        /// </summary>
        TvShowName = 0x74767368, // tvsh

        /// <summary>
        /// A modified form of the TvShowName used for sorting.
        /// </summary>
        SortTvShowName = 0x736F736E, // sosn

        /// <summary>
        /// The season of the television show associated to the content in which the episode aired.
        /// </summary>
        TvSeason = 0x7476736E, // tvsn

        /// <summary>
        /// The index of the episode within the season in which the episode aired.
        /// </summary>
        TvEpisode = 0x74766573, // tves

        /// <summary>
        /// The network on which the television episode aired.
        /// </summary>
        TvNetwork = 0x74766E6E, // tvnn

        /// <summary>
        /// The rating of the content.
        /// </summary>
        Rating = 0x72746E67, // rtng

        /// <summary>
        /// What was used to generate the content.
        /// </summary>
        Encoder = 0xA9746F6F, // too

        /// <summary>
        /// The artist associated to the content.
        /// </summary>
        Artist = 0xA9415254, // ART

        /// <summary>
        /// A modified form of the artist name used for sorting.
        /// </summary>
        SortArtist = 0x736F6172, // soar

        /// <summary>
        /// The album associated to the content.
        /// </summary>
        Album = 0xA9616C62, // alb

        /// <summary>
        /// A modified form of the album name used for sorting.
        /// </summary>
        SortAlbum = 0x736F616C, // soal

        /// <summary>
        /// The date the associated content was released.
        /// </summary>
        ReleaseDate = 0xA9646179, // day

        /// <summary>
        /// The index of the media associated to the content within the release if released on multiple media (i.e. multiple CDs)
        /// </summary>
        DiskNumber = 0x6469736B, // disk

        /// <summary>
        /// The track within the media the content was released on.
        /// </summary>
        TrackNumber = 0x74726B6E, // trkn

        /// <summary>
        /// A comment about the content
        /// </summary>
        Comment = 0xA9636D74, // cmt

        /// <summary>
        /// The parental rating of the content (Windows Media extension - not part of MP4 spec)
        /// </summary>
        ParentalRating = 0x00000001, // WM/ParentalRating
    }
}
