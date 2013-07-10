using System;
using System.IO;

namespace Jamiras.IO
{
    /// <summary>
    /// Provides a way to read bits from a <see cref="Stream"/>.
    /// </summary>
    public class BitReader
    {
        /// <summary>
        /// Constructs a new <see cref="BitReader"/>.
        /// </summary>
        /// <param name="stream">Stream to read bits from.</param>
        public BitReader(Stream stream)
        {
            _stream = stream;
            _readBuffer = new byte[4];
        }

        private readonly Stream _stream;
        private readonly byte[] _readBuffer;
        private uint _currentData;
        private int _availableBits;

        internal static readonly uint[] Masks = { 0,
                                                  0x00000001, 0x00000003, 0x00000007, 0x0000000F,
                                                  0x0000001F, 0x0000003F, 0x0000007F, 0x000000FF,
                                                  0x000001FF, 0x000003FF, 0x000007FF, 0x00000FFF,
                                                  0x00001FFF, 0x00003FFF, 0x00007FFF, 0x0000FFFF,
                                                  0x0001FFFF, 0x0003FFFF, 0x0007FFFF, 0x000FFFFF,
                                                  0x001FFFFF, 0x003FFFFF, 0x007FFFFF, 0x00FFFFFF,
                                                  0x01FFFFFF, 0x03FFFFFF, 0x07FFFFFF, 0x0FFFFFFF,
                                                  0x1FFFFFFF, 0x3FFFFFFF, 0x7FFFFFFF, 0xFFFFFFFF };

        /// <summary>
        /// Reads the next bit from the stream.
        /// </summary>
        /// <returns><c>true</c> if the bit was set, <c>false</c> if not.</returns>
        public bool ReadBit()
        {
            if (_availableBits == 0)
            {
                FillCurrentData();
                if (_availableBits == 0)
                    return false;
            }

            _availableBits--;

            var bit = Masks[_availableBits] + 1;
            return ((_currentData & bit) != 0);
        }

        /// <summary>
        /// Reads the next <paramref name="bits"/> bits from the stream.
        /// </summary>
        /// <param name="bits">Number of bits to read.</param>
        /// <returns>Integer value constructed from the read bits.</returns>
        public int ReadBits(int bits)
        {
            if (bits < 1 || bits > 31)
                throw new ArgumentOutOfRangeException("bits", "bits must be between 1 and 31");

            uint value = _currentData;
            if (bits <= _availableBits)
            {
                _availableBits -= bits;

                if (_availableBits > 0)
                {
                    value >>= _availableBits;
                    _currentData &= Masks[_availableBits];
                }
            }
            else
            {
                int additionalBits = bits - _availableBits;

                FillCurrentData();
                if (additionalBits > _availableBits)
                    additionalBits = _availableBits;

                if (additionalBits > 0)
                {
                    value <<= additionalBits;
                    value |= (uint)ReadBits(additionalBits);
                }
            }

            return (int)value;
        }

        private void FillCurrentData()
        {
            var read = _stream.Read(_readBuffer, 0, 4);
            switch (read)
            {
                case 0:
                    _currentData = 0;
                    _availableBits = 0;
                    break;

                case 1:
                    _currentData = _readBuffer[0];
                    _availableBits = 8;
                    break;

                case 2:
                    _currentData = (uint)((_readBuffer[0] << 8) | _readBuffer[1]);
                    _availableBits = 16;
                    break;

                case 3:
                    _currentData = (uint)((_readBuffer[0] << 16) | (_readBuffer[1] << 8) | _readBuffer[2]);
                    _availableBits = 24;
                    break;

                default:
                    _currentData = (uint)((_readBuffer[0] << 24) | (_readBuffer[1] << 16) | (_readBuffer[2] << 8) | _readBuffer[3]);
                    _availableBits = 32;
                    break;
            }
        }

        /// <summary>
        /// Gets or sets the current bit offset within the stream.
        /// </summary>
        public int Position
        {
            get
            {
                return (int)(_stream.Position * 8) - _availableBits;
            }
            set
            {
                if (Position != value)
                {
                    var aligned = value / 32;
                    _stream.Seek(aligned * 4, SeekOrigin.Begin);

                    var skipBits = value % 32;
                    if (skipBits > 0)
                    {
                        FillCurrentData();

                        if (skipBits < _availableBits)
                            _availableBits -= skipBits;
                        else
                            _availableBits = 0;

                        _currentData &= Masks[_availableBits];
                    }
                }
            }
        }

        /// <summary>
        /// Gets whether or not there are more bits to be read.
        /// </summary>
        /// <value><c>true</c> if there are no more bits to read, <c>false</c> if there are.</value>
        public bool IsEndOfStream 
        {
            get { return (_availableBits == 0 && _stream.Position == _stream.Length); }
        }
    }
}
