using System.IO;
using System.Text;
using Jamiras.IO;
using NUnit.Framework;

namespace Jamiras.Core.Tests.IO
{
    [TestFixture]
    public class CsvReaderTests
    {
        private CsvReader CreateReader(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            var memoryStream = new MemoryStream(bytes);
            return new CsvReader(memoryStream);
        }

        [Test]
        public void TestEmpty()
        {
            var reader = CreateReader("");
            Assert.That(reader.ReadLine(), Is.Null);
        }

        [Test]
        public void TestSingleWord()
        {
            var reader = CreateReader("foo");
            var tokens = reader.ReadLine();
            Assert.That(tokens, Is.Not.Null);
            Assert.That(tokens.Length, Is.EqualTo(1));
            Assert.That(tokens[0].ToString(), Is.EqualTo("foo"));

            Assert.That(reader.ReadLine(), Is.Null);
        }

        [Test]
        public void TestSingleNumber()
        {
            var reader = CreateReader("1234");
            var tokens = reader.ReadLine();
            Assert.That(tokens, Is.Not.Null);
            Assert.That(tokens.Length, Is.EqualTo(1));
            Assert.That(tokens[0].ToString(), Is.EqualTo("1234"));

            Assert.That(reader.ReadLine(), Is.Null);
        }

        [Test]
        public void TestSingleQuotedString()
        {
            var reader = CreateReader("\"Look at me now!\"");
            var tokens = reader.ReadLine();
            Assert.That(tokens, Is.Not.Null);
            Assert.That(tokens.Length, Is.EqualTo(1));
            Assert.That(tokens[0].ToString(), Is.EqualTo("Look at me now!"));

            Assert.That(reader.ReadLine(), Is.Null);
        }

        [Test]
        public void TestCommaInQuotedString()
        {
            var reader = CreateReader("\"Look, now!\"");
            var tokens = reader.ReadLine();
            Assert.That(tokens, Is.Not.Null);
            Assert.That(tokens.Length, Is.EqualTo(1));
            Assert.That(tokens[0].ToString(), Is.EqualTo("Look, now!"));

            Assert.That(reader.ReadLine(), Is.Null);
        }

        [Test]
        public void TestUnquotedString()
        {
            var reader = CreateReader("1,beneath the trees,8");
            var tokens = reader.ReadLine();
            Assert.That(tokens, Is.Not.Null);
            Assert.That(tokens.Length, Is.EqualTo(3));
            Assert.That(tokens[0].ToString(), Is.EqualTo("1"));
            Assert.That(tokens[1].ToString(), Is.EqualTo("beneath the trees"));
            Assert.That(tokens[2].ToString(), Is.EqualTo("8"));

            Assert.That(reader.ReadLine(), Is.Null);
        }

        [Test]
        public void TestSingleLine()
        {
            var reader = CreateReader("foo,1,\"hello, world\",8,$9.15,11:30,bar");
            var tokens = reader.ReadLine();
            Assert.That(tokens, Is.Not.Null);
            Assert.That(tokens.Length, Is.EqualTo(7));
            Assert.That(tokens[0].ToString(), Is.EqualTo("foo"));
            Assert.That(tokens[1].ToString(), Is.EqualTo("1"));
            Assert.That(tokens[2].ToString(), Is.EqualTo("hello, world"));
            Assert.That(tokens[3].ToString(), Is.EqualTo("8"));
            Assert.That(tokens[4].ToString(), Is.EqualTo("$9.15"));
            Assert.That(tokens[5].ToString(), Is.EqualTo("11:30"));
            Assert.That(tokens[6].ToString(), Is.EqualTo("bar"));

            Assert.That(reader.ReadLine(), Is.Null);
        }

        [Test]
        public void TestMultipleLines()
        {
            var reader = CreateReader("1,\"one\"\n2,two\n3,'three'");
            var tokens = reader.ReadLine();
            Assert.That(tokens, Is.Not.Null);
            Assert.That(tokens.Length, Is.EqualTo(2));
            Assert.That(tokens[0].ToString(), Is.EqualTo("1"));
            Assert.That(tokens[1].ToString(), Is.EqualTo("one"));

            tokens = reader.ReadLine();
            Assert.That(tokens, Is.Not.Null);
            Assert.That(tokens.Length, Is.EqualTo(2));
            Assert.That(tokens[0].ToString(), Is.EqualTo("2"));
            Assert.That(tokens[1].ToString(), Is.EqualTo("two"));

            tokens = reader.ReadLine();
            Assert.That(tokens, Is.Not.Null);
            Assert.That(tokens.Length, Is.EqualTo(2));
            Assert.That(tokens[0].ToString(), Is.EqualTo("3"));
            Assert.That(tokens[1].ToString(), Is.EqualTo("'three'"));

            Assert.That(reader.ReadLine(), Is.Null);
        }

        [Test]
        public void TestTrailingNewLine()
        {
            var reader = CreateReader("1,\"one\"\n2,two\n");
            var tokens = reader.ReadLine();
            Assert.That(tokens, Is.Not.Null);
            Assert.That(tokens.Length, Is.EqualTo(2));
            Assert.That(tokens[0].ToString(), Is.EqualTo("1"));
            Assert.That(tokens[1].ToString(), Is.EqualTo("one"));

            tokens = reader.ReadLine();
            Assert.That(tokens, Is.Not.Null);
            Assert.That(tokens.Length, Is.EqualTo(2));
            Assert.That(tokens[0].ToString(), Is.EqualTo("2"));
            Assert.That(tokens[1].ToString(), Is.EqualTo("two"));

            Assert.That(reader.ReadLine(), Is.Null);
        }
    }
}
