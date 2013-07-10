using System.IO;
using Jamiras.IO;
using NUnit.Framework;

namespace Jamiras.Core.Tests.IO
{
    [TestFixture]
    class BitReaderTests
    {
        private readonly byte[] _testData1 = new byte[] { 0xA3, 0x7E, 0x81, 0xF2 }; // 1010 0011  0111 1110  1000 0001  1111 0010

        private readonly byte[] _testData2 = new byte[] { 0xA3, 0x7E, 0x81, 0xF2,   // 1010 0011  0111 1110  1000 0001  1111 0010
                                                          0xCC, 0x00, 0x38, 0xBB }; // 1100 1100  0000 0000  0011 1000  1011 1011

        [Test]
        public void TestReadBit()
        {
            var values = new bool[]
            {
                true, false, true, false, false, false, true, true,
                false, true, true, true, true, true, true, false,
                true, false, false, false, false, false, false, true,
                true, true, true, true, false, false, true, false
            };

            var reader = new BitReader(new MemoryStream(_testData1));

            for (int i = 0; i < 32; i++)
            {
                if (values[i])
                    Assert.That(reader.ReadBit(), Is.True, "bit " + i);
                else
                    Assert.That(reader.ReadBit(), Is.False, "bit " + i);

                Assert.That(reader.Position, Is.EqualTo(i + 1), "after read bit " + i);
            }

            Assert.That(reader.ReadBit(), Is.False, "read past end of stream");
            Assert.That(reader.Position, Is.EqualTo(32), "after read past end of stream");
            Assert.That(reader.IsEndOfStream, Is.True, "IsEndOfStream");
        }

        [Test]
        [TestCase(0, 1, 1)]
        [TestCase(1, 1, 0)]
        [TestCase(0, 4, 10)]
        [TestCase(1, 4, 4)]
        [TestCase(7, 3, 5)]
        [TestCase(6, 8, 0xDF)]
        [TestCase(6, 16, 0xDFA0)]
        public void TestReadBitsInChunk(int offset, int bits, int expectedValue)
        {
            var reader = new BitReader(new MemoryStream(_testData1));
            reader.Position = offset;
            Assert.That(reader.Position, Is.EqualTo(offset), "position not updated");

            var value = reader.ReadBits(bits);
            Assert.That(value, Is.EqualTo(expectedValue));
        }

        [Test]
        [TestCase(30, 4, 0x0B)]
        [TestCase(28, 8, 0x2C)]
        [TestCase(24, 16, 0xF2CC)]
        [TestCase(25, 11, 0x72C)] // 111 0010 1100
        public void TestReadBitsAcrossChunks(int offset, int bits, int expectedValue)
        {
            var reader = new BitReader(new MemoryStream(_testData2));
            reader.Position = offset;
            Assert.That(reader.Position, Is.EqualTo(offset), "position not updated");

            var value = reader.ReadBits(bits);
            Assert.That(value, Is.EqualTo(expectedValue));
        }

        [Test]
        [TestCase(30, 4, 2)]
        [TestCase(28, 8, 2)]
        [TestCase(24, 16, 0xF2)]
        [TestCase(25, 11, 0x72)]
        [Description("Read past end will only read as many bits as are remaining")]
        public void TestReadBitsAtEnd(int offset, int bits, int expectedValue)
        {
            var reader = new BitReader(new MemoryStream(_testData1));
            reader.Position = offset;
            Assert.That(reader.Position, Is.EqualTo(offset), "position not updated");

            var value = reader.ReadBits(bits);
            Assert.That(value, Is.EqualTo(expectedValue));

            Assert.That(reader.Position, Is.EqualTo(32), "position after read");
            Assert.That(reader.IsEndOfStream, Is.True, "IsEndOfStream");
        }

        [Test]
        public void TestPartialChunk1()
        {
            var data = new byte[] { 0xAB };
            var reader = new BitReader(new MemoryStream(data));

            Assert.That(reader.ReadBits(3), Is.EqualTo(5));
            Assert.That(reader.ReadBits(5), Is.EqualTo(11));
            Assert.That(reader.IsEndOfStream, Is.True);
        }

        [Test]
        public void TestPartialChunk2()
        {
            var data = new byte[] { 0xAB, 0xCD };
            var reader = new BitReader(new MemoryStream(data));

            Assert.That(reader.ReadBits(3), Is.EqualTo(5)); // 101
            Assert.That(reader.ReadBits(6), Is.EqualTo(0x17)); // 010111
            Assert.That(reader.ReadBits(7), Is.EqualTo(0x4D)); // 1001101
            Assert.That(reader.IsEndOfStream, Is.True);
        }

        [Test]
        public void TestPartialChunk3()
        {
            var data = new byte[] { 0xAB, 0xCD, 0xEF };
            var reader = new BitReader(new MemoryStream(data));

            Assert.That(reader.ReadBits(3), Is.EqualTo(5)); // 101
            Assert.That(reader.ReadBits(6), Is.EqualTo(0x17)); // 010111
            Assert.That(reader.ReadBits(9), Is.EqualTo(0x137)); // 100110111
            Assert.That(reader.ReadBits(6), Is.EqualTo(0x2F)); // 101111
            Assert.That(reader.IsEndOfStream, Is.True);
        }
    }
}
