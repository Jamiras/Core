using System.Linq;
using Jamiras.Components;
using NUnit.Framework;

namespace Jamiras.Core.Tests.Components
{
    [TestFixture]
    class FixedSizeStackTests
    {
        [SetUp]
        public void Setup()
        {
            _stack = new FixedSizeStack<string>(5);
        }

        private FixedSizeStack<string> _stack;

        [Test]
        public void TestInitialization()
        {
            Assert.That(_stack.Count, Is.EqualTo(0));
            Assert.That(_stack.Capacity, Is.EqualTo(5));
            Assert.That(_stack.Peek(), Is.Null);
        }

        [Test]
        public void TestPushOneItem()
        {
            _stack.Push("One");
            Assert.That(_stack.Count, Is.EqualTo(1));

            var array = _stack.ToArray();
            Assert.That(array.Length, Is.EqualTo(1));
            Assert.That(array[0], Is.EqualTo("One"));
        }

        [Test]
        public void TestPushMultipleItems()
        {
            _stack.Push("One");
            _stack.Push("Two");
            _stack.Push("Three");
            Assert.That(_stack.Count, Is.EqualTo(3));

            var array = _stack.ToArray();
            Assert.That(array.Length, Is.EqualTo(3));
            Assert.That(array[0], Is.EqualTo("Three"));
            Assert.That(array[1], Is.EqualTo("Two"));
            Assert.That(array[2], Is.EqualTo("One"));
        }

        [Test]
        public void TestPushOverflowingItems()
        {
            _stack.Push("One");
            _stack.Push("Two");
            _stack.Push("Three");
            _stack.Push("Four");
            _stack.Push("Five");
            _stack.Push("Six");
            Assert.That(_stack.Count, Is.EqualTo(5));

            var array = _stack.ToArray();
            Assert.That(array.Length, Is.EqualTo(5));
            Assert.That(array[0], Is.EqualTo("Six"));
            Assert.That(array[1], Is.EqualTo("Five"));
            Assert.That(array[2], Is.EqualTo("Four"));
            Assert.That(array[3], Is.EqualTo("Three"));
            Assert.That(array[4], Is.EqualTo("Two"));
        }

        [Test]
        public void TestPopOnlyItem()
        {
            _stack.Push("One");
            Assert.That(_stack.Count, Is.EqualTo(1));

            Assert.That(_stack.Peek(), Is.EqualTo("One"));
            Assert.That(_stack.Pop(), Is.EqualTo("One"));
            Assert.That(_stack.Count, Is.EqualTo(0));
            Assert.That(_stack.Peek(), Is.Null);

            _stack.Push("Two");
            Assert.That(_stack.Count, Is.EqualTo(1));

            var array = _stack.ToArray();
            Assert.That(array.Length, Is.EqualTo(1));
            Assert.That(array[0], Is.EqualTo("Two"));
        }

        [Test]
        public void TestPopOneOfTwoItems()
        {
            _stack.Push("One");
            _stack.Push("Two");
            Assert.That(_stack.Count, Is.EqualTo(2));

            Assert.That(_stack.Pop(), Is.EqualTo("Two"));
            Assert.That(_stack.Count, Is.EqualTo(1));

            var array = _stack.ToArray();
            Assert.That(array.Length, Is.EqualTo(1));
            Assert.That(array[0], Is.EqualTo("One"));
        }

        [Test]
        public void TestPopFirstOfFullBuffer()
        {
            _stack.Push("One");
            _stack.Push("Two");
            _stack.Push("Three");
            _stack.Push("Four");
            _stack.Push("Five");
            Assert.That(_stack.Count, Is.EqualTo(5));

            Assert.That(_stack.Peek(), Is.EqualTo("Five"));
            Assert.That(_stack.Pop(), Is.EqualTo("Five"));
            Assert.That(_stack.Count, Is.EqualTo(4));
            Assert.That(_stack.Peek(), Is.EqualTo("Four"));

            var array = _stack.ToArray();
            Assert.That(array, Is.EquivalentTo(new[] { "Four", "Three", "Two", "One" }));

            _stack.Push("Six");
            _stack.Push("Seven");
            Assert.That(_stack.Count, Is.EqualTo(5));

            array = _stack.ToArray();
            Assert.That(array, Is.EquivalentTo(new[] { "Seven", "Six", "Four", "Three", "Two" }));
        }
    }
}
