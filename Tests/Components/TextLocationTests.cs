using Jamiras.Components;
using NUnit.Framework;

namespace Jamiras.Core.Tests.Components
{
    [TestFixture]
    public class TextLocationTests
    {
        [Test]
        public void TestInitialization()
        {
            var location = new TextLocation(1, 6);
            Assert.That(location.Line, Is.EqualTo(1));
            Assert.That(location.Column, Is.EqualTo(6));
            Assert.That(location.ToString(), Is.EqualTo("1,6"));
        }

        [Test]
        public void TestGetHashCode()
        {
            var location1 = new TextLocation(1, 6);
            var location2 = new TextLocation(6, 1);
            var location3 = new TextLocation(1, 7);
            var location4 = new TextLocation(2, 6);
            var location5 = new TextLocation(1, 6);

            Assert.That(location1.GetHashCode(), Is.Not.EqualTo(location2.GetHashCode()));
            Assert.That(location1.GetHashCode(), Is.Not.EqualTo(location3.GetHashCode()));
            Assert.That(location1.GetHashCode(), Is.Not.EqualTo(location4.GetHashCode()));
            Assert.That(location2.GetHashCode(), Is.Not.EqualTo(location3.GetHashCode()));
            Assert.That(location2.GetHashCode(), Is.Not.EqualTo(location4.GetHashCode()));
            Assert.That(location3.GetHashCode(), Is.Not.EqualTo(location4.GetHashCode()));

            Assert.That(location1.GetHashCode(), Is.EqualTo(location5.GetHashCode()));
        }

        [Test]
        [TestCase(1, 4, -1)] // previous line, previous column
        [TestCase(1, 5, -1)] // previous line, same column
        [TestCase(1, 6, -1)] // previous line, later column
        [TestCase(2, 1, -1)] // previous column
        [TestCase(2, 4, -1)] // previous column
        [TestCase(2, 5, 0)] // exact match
        [TestCase(2, 6, 1)] // later column
        [TestCase(2, 99, 1)] // later column
        [TestCase(3, 4, 1)] // later line, previous column
        [TestCase(3, 5, 1)] // later line, same column
        [TestCase(3, 6, 1)] // later line, later column
        public void TestCompare(int line, int column, int result)
        {
            var location = new TextLocation(2, 5);
            var testLocation = new TextLocation(line, column);

            if (result < 0)
            {
                Assert.That(testLocation < location, Is.True);
                Assert.That(testLocation <= location, Is.True);
                Assert.That(testLocation == location, Is.False);
                Assert.That(testLocation > location, Is.False);
                Assert.That(testLocation >= location, Is.False);
                Assert.That(testLocation != location, Is.True);
                Assert.That(testLocation.Equals(location), Is.False);
            }
            else if (result > 0)
            {
                Assert.That(testLocation < location, Is.False);
                Assert.That(testLocation <= location, Is.False);
                Assert.That(testLocation == location, Is.False);
                Assert.That(testLocation > location, Is.True);
                Assert.That(testLocation >= location, Is.True);
                Assert.That(testLocation != location, Is.True);
                Assert.That(testLocation.Equals(location), Is.False);
            }
            else
            {
                Assert.That(testLocation < location, Is.False);
                Assert.That(testLocation <= location, Is.True);
                Assert.That(testLocation == location, Is.True);
                Assert.That(testLocation > location, Is.False);
                Assert.That(testLocation >= location, Is.True);
                Assert.That(testLocation != location, Is.False);
                Assert.That(testLocation.Equals(location), Is.True);
            }
        }
    }
}
