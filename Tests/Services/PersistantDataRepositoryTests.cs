using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Jamiras.Services;
using Moq;
using System.IO;

namespace Jamiras.Core.Tests.Services
{
    [TestFixture]
    class PersistantDataRepositoryTests
    {
        [TestFixtureSetUp]
        public void FixtureSetup()
        {
            _fileName = PersistantDataRepository.GetFileName();
        }

        [SetUp]
        public void Setup()
        {
            _mockFileSystemService = new Mock<IFileSystemService>();
            _repository = new PersistantDataRepository(_mockFileSystemService.Object);
        }

        private Mock<IFileSystemService> _mockFileSystemService;
        private PersistantDataRepository _repository;
        private string _fileName;

        private void SetupFile(string contents)
        {
            _mockFileSystemService.Setup(f => f.FileExists(_fileName)).Returns(true);
            _mockFileSystemService.Setup(f => f.OpenFile(_fileName, OpenFileMode.Read)).Returns(new MemoryStream(Encoding.UTF8.GetBytes(contents)));
        }

        private byte[] SetupFileWrite()
        {
            byte[] buffer = new byte[256];
            MemoryStream stream = new MemoryStream(buffer);
            _mockFileSystemService.Setup(f => f.FileExists(_fileName)).Returns(true);
            _mockFileSystemService.Setup(f => f.CreateFile(_fileName)).Returns(stream);
            return buffer;
        }

        private string GetFileContents(byte[] buffer)
        {
            int count = 0;
            while (buffer[count] != 0)
                count++;

            return Encoding.UTF8.GetString(buffer, 0, count);
        }

        [Test]
        public void TestGetFileName()
        {
            Assert.That(_fileName, Is.StringContaining("\\Jamiras\\"));
            Assert.That(_fileName, Is.StringEnding("\\userdata.ini"));
        }

        [Test]
        public void TestGetValueNoFile()
        {
            var value = _repository.GetValue("x");
            Assert.That(value, Is.Null);

            _mockFileSystemService.Verify(f => f.FileExists(_fileName));
            _mockFileSystemService.Verify(f => f.OpenFile(_fileName, It.IsAny<OpenFileMode>()), Times.Never());
        }

        [Test]
        public void TestGetValueFile()
        {
            SetupFile("x=3");

            var value = _repository.GetValue("x");
            Assert.That(value, Is.EqualTo("3"));
        }

        [Test]
        public void TestGetValueFileTwoEntries()
        {
            SetupFile("x=3\ny=4");

            var value = _repository.GetValue("x");
            Assert.That(value, Is.EqualTo("3"));

            value = _repository.GetValue("y");
            Assert.That(value, Is.EqualTo("4"));
        }

        [Test]
        public void TestGetValueFileTwoEntriesReversed()
        {
            SetupFile("y=4\nx=3");

            var value = _repository.GetValue("x");
            Assert.That(value, Is.EqualTo("3"));

            value = _repository.GetValue("y");
            Assert.That(value, Is.EqualTo("4"));
        }

        [Test]
        public void TestGetValueEscaped()
        {
            SetupFile("x=Multiple\\nLines");

            var value = _repository.GetValue("x");
            Assert.That(value, Is.EqualTo("Multiple\nLines"));
        }

        [Test]
        public void TestGetValueEscapedPath()
        {
            SetupFile("x=C:\\\\root\\\\node\\\\file.txt"); // \r and \n are not escape sequence. \\ is.

            var value = _repository.GetValue("x");
            Assert.That(value, Is.EqualTo("C:\\root\\node\\file.txt"));
        }

        [Test]
        public void TestSetValue()
        {
            var stream = SetupFileWrite();
            _repository.SetValue("x", "3");

            var value = _repository.GetValue("x");
            Assert.That(value, Is.EqualTo("3"));

            _mockFileSystemService.Verify(f => f.CreateFile(_fileName));
            var contents = GetFileContents(stream);
            Assert.That(contents, Is.EqualTo("x=3\r\n"));
        }

        [Test]
        public void TestSetValueEscaped()
        {
            var stream = SetupFileWrite();
            _repository.SetValue("x", "C:\\root\\node\\file.txt\nC:\\file2.txt");

            var value = _repository.GetValue("x");
            Assert.That(value, Is.EqualTo("C:\\root\\node\\file.txt\nC:\\file2.txt"));

            _mockFileSystemService.Verify(f => f.CreateFile(_fileName));
            var contents = GetFileContents(stream);
            Assert.That(contents, Is.EqualTo("x=C:\\\\root\\\\node\\\\file.txt\\nC:\\\\file2.txt\r\n"));
        }

        [Test]
        public void TestSuspendedSetValue()
        {
            var stream = SetupFileWrite();

            _repository.BeginUpdate();
            _repository.SetValue("x", "3");

            var value = _repository.GetValue("x");
            Assert.That(value, Is.EqualTo("3"));

            _mockFileSystemService.Verify(f => f.CreateFile(_fileName), Times.Never(), "file written while suspended");

            _repository.EndUpdate();
            _mockFileSystemService.Verify(f => f.CreateFile(_fileName), "file not written after resume");
            var contents = GetFileContents(stream);
            Assert.That(contents, Is.EqualTo("x=3\r\n"));
        }

        [Test]
        public void TestSuspendedMultipleSetValue()
        {
            var stream = SetupFileWrite();

            _repository.BeginUpdate();
            _repository.BeginUpdate();
            _repository.SetValue("x", "3");

            var value = _repository.GetValue("x");
            Assert.That(value, Is.EqualTo("3"));

            _mockFileSystemService.Verify(f => f.CreateFile(_fileName), Times.Never(), "file written while suspended");

            _repository.EndUpdate();
            _mockFileSystemService.Verify(f => f.CreateFile(_fileName), Times.Never(), "file written after first resume");

            _repository.EndUpdate();
            _mockFileSystemService.Verify(f => f.CreateFile(_fileName), "file not written after resume");
            var contents = GetFileContents(stream);
            Assert.That(contents, Is.EqualTo("x=3\r\n"));
        }

        [Test]
        public void TestSetValueNoChange()
        {
            SetupFile("x=3");
            var stream = SetupFileWrite();
            _repository.SetValue("x", "3");

            var value = _repository.GetValue("x");
            Assert.That(value, Is.EqualTo("3"));

            _mockFileSystemService.Verify(f => f.CreateFile(_fileName), Times.Never(), "file written even though it didn't change");
        }

        [Test]
        public void TestSuspendedSetValueNoChange()
        {
            SetupFile("x=3");
            var stream = SetupFileWrite();

            _repository.BeginUpdate();
            _repository.SetValue("x", "3");

            var value = _repository.GetValue("x");
            Assert.That(value, Is.EqualTo("3"));

            _mockFileSystemService.Verify(f => f.CreateFile(_fileName), Times.Never(), "file written while suspended");

            _repository.EndUpdate();
            _mockFileSystemService.Verify(f => f.CreateFile(_fileName), Times.Never(), "file written after resume");
        }
    }
}
