using Jamiras.Components;
using NUnit.Framework;

namespace Jamiras.Core.Tests.Components
{
    [TestFixture]
    public class NoPropertyWatcherTests
    {
        [SetUp]
        public void Setup()
        {
            _source = new TestPropertyChangedObject();
            _watcher = new NoPropertyWatcher(_source, CallbackHandler);
        }

        private TestPropertyChangedObject _source;
        private NoPropertyWatcher _watcher;

        private void CallbackHandler(string propertyName, object callbackData)
        {
        }

        [Test]
        public void TestToString()
        {
            Assert.That(_watcher.ToString(), Is.EqualTo("PropertyWatcher: none"));
        }

        [Test]
        public void TestInitialSource()
        {
            Assert.That(_watcher.Source, Is.SameAs(_source));
        }

        [Test]
        public void TestChangeSource()
        {
            var newSource = new TestPropertyChangedObject();
            Assert.That(_watcher.Source, Is.Not.SameAs(newSource));

            _watcher.Source = newSource;
            Assert.That(_watcher.Source, Is.SameAs(newSource));
        }

        [Test]
        public void TestRemoveHandler()
        {
            var newWatcher = _watcher.RemoveHandler("ClassicProperty");
            Assert.That(newWatcher, Is.SameAs(_watcher));
        }

        [Test]
        public void TestIsWatching()
        {
            Assert.That(_watcher.IsWatching("ClassicProperty"), Is.False);
        }

        [Test]
        public void TestAddHandler()
        {
            var newWatcher = _watcher.AddHandler("ClassicProperty", null);
            Assert.That(newWatcher.IsWatching("ClassicProperty"), Is.True);
        }

        [Test]
        public void TestGetCallbackData()
        {
            Assert.That(_watcher.GetCallbackData("ClassicProperty"), Is.Null);
        }

        [Test]
        public void TestWatchedProperties()
        {
            Assert.That(_watcher.WatchedProperties, Is.EquivalentTo(new string[0]));
        }
    }
}
