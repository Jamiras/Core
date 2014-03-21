using System.Linq;
using Jamiras.Components;
using NUnit.Framework;

namespace Jamiras.Core.Tests.Components
{
    [TestFixture]
    class RecencyBufferTests
    {
        [SetUp]
        public void Setup()
        {
            _buffer = new RecencyBuffer<string>(5);
        }

        private RecencyBuffer<string> _buffer;

        [Test]
        public void TestInitialization()
        {
            Assert.That(_buffer.Count, Is.EqualTo(0));
            Assert.That(_buffer.Capacity, Is.EqualTo(5));

            var array = _buffer.ToArray();
            Assert.That(array.Length, Is.EqualTo(0));
        }

        [Test]
        public void TestAddOneItem()
        {
            _buffer.Add("One");
            Assert.That(_buffer.Count, Is.EqualTo(1));

            var array = _buffer.ToArray();
            Assert.That(array.Length, Is.EqualTo(1));
            Assert.That(array[0], Is.EqualTo("One"));
        }

        [Test]
        public void TestAddMultipleItems()
        {
            _buffer.Add("One");
            _buffer.Add("Two");
            _buffer.Add("Three");
            Assert.That(_buffer.Count, Is.EqualTo(3));

            var array = _buffer.ToArray();
            Assert.That(array.Length, Is.EqualTo(3));
            Assert.That(array[0], Is.EqualTo("Three"));
            Assert.That(array[1], Is.EqualTo("Two"));
            Assert.That(array[2], Is.EqualTo("One"));
        }

        [Test]
        public void TestAddOverflowingItems()
        {
            _buffer.Add("One");
            _buffer.Add("Two");
            _buffer.Add("Three");
            _buffer.Add("Four");
            _buffer.Add("Five");
            _buffer.Add("Six");
            Assert.That(_buffer.Count, Is.EqualTo(5));

            var array = _buffer.ToArray();
            Assert.That(array.Length, Is.EqualTo(5));
            Assert.That(array[0], Is.EqualTo("Six"));
            Assert.That(array[1], Is.EqualTo("Five"));
            Assert.That(array[2], Is.EqualTo("Four"));
            Assert.That(array[3], Is.EqualTo("Three"));
            Assert.That(array[4], Is.EqualTo("Two"));
        }

        [Test]
        public void TestRemoveOnlyItem()
        {
            _buffer.Add("One");
            Assert.That(_buffer.Count, Is.EqualTo(1));

            _buffer.Remove("One");
            Assert.That(_buffer.Count, Is.EqualTo(0));

            _buffer.Add("Two");
            Assert.That(_buffer.Count, Is.EqualTo(1));

            var array = _buffer.ToArray();
            Assert.That(array.Length, Is.EqualTo(1));
            Assert.That(array[0], Is.EqualTo("Two"));
        }

        [Test]
        public void TestRemoveFirstOfTwoItems()
        {
            _buffer.Add("One");
            _buffer.Add("Two");
            Assert.That(_buffer.Count, Is.EqualTo(2));

            _buffer.Remove("One");
            Assert.That(_buffer.Count, Is.EqualTo(1));

            var array = _buffer.ToArray();
            Assert.That(array.Length, Is.EqualTo(1));
            Assert.That(array[0], Is.EqualTo("Two"));
        }

        [Test]
        public void TestRemoveLastOfTwoItems()
        {
            _buffer.Add("One");
            _buffer.Add("Two");
            Assert.That(_buffer.Count, Is.EqualTo(2));

            _buffer.Remove("Two");
            Assert.That(_buffer.Count, Is.EqualTo(1));

            var array = _buffer.ToArray();
            Assert.That(array.Length, Is.EqualTo(1));
            Assert.That(array[0], Is.EqualTo("One"));
        }

        [Test]
        public void TestRemoveFirstOfFullBuffer()
        {
            _buffer.Add("One");
            _buffer.Add("Two");
            _buffer.Add("Three");
            _buffer.Add("Four");
            _buffer.Add("Five");
            Assert.That(_buffer.Count, Is.EqualTo(5));

            _buffer.Remove("One");
            Assert.That(_buffer.Count, Is.EqualTo(4));

            var array = _buffer.ToArray();
            Assert.That(array, Is.EquivalentTo(new[] { "Five", "Four", "Three", "Two" }));

            _buffer.Add("Six");
            _buffer.Add("Seven");
            Assert.That(_buffer.Count, Is.EqualTo(5));

            array = _buffer.ToArray();
            Assert.That(array, Is.EquivalentTo(new[] { "Seven", "Six", "Five", "Four", "Three" }));
        }

        [Test]
        public void TestRemoveLastOfFullBuffer()
        {
            _buffer.Add("One");
            _buffer.Add("Two");
            _buffer.Add("Three");
            _buffer.Add("Four");
            _buffer.Add("Five");
            Assert.That(_buffer.Count, Is.EqualTo(5));

            _buffer.Remove("Five");
            Assert.That(_buffer.Count, Is.EqualTo(4));

            var array = _buffer.ToArray();
            Assert.That(array, Is.EquivalentTo(new[] { "Four", "Three", "Two", "One" }));

            _buffer.Add("Six");
            _buffer.Add("Seven");
            Assert.That(_buffer.Count, Is.EqualTo(5));

            array = _buffer.ToArray();
            Assert.That(array, Is.EquivalentTo(new[] { "Seven", "Six", "Four", "Three", "Two" }));
        }

        [Test]
        public void TestRemoveMiddleOfFullBuffer()
        {
            _buffer.Add("One");
            _buffer.Add("Two");
            _buffer.Add("Three");
            _buffer.Add("Four");
            _buffer.Add("Five");
            Assert.That(_buffer.Count, Is.EqualTo(5));

            _buffer.Remove("Three");
            Assert.That(_buffer.Count, Is.EqualTo(4));

            var array = _buffer.ToArray();
            Assert.That(array, Is.EquivalentTo(new[] { "Five", "Four", "Two", "One" }));

            _buffer.Add("Six");
            _buffer.Add("Seven");
            Assert.That(_buffer.Count, Is.EqualTo(5));

            array = _buffer.ToArray();
            Assert.That(array.Length, Is.EqualTo(5));
            Assert.That(array, Is.EquivalentTo(new[] { "Seven", "Six", "Five", "Four", "Two" }));
        }

        [Test]
        public void TestFindAndMakeRecentTwoItems()
        {
            _buffer.Add("One");
            _buffer.Add("Two");
            Assert.That(_buffer.Count, Is.EqualTo(2));
            Assert.That(_buffer.ToArray(), Is.EquivalentTo(new[] { "Two", "One" }));

            Assert.That(_buffer.FindAndMakeRecent(i => i == "Two"), Is.EqualTo("Two"));
            Assert.That(_buffer.ToArray(), Is.EquivalentTo(new[] { "Two", "One" }));

            Assert.That(_buffer.FindAndMakeRecent(i => i == "One"), Is.EqualTo("One"));
            Assert.That(_buffer.ToArray(), Is.EquivalentTo(new[] { "One", "Two" }));
        }

        [Test]
        public void TestFindAndMakeRecentTwoItemsNoMatch()
        {
            _buffer.Add("One");
            _buffer.Add("Two");
            Assert.That(_buffer.Count, Is.EqualTo(2));
            Assert.That(_buffer.ToArray(), Is.EquivalentTo(new[] { "Two", "One" }));

            Assert.That(_buffer.FindAndMakeRecent(i => i == "Three"), Is.Null);
            Assert.That(_buffer.ToArray(), Is.EquivalentTo(new[] { "Two", "One" }));
        }

        [Test]
        public void TestFindAndMakeRecentFirstOfFullBuffer()
        {
            _buffer.Add("One");
            _buffer.Add("Two");
            _buffer.Add("Three");
            _buffer.Add("Four");
            _buffer.Add("Five");
            Assert.That(_buffer.Count, Is.EqualTo(5));

            Assert.That(_buffer.FindAndMakeRecent(i => i == "One"), Is.EqualTo("One"));
            Assert.That(_buffer.ToArray(), Is.EquivalentTo(new[] { "One", "Five", "Four", "Three", "Two" }));
        }

        [Test]
        public void TestFindAndMakeRecentLastOfFullBuffer()
        {
            _buffer.Add("One");
            _buffer.Add("Two");
            _buffer.Add("Three");
            _buffer.Add("Four");
            _buffer.Add("Five");
            Assert.That(_buffer.Count, Is.EqualTo(5));

            Assert.That(_buffer.FindAndMakeRecent(i => i == "Five"), Is.EqualTo("Five"));
            Assert.That(_buffer.ToArray(), Is.EquivalentTo(new[] { "Five", "Four", "Three", "Two", "One" }));
        }

        [Test]
        public void TestFindAndMakeRecentMiddleOfFullBuffer()
        {
            _buffer.Add("One");
            _buffer.Add("Two");
            _buffer.Add("Three");
            _buffer.Add("Four");
            _buffer.Add("Five");
            Assert.That(_buffer.Count, Is.EqualTo(5));

            Assert.That(_buffer.FindAndMakeRecent(i => i == "Three"), Is.EqualTo("Three"));
            Assert.That(_buffer.ToArray(), Is.EquivalentTo(new[] { "Three", "Five", "Four", "Two", "One" }));
        }
    }
}
