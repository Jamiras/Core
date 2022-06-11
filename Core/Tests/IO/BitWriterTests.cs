using System.IO;
using Jamiras.IO;
using NUnit.Framework;

namespace Jamiras.Core.Tests.IO
{
    [TestFixture]
    class BitWriterTests
    {
        [Test]
        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(3, 2)]
        [TestCase(4, 3)]
        [TestCase(7, 3)]
        [TestCase(8, 4)]
        [TestCase(15, 4)]
        [TestCase(16, 5)]
        [TestCase(31, 5)]
        [TestCase(32, 6)]
        [TestCase(63, 6)]
        [TestCase(64, 7)]
        [TestCase(127, 7)]
        [TestCase(128, 8)]
        [TestCase(255, 8)]
        [TestCase(256, 9)]
        [TestCase(511, 9)]
        [TestCase(512, 10)]
        [TestCase(1023, 10)]
        [TestCase(1024, 11)]
        [TestCase(2047, 11)]
        [TestCase(2048, 12)]
        [TestCase(4095, 12)]
        [TestCase(4096, 13)]
        [TestCase(8191, 13)]
        [TestCase(8192, 14)]
        [TestCase(16383, 14)]
        [TestCase(16384, 15)]
        [TestCase(32767, 15)]
        [TestCase(32768, 16)]
        [TestCase(65535, 16)]
        [TestCase(0x00010000, 17)]
        [TestCase(0x0001FFFF, 17)]
        [TestCase(0x00020000, 18)]
        [TestCase(0x0003FFFF, 18)]
        [TestCase(0x00040000, 19)]
        [TestCase(0x0007FFFF, 19)]
        [TestCase(0x00080000, 20)]
        [TestCase(0x000FFFFF, 20)]
        [TestCase(0x00100000, 21)]
        [TestCase(0x001FFFFF, 21)]
        [TestCase(0x00200000, 22)]
        [TestCase(0x003FFFFF, 22)]
        [TestCase(0x00400000, 23)]
        [TestCase(0x007FFFFF, 23)]
        [TestCase(0x00800000, 24)]
        [TestCase(0x00FFFFFF, 24)]
        [TestCase(0x01000000, 25)]
        [TestCase(0x01FFFFFF, 25)]
        [TestCase(0x02000000, 26)]
        [TestCase(0x03FFFFFF, 26)]
        [TestCase(0x04000000, 27)]
        [TestCase(0x07FFFFFF, 27)]
        [TestCase(0x08000000, 28)]
        [TestCase(0x0FFFFFFF, 28)]
        [TestCase(0x10000000, 29)]
        [TestCase(0x1FFFFFFF, 29)]
        [TestCase(0x20000000, 30)]
        [TestCase(0x3FFFFFFF, 30)]
        [TestCase(0x40000000, 31)]
        [TestCase(0x7FFFFFFF, 31)]
        public void TestBitsNeeded(int value, int bitsNeeded)
        {
            Assert.That(BitWriter.BitsNeeded(value), Is.EqualTo(bitsNeeded));
        }

        [Test]
        public void TestWriteBit()
        {
            var stream = new MemoryStream();
            var writer = new BitWriter(stream);

            var values = new bool[]
            {
                true, false, true, false, false, false, true, true,
                false, true, true, true, true, true, true, false,
                true, false, false, false, false, false, false, true,
                true, true, true, true, false, false, true, false
            };

            for (int i = 0; i < 32; i++)
            {
                writer.WriteBit(values[i]);
                Assert.That(writer.Position, Is.EqualTo(i + 1), "after bit " + i);
            }

            writer.Flush();
            var bytes = stream.ToArray();
            Assert.That(bytes, Is.EqualTo(new byte[] { 0xA3, 0x7E, 0x81, 0xF2 }));
        }

        [Test]
        public void TestWriteBits8()
        {
            var stream = new MemoryStream();
            var writer = new BitWriter(stream);

            writer.WriteBits(3, 5); // 101
            writer.WriteBits(2, 1); // 01
            writer.WriteBits(3, 3); // 011
            Assert.That(writer.Position, Is.EqualTo(8));

            writer.Flush();
            var bytes = stream.ToArray();
            Assert.That(bytes, Is.EqualTo(new byte[] { 0xAB }));
        }

        [Test]
        public void TestWriteBits7()
        {
            var stream = new MemoryStream();
            var writer = new BitWriter(stream);

            writer.WriteBits(3, 5); // 101
            writer.WriteBits(2, 1); // 01
            writer.WriteBits(2, 3); // 11
            Assert.That(writer.Position, Is.EqualTo(7));

            writer.Flush();
            var bytes = stream.ToArray();
            Assert.That(bytes, Is.EqualTo(new byte[] { 0xAE }));
        }

        [Test]
        public void TestWriteBits9()
        {
            var stream = new MemoryStream();
            var writer = new BitWriter(stream);

            writer.WriteBits(3, 5); // 101
            writer.WriteBits(2, 1); // 01
            writer.WriteBits(4, 3); // 0011
            Assert.That(writer.Position, Is.EqualTo(9));

            writer.Flush();
            var bytes = stream.ToArray();
            Assert.That(bytes, Is.EqualTo(new byte[] { 0xA9, 0x80 }));
        }

        [Test]
        public void TestWriteBits16()
        {
            var stream = new MemoryStream();
            var writer = new BitWriter(stream);

            writer.WriteBits(3, 5); // 101
            writer.WriteBits(2, 1); // 01
            writer.WriteBits(11, 0x39B); // 011 1001 1011
            Assert.That(writer.Position, Is.EqualTo(16));

            writer.Flush();
            var bytes = stream.ToArray();
            Assert.That(bytes, Is.EqualTo(new byte[] { 0xAB, 0x9B }));
        }

        [Test]
        public void TestWriteBits24()
        {
            var stream = new MemoryStream();
            var writer = new BitWriter(stream);

            writer.WriteBits(3, 5); // 101
            writer.WriteBits(2, 1); // 01
            writer.WriteBits(13, 0x1A74); // 1 1010 0111 0100
            writer.WriteBits(6, 19); // 010011
            Assert.That(writer.Position, Is.EqualTo(24));

            writer.Flush();
            var bytes = stream.ToArray();
            Assert.That(bytes, Is.EqualTo(new byte[] { 0xAE, 0x9D, 0x13 }));
        }

        [Test]
        public void TestWriteBits32()
        {
            var stream = new MemoryStream();
            var writer = new BitWriter(stream);

            writer.WriteBits(3, 5); // 101
            writer.WriteBits(2, 1); // 01
            writer.WriteBits(27, 0x39B4DAC);
            Assert.That(writer.Position, Is.EqualTo(32));

            writer.Flush();
            var bytes = stream.ToArray();
            Assert.That(bytes, Is.EqualTo(new byte[] { 0xAB, 0x9B, 0x4D, 0xAC }));
        }

        [Test]
        public void TestWriteBitsValueTooLarge()
        {
            var stream = new MemoryStream();
            var writer = new BitWriter(stream);
            Assert.That(() => writer.WriteBits(3, 8), Throws.ArgumentException);
        }

        [Test]
        public void TestPartialFlushWhenPositionChanges()
        {
            var stream = new MemoryStream();
            var writer = new BitWriter(stream);

            writer.WriteBits(11, 0x2B6); // 01010[1101]10
            Assert.That(writer.Position, Is.EqualTo(11));

            writer.Position = 5;
            Assert.That(writer.Position, Is.EqualTo(5));

            writer.WriteBits(4, 6); // 0110 => 01010 0110 10 
            Assert.That(writer.Position, Is.EqualTo(9));

            writer.Flush();
            var bytes = stream.ToArray();
            Assert.That(bytes, Is.EqualTo(new byte[] { 0x53, 0x40 })); // 0101 0011 0100 0000
        }

        [Test]
        public void TestFlushAndContinue()
        {
            var stream = new MemoryStream();
            var writer = new BitWriter(stream);

            writer.WriteBits(2, 3); // 01010[1101]10
            Assert.That(writer.Position, Is.EqualTo(2));

            writer.Flush();
            Assert.That(writer.Position, Is.EqualTo(2));

            var bytes = stream.ToArray();
            Assert.That(bytes, Is.EqualTo(new byte[] { 0xC0 }));

            writer.WriteBits(4, 6); 
            writer.Flush();
            Assert.That(writer.Position, Is.EqualTo(6));

            bytes = stream.ToArray();
            Assert.That(bytes, Is.EqualTo(new byte[] { 0xD8 })); // 11 0110 00
        }
    }
}
