using Jamiras.Components;
using NUnit.Framework;

namespace Jamiras.Core.Tests.Components
{
    [TestFixture]
    public class SinglePropertyWatcherTests
    {
        [SetUp]
        public void Setup()
        {
            _source = new TestPropertyChangedObject();
            _watcher = new SinglePropertyWatcher(_source, CallbackHandler, "ClassicProperty", "callbackData");
            _callbackCalled = false;
        }

        private void CallbackHandler(string propertyName, object callbackData)
        {
            Assert.That(propertyName, Is.EqualTo("ClassicProperty"));
            Assert.That(callbackData, Is.EqualTo("callbackData"));
            _callbackCalled = true;
        }

        private TestPropertyChangedObject _source;
        private SinglePropertyWatcher _watcher;
        private bool _callbackCalled;

        [Test]
        public void TestToString()
        {
            Assert.That(_watcher.ToString(), Is.EqualTo("PropertyWatcher: ClassicProperty"));
        }

        [Test]
        public void TestInitialSource()
        {
            Assert.That(_watcher.Source, Is.SameAs(_source));
        }

        [Test]
        public void TestSourcePropertyChanged()
        {
            _source.ClassicProperty = 6;
            Assert.That(_callbackCalled, Is.True);

            _callbackCalled = false;
            _source.ClassicProperty = 6;
            Assert.That(_callbackCalled, Is.False);

            _source.ClassicProperty = 8;
            Assert.That(_callbackCalled, Is.True);
        }

        [Test]
        public void TestChangeSource()
        {
            var newSource = new TestPropertyChangedObject();
            Assert.That(_watcher.Source, Is.Not.SameAs(newSource));

            _watcher.Source = newSource;
            Assert.That(_watcher.Source, Is.SameAs(newSource));

            _source.ClassicProperty = 6;
            Assert.That(_callbackCalled, Is.False);

            newSource.ClassicProperty = 6;
            Assert.That(_callbackCalled, Is.True);
        }

        [Test]
        public void TestRemoveHandler()
        {
            var newWatcher = _watcher.RemoveHandler("ClassicProperty");
            Assert.That(newWatcher.IsWatching("ClassicProperty"), Is.False);
        }

        [Test]
        public void TestRemoveUnregisteredHandler()
        {
            var newWatcher = _watcher.RemoveHandler("NewProperty");
            Assert.That(newWatcher, Is.SameAs(_watcher));
            Assert.That(newWatcher.IsWatching("ClassicProperty"), Is.True);
        }

        [Test]
        public void TestIsWatching()
        {
            Assert.That(_watcher.IsWatching("ClassicProperty"), Is.True);
            Assert.That(_watcher.IsWatching("NewProperty"), Is.False);
        }

        [Test]
        public void TestAddHandler()
        {
            var newWatcher = _watcher.AddHandler("NewProperty", null);
            Assert.That(newWatcher.IsWatching("ClassicProperty"), Is.True);
            Assert.That(newWatcher.IsWatching("NewProperty"), Is.True);
        }

        [Test]
        public void TestAddDuplicateHandler()
        {
            Assert.That(() => _watcher.AddHandler("ClassicProperty", null), Throws.InvalidOperationException);
        }

        [Test]
        public void TestGetCallbackData()
        {
            Assert.That(_watcher.GetCallbackData("ClassicProperty"), Is.EqualTo("callbackData"));
            Assert.That(_watcher.GetCallbackData("NewProperty"), Is.Null);
        }

        [Test]
        public void TestWatchedProperties()
        {
            Assert.That(_watcher.WatchedProperties, Is.EquivalentTo(new string[] { "ClassicProperty" }));
        }
    }
}
