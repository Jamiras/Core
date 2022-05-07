using System;
using System.IO;

namespace Jamiras.IO
{
    /// <summary>
    /// Provides a way to write bits to a <see cref="Stream"/>.
    /// </summary>
    public class BitWriter
    {
        /// <summary>
        /// Constructs a new <see cref="BitWriter"/>.
        /// </summary>
        /// <param name="stream">Stream to write bits to.</param>
        public BitWriter(Stream stream)
        {
            _stream = stream;
        }

        private readonly Stream _stream;
        private byte _currentData;
        private int _pendingBits;

        /// <summary>
        /// Writes a bit to the stream.
        /// </summary>
        public void WriteBit(bool value)
        {
            _currentData <<= 1;

            if (value)
                _currentData |= 1;

            _pendingBits++;
            if (_pendingBits == 8)
            {
                _stream.WriteByte(_currentData);
                _currentData = 0;
                _pendingBits = 0;
            }
        }


        /// <summary>
        /// Writes a series of bits to the stream.
        /// </summary>
        /// <param name="bits">Number of bits to write.</param>
        /// <param name="value">Integer value containing the bits to write.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="bits"/> must be between 1 and 31.</exception>
        /// <exception cref="ArgumentException"><paramref name="bits"/> is not large enough for the provided <paramref name="value"/>.</exception>
        public void WriteBits(int bits, int value)
        {
            if (bits < 1 || bits > 31)
                throw new ArgumentOutOfRangeException("bits", "bits must be between 1 and 31");
            if ((value & ~BitReader.Masks[bits]) != 0)
                throw new ArgumentException("value contains more than the specified number of bits", "bits");

            int availableBits = 8 - _pendingBits;
            if (bits <= availableBits)
            {
                if (_pendingBits > 0)
                {
                    _currentData <<= bits;
                    _currentData |= (byte)value;
                }
                else
                {
                    _currentData = (byte)value;
                }

                _pendingBits += bits;
                if (_pendingBits == 8)
                {
                    _stream.WriteByte(_currentData);
                    _currentData = 0;
                    _pendingBits = 0;
                }
            }
            else
            {
                if (_pendingBits > 0)
                {
                    _currentData <<= availableBits;
                    _currentData |= (byte)(value >> (bits - availableBits));
                    _stream.WriteByte(_currentData);
                    bits -= availableBits;
                }

                while (bits >= 8)
                {
                    bits -= 8;
                    _currentData = (byte)(value >> bits);
                    _stream.WriteByte(_currentData);
                }

                if (bits > 0)
                {
                    _currentData = (byte)(value & BitReader.Masks[bits]);
                    _pendingBits = bits;
                }
                else
                {
                    _currentData = 0;
                    _pendingBits = 0;
                }
            }
        }

        /// <summary>
        /// Gets or sets the current bit offset within the stream.
        /// </summary>
        public int Position
        {
            get
            {
                return (int)(_stream.Position * 8) + _pendingBits;
            }
            set
            {
                if (Position != value)
                {
                    if (_pendingBits > 0)
                        FlushBufferedData();

                    var aligned = value / 8;
                    _stream.Seek(aligned, SeekOrigin.Begin);

                    _pendingBits = value % 8;
                    if (_pendingBits == 0)
                    {
                        _currentData = 0;
                    }
                    else
                    {
                        _currentData = (byte)_stream.ReadByte();
                        _stream.Seek(-1, SeekOrigin.Current);
                        _currentData >>= (8 - _pendingBits);
                    }
                }
            }
        }

        /// <summary>
        /// Causes any buffered data to be written to the underlying stream.
        /// </summary>
        public void Flush()
        {
            if (_pendingBits > 0)
                FlushBufferedData();

            _stream.Flush();
            
            if (_pendingBits > 0)
                _stream.Seek(-1, SeekOrigin.Current);
        }

        private void FlushBufferedData()
        {
            int neededBits = 8 - _pendingBits;
            byte flushData = (byte)(_currentData << neededBits);

            if (_stream.Position < _stream.Length)
            {
                flushData |= (byte)(_stream.ReadByte() & BitReader.Masks[neededBits]);
                _stream.Seek(-1, SeekOrigin.Current);
            }

            _stream.WriteByte(flushData);
        }

        /// <summary>
        /// Determines the number of bits required to represent a value.
        /// </summary>
        /// <param name="value">Value to determine number of bits required for.</param>
        /// <returns>Number of bits required to represent the value.</returns>
        public static int BitsNeeded(int value)
        {
            uint masked = ((uint)value & 0xFFFF0000);
            if (masked != 0)
                return BitsNeeded16((int)(masked >> 16)) + 16;

            return BitsNeeded16(value);
        }

        private static int BitsNeeded16(int value)
        {
            int masked = (value & 0xFF00);
            if (masked != 0)
                return BitsNeeded8(masked >> 8) + 8;

            return BitsNeeded8(value);
        }

        private static int BitsNeeded8(int value)
        {
            int masked = (value & 0xF0);
            if (masked != 0)
                return _bitsNeeded4[masked >> 4] + 4;

            return _bitsNeeded4[value];
        }

        private static readonly byte[] _bitsNeeded4 = new byte[] { 0, 1, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4 };
    }
}
