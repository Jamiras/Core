using System;
using System.IO;
using System.Linq;
using Jamiras.IO;
using Jamiras.Services;
using Moq;
using NUnit.Framework;

namespace Jamiras.Core.Tests.IO
{
    [TestFixture]
    public class FileBundleTests
    {
        private DateTime fileTime = new DateTime(2014, 12, 5, 11, 7, 15, DateTimeKind.Utc);
        private byte[] fileTimeBinary = new byte[] { 0x80, 0x73, 0xC1, 0xC2, 0x92, 0xDE, 0xD1, 0x48 };

        private readonly byte[] emptyFile = new byte[]
        {
            (byte)'J', (byte)'B', (byte)'D', 1, // signature
            3, 0, 0, 0, // buckets
            0, 0, 0, 0, // bucket[0]
            0, 0, 0, 0, // bucket[1]
            0, 0, 0, 0, // bucket[2]
            0, 0, 0, 0, // bucket[free]
        };

        private readonly byte[] fooTxtFile = new byte[]
        {
            (byte)'J', (byte)'B', (byte)'D', 1, // signature
            3, 0, 0, 0, // buckets
            0, 0, 0, 0, // bucket[0]
            0, 0, 0, 0, // bucket[1]
            24, 0, 0, 0, // bucket[2]
            0, 0, 0, 0, // bucket[free]

            0, 0, 0, 0, // next
            4, 0, 0, 0, // size
            0x80, 0x73, 0xC1, 0xC2, 0x92, 0xDE, 0xD1, 0x48, // date
            7, 0x66, 0x6F, 0x6F, 0x2E, 0x74, 0x78, 0x74, // foo.txt
            1, 2, 3, 4, // content
        };

        private readonly byte[] foo2TxtFile = new byte[]
        {
            (byte)'J', (byte)'B', (byte)'D', 1, // signature
            3, 0, 0, 0, // buckets
            0, 0, 0, 0, // bucket[0]
            52, 0, 0, 0, // bucket[1]
            24, 0, 0, 0, // bucket[2]
            0, 0, 0, 0, // bucket[free]

            0, 0, 0, 0, // next
            4, 0, 0, 0, // size
            0x80, 0x73, 0xC1, 0xC2, 0x92, 0xDE, 0xD1, 0x48, // date
            7, 0x66, 0x6F, 0x6F, 0x2E, 0x74, 0x78, 0x74, // foo.txt
            1, 2, 3, 4, // content

            0, 0, 0, 0, // next
            4, 0, 0, 0, // size
            0x80, 0x73, 0xC1, 0xC2, 0x92, 0xDE, 0xD1, 0x48, // date
            8, 0x66, 0x6F, 0x6F, 0x32, 0x2E, 0x74, 0x78, 0x74, // foo2.txt
            1, 2, 3, 4, // content
        };

        private readonly byte[] barJpgFile = new byte[]
        {
            (byte)'J', (byte)'B', (byte)'D', 1, // signature
            3, 0, 0, 0, // buckets
            0, 0, 0, 0, // bucket[0]
            0, 0, 0, 0, // bucket[1]
            24, 0, 0, 0, // bucket[2]
            0, 0, 0, 0, // bucket[free]

            52, 0, 0, 0, // next
            4, 0, 0, 0, // size
            0x80, 0x73, 0xC1, 0xC2, 0x92, 0xDE, 0xD1, 0x48, // date
            7, 0x66, 0x6F, 0x6F, 0x2E, 0x74, 0x78, 0x74, // foo.txt
            1, 2, 3, 4, // content

            0, 0, 0, 0, // next
            4, 0, 0, 0, // size
            0x80, 0x73, 0xC1, 0xC2, 0x92, 0xDE, 0xD1, 0x48, // date
            7, 0x62, 0x61, 0x72, 0x2E, 0x6A, 0x70, 0x67, // bar.jpg
            1, 2, 3, 4, // content
        };

        private const string BundleFileName = "bundle.jbd";

        [Test]
        public void TestEmptyFile()
        {
            var mockFileSystem = new Mock<IFileSystemService>();
            mockFileSystem.Setup(f => f.FileExists(BundleFileName)).Returns(true);
            mockFileSystem.Setup(f => f.OpenFile(BundleFileName, OpenFileMode.Read)).Returns(new MemoryStream(emptyFile));

            var bundle = new FileBundle(BundleFileName, mockFileSystem.Object);

            Assert.That(bundle.GetDirectories(), Is.Empty);
            Assert.That(bundle.GetFiles(), Is.Empty);
            Assert.That(bundle.FileName, Is.EqualTo(BundleFileName));
        }

        [Test]
        public void TestInvalidSignature()
        {
            var badFile = new byte[emptyFile.Length];
            Array.Copy(emptyFile, badFile, emptyFile.Length);
            badFile[1] = (byte)'Q';

            var mockFileSystem = new Mock<IFileSystemService>();
            mockFileSystem.Setup(f => f.FileExists(BundleFileName)).Returns(true);
            mockFileSystem.Setup(f => f.OpenFile(BundleFileName, OpenFileMode.Read)).Returns(new MemoryStream(badFile));

            Assert.That(() => new FileBundle(BundleFileName, mockFileSystem.Object), Throws.InvalidOperationException);
        }

        [Test]
        public void TestUnsupportedVersion()
        {
            var badFile = new byte[emptyFile.Length];
            Array.Copy(emptyFile, badFile, emptyFile.Length);
            badFile[3] = 0xFF;

            var mockFileSystem = new Mock<IFileSystemService>();
            mockFileSystem.Setup(f => f.FileExists(BundleFileName)).Returns(true);
            mockFileSystem.Setup(f => f.OpenFile(BundleFileName, OpenFileMode.Read)).Returns(new MemoryStream(badFile));

            Assert.That(() => new FileBundle(BundleFileName, mockFileSystem.Object), Throws.InvalidOperationException);
        }

        [Test]
        public void TestCreateBundle()
        {
            var memoryStream = new MemoryStream();
            var mockFileSystem = new Mock<IFileSystemService>();
            mockFileSystem.Setup(f => f.FileExists(BundleFileName)).Returns(false);
            mockFileSystem.Setup(f => f.CreateFile(BundleFileName)).Returns(memoryStream);

            var bundle = new FileBundle(BundleFileName, 3, mockFileSystem.Object);

            var buffer = memoryStream.ToArray();
            Assert.That(buffer, Is.EqualTo(emptyFile));
        }

        private class TestStream : MemoryStream
        {
            public override void Close()
            {
                // base.Close will Dispose the MemoryStream, making it impossible to open again
            }

            public override byte[] GetBuffer()
            {
                var buffer = new byte[Length];
                Array.Copy(base.GetBuffer(), buffer, buffer.Length);
                return buffer;
            }
        }

        private FileBundle CreateBundle(TestStream memoryStream)
        {
            var mockFileSystem = new Mock<IFileSystemService>();
            mockFileSystem.Setup(f => f.FileExists(BundleFileName)).Returns(false);
            mockFileSystem.Setup(f => f.CreateFile(BundleFileName)).Returns(memoryStream);
            mockFileSystem.Setup(f => f.OpenFile(BundleFileName, OpenFileMode.Read)).Returns(memoryStream);
            mockFileSystem.Setup(f => f.OpenFile(BundleFileName, OpenFileMode.ReadWrite)).Returns(memoryStream);
            var bundle = new FileBundle(BundleFileName, 3, mockFileSystem.Object);
            return bundle;
        }

        [Test]
        public void TestAddFile()
        {
            var memoryStream = new TestStream();
            var bundle = CreateBundle(memoryStream);

            var fileStream = bundle.CreateFile("foo.txt");
            var files = bundle.GetFiles().ToArray();
            Assert.That(files, Is.Empty, "file should not be available until committed");

            fileStream.Write(new byte[] {1, 2, 3, 4}, 0, 4);
            fileStream.Close();

            files = bundle.GetFiles().ToArray();
            Assert.That(files, Has.Member("foo.txt"));
            Assert.That(bundle.FileExists("foo.txt"));

            var buffer = memoryStream.GetBuffer();
            Array.Copy(fileTimeBinary, 0, buffer, 32, 8); // replace timestamp with test timestamp
            Assert.That(buffer, Is.EqualTo(fooTxtFile));
        }

        [Test]
        public void TestAddSecondFile()
        {
            var memoryStream = new TestStream();
            var bundle = CreateBundle(memoryStream);

            var fileStream = bundle.CreateFile("foo.txt");
            var files = bundle.GetFiles().ToArray();
            Assert.That(files, Is.Empty, "file should not be available until committed");

            fileStream.Write(new byte[] { 1, 2, 3, 4 }, 0, 4);
            fileStream.Close();

            fileStream = bundle.CreateFile("foo2.txt");
            fileStream.Write(new byte[] { 1, 2, 3, 4 }, 0, 4);
            fileStream.Close();

            files = bundle.GetFiles().ToArray();
            Assert.That(files, Has.Member("foo.txt"));
            Assert.That(files, Has.Member("foo2.txt"));
            Assert.That(bundle.FileExists("foo.txt"));
            Assert.That(bundle.FileExists("foo2.txt"));

            var buffer = memoryStream.GetBuffer();
            Array.Copy(fileTimeBinary, 0, buffer, 32, 8);
            Array.Copy(fileTimeBinary, 0, buffer, 60, 8);
            Assert.That(buffer, Is.EqualTo(foo2TxtFile));
        }

        [Test]
        public void TestAddSecondFileSameHash()
        {
            var memoryStream = new TestStream();
            var bundle = CreateBundle(memoryStream);

            var fileStream = bundle.CreateFile("foo.txt");
            var files = bundle.GetFiles().ToArray();
            Assert.That(files, Is.Empty, "file should not be available until committed");

            fileStream.Write(new byte[] { 1, 2, 3, 4 }, 0, 4);
            fileStream.Close();

            fileStream = bundle.CreateFile("bar.jpg");
            fileStream.Write(new byte[] { 1, 2, 3, 4 }, 0, 4);
            fileStream.Close();

            files = bundle.GetFiles().ToArray();
            Assert.That(files, Has.Member("foo.txt"));
            Assert.That(files, Has.Member("bar.jpg"));

            var buffer = memoryStream.GetBuffer();
            Array.Copy(fileTimeBinary, 0, buffer, 32, 8);
            Array.Copy(fileTimeBinary, 0, buffer, 60, 8);
            Assert.That(buffer, Is.EqualTo(barJpgFile));
        }

        private FileBundle OpenBundle(TestStream memoryStream, byte[] originalData)
        {
            memoryStream.Write(originalData, 0, originalData.Length);
            memoryStream.Seek(0, SeekOrigin.Begin);

            var mockFileSystem = new Mock<IFileSystemService>();
            mockFileSystem.Setup(f => f.FileExists(BundleFileName)).Returns(true);
            mockFileSystem.Setup(f => f.OpenFile(BundleFileName, OpenFileMode.Read)).Returns(memoryStream);
            mockFileSystem.Setup(f => f.OpenFile(BundleFileName, OpenFileMode.ReadWrite)).Returns(memoryStream);
            var bundle = new FileBundle(BundleFileName, 3, mockFileSystem.Object);
            return bundle;
        }

        [Test]
        public void TestDeleteFile()
        {
            var memoryStream = new TestStream();
            var bundle = OpenBundle(memoryStream, foo2TxtFile);

            var files = bundle.GetFiles().ToArray();
            Assert.That(files, Has.Member("foo.txt"));
            Assert.That(files, Has.Member("foo2.txt"));

            Assert.That(bundle.DeleteFile("foo2.txt"), Is.True);

            files = bundle.GetFiles().ToArray();
            Assert.That(files, Has.Member("foo.txt"));
            Assert.That(files, Has.No.Member("foo2.txt"));
            Assert.That(bundle.FileExists("foo.txt"));
            Assert.That(bundle.FileExists("foo2.txt"), Is.False);

            var expected = new byte[foo2TxtFile.Length];
            Array.Copy(foo2TxtFile, expected, expected.Length);
            expected[12] = 0; // bucket[1] is empty
            expected[20] = 52; // available points at deleted item

            var buffer = memoryStream.GetBuffer();
            Assert.That(buffer, Is.EqualTo(expected));
        }

        [Test]
        public void TestDeleteFileSameHash()
        {
            var memoryStream = new TestStream();
            var bundle = OpenBundle(memoryStream, barJpgFile);

            var files = bundle.GetFiles().ToArray();
            Assert.That(files, Has.Member("foo.txt"));
            Assert.That(files, Has.Member("bar.jpg"));

            Assert.That(bundle.DeleteFile("bar.jpg"), Is.True);

            files = bundle.GetFiles().ToArray();
            Assert.That(files, Has.Member("foo.txt"));
            Assert.That(files, Has.No.Member("bar.jpg"));

            var expected = new byte[barJpgFile.Length];
            Array.Copy(barJpgFile, expected, expected.Length);
            expected[20] = 52; // available points at deleted item
            expected[24] = 0; // non-deleted item points at nothing

            var buffer = memoryStream.GetBuffer();
            Assert.That(buffer, Is.EqualTo(expected));
        }

        [Test]
        public void TestDeleteFileSameHashFirstNode()
        {
            var memoryStream = new TestStream();
            var bundle = OpenBundle(memoryStream, barJpgFile);

            var files = bundle.GetFiles().ToArray();
            Assert.That(files, Has.Member("foo.txt"));
            Assert.That(files, Has.Member("bar.jpg"));

            Assert.That(bundle.DeleteFile("foo.txt"), Is.True);

            files = bundle.GetFiles().ToArray();
            Assert.That(files, Has.Member("bar.jpg"));
            Assert.That(files, Has.No.Member("foo.txt"));

            var expected = new byte[barJpgFile.Length];
            Array.Copy(barJpgFile, expected, expected.Length);
            expected[16] = 52; // bucket[1] points at non-deleted item
            expected[20] = 24; // available points at deleted item
            expected[24] = 0; // deleted item points at nothing

            var buffer = memoryStream.GetBuffer();
            Assert.That(buffer, Is.EqualTo(expected));
        }

        [Test]
        public void TestDeleteNonExistantFile()
        {
            var memoryStream = new TestStream();
            var bundle = OpenBundle(memoryStream, foo2TxtFile);

            var files = bundle.GetFiles().ToArray();
            Assert.That(files, Has.Member("foo.txt"));
            Assert.That(files, Has.Member("foo2.txt"));

            Assert.That(bundle.DeleteFile("bar.jpg"), Is.False);
            Assert.That(bundle.DeleteFile("foo.txt2"), Is.False);
            Assert.That(bundle.DeleteFile("\foo.txt"), Is.False);
            Assert.That(bundle.DeleteFile("bar\foo.txt"), Is.False);

            files = bundle.GetFiles().ToArray();
            Assert.That(files, Has.Member("foo.txt"));
            Assert.That(files, Has.Member("foo2.txt"));
        }
    }
}
