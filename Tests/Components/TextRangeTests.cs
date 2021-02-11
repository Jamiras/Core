using Jamiras.Components;
using NUnit.Framework;

namespace Jamiras.Core.Tests.Components
{
    [TestFixture]
    public class TextRangeTests
    {
        [Test]
        public void TestInitializationForward()
        {
            var location1 = new TextLocation(1, 6);
            var location2 = new TextLocation(3, 2);
            var selection = new TextRange(location1, location2);
            Assert.That(selection.ToString(), Is.EqualTo("1,6-3,2"));

            Assert.That(selection.Start, Is.EqualTo(location1));
            Assert.That(selection.End, Is.EqualTo(location2));
            Assert.That(selection.Front, Is.EqualTo(location1));
            Assert.That(selection.Back, Is.EqualTo(location2));
        }

        [Test]
        public void TestInitializationReverse()
        {
            var location1 = new TextLocation(1, 6);
            var location2 = new TextLocation(3, 2);
            var selection = new TextRange(location2, location1);
            Assert.That(selection.ToString(), Is.EqualTo("3,2-1,6"));

            Assert.That(selection.Start, Is.EqualTo(location2));
            Assert.That(selection.End, Is.EqualTo(location1));
            Assert.That(selection.Front, Is.EqualTo(location1));
            Assert.That(selection.Back, Is.EqualTo(location2));
        }

        [Test]
        [TestCase(1, 4, false)] // before first line
        [TestCase(2, 3, false)] // before first match
        [TestCase(2, 4, true)] // first match
        [TestCase(2, 5, true)] // inside, first line
        [TestCase(3, 4, true)] // inside, next line
        [TestCase(4, 5, true)] // inside, last line
        [TestCase(4, 6, true)] // last match
        [TestCase(4, 7, false)] // after last match
        [TestCase(5, 6, false)] // after last line
        public void TestContains(int line, int column, bool expected)
        {
            var location1 = new TextLocation(2, 4);
            var location2 = new TextLocation(4, 6);
            var selection1 = new TextRange(location1, location2);
            var selection2 = new TextRange(location2, location1);
            var testLocation = new TextLocation(line, column);

            if (expected)
            {
                Assert.That(selection1.Contains(testLocation), Is.True);
                Assert.That(selection2.Contains(testLocation), Is.True);
            }
            else
            {
                Assert.That(selection1.Contains(testLocation), Is.False);
                Assert.That(selection2.Contains(testLocation), Is.False);
            }
        }

        [Test]
        public void TestIsEmpty()
        {
            var location1 = new TextLocation(5, 5);
            var location2 = new TextLocation(7, 7);

            Assert.That(new TextRange().IsEmpty, Is.True);
            Assert.That(new TextRange(location1, location1).IsEmpty, Is.True);
            Assert.That(new TextRange(location1, location2).IsEmpty, Is.False);
            Assert.That(new TextRange(location2, location1).IsEmpty, Is.False);
        }

        [Test]
        public void TestEnsureForward()
        {
            var location1 = new TextLocation(5, 5);
            var location2 = new TextLocation(7, 7);

            var range = new TextRange(location1, location2);
            Assert.That(range.Start, Is.EqualTo(location1));
            Assert.That(range.End, Is.EqualTo(location2));
            Assert.That(range.Front, Is.EqualTo(location1));
            Assert.That(range.Back, Is.EqualTo(location2));

            range.EnsureForward();
            Assert.That(range.Start, Is.EqualTo(location1));
            Assert.That(range.End, Is.EqualTo(location2));
            Assert.That(range.Front, Is.EqualTo(location1));
            Assert.That(range.Back, Is.EqualTo(location2));

            range = new TextRange(location2, location1);
            Assert.That(range.Start, Is.EqualTo(location2));
            Assert.That(range.End, Is.EqualTo(location1));
            Assert.That(range.Front, Is.EqualTo(location1));
            Assert.That(range.Back, Is.EqualTo(location2));

            range.EnsureForward();
            Assert.That(range.Start, Is.EqualTo(location1));
            Assert.That(range.End, Is.EqualTo(location2));
            Assert.That(range.Front, Is.EqualTo(location1));
            Assert.That(range.Back, Is.EqualTo(location2));
        }
    }
}
