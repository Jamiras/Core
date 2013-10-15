using Jamiras.Components;
using NUnit.Framework;

namespace Jamiras.Core.Tests.Components
{
    [TestFixture]
    public class DoublePropertyWatcherTests
    {
        [SetUp]
        public void Setup()
        {
            _source = new TestPropertyChangedObject();
            _watcher = new DoublePropertyWatcher(_source, CallbackHandler, "ClassicProperty", "ClassicPropertyCallbackData", "NewProperty", "NewPropertyCallbackData");
            _propertyRaised = null;
        }

        private void CallbackHandler(string propertyName, object callbackData)
        {
            _propertyRaised = propertyName;
            _callbackData = (string)callbackData;
        }

        private TestPropertyChangedObject _source;
        private DoublePropertyWatcher _watcher;
        private string _propertyRaised, _callbackData;

        [Test]
        public void TestToString()
        {
            Assert.That(_watcher.ToString(), Is.EqualTo("PropertyWatcher: ClassicProperty, NewProperty"));
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
            Assert.That(_propertyRaised, Is.EqualTo("ClassicProperty"));
            Assert.That(_callbackData, Is.EqualTo("ClassicPropertyCallbackData"));

            _propertyRaised = null;
            _source.ClassicProperty = 6;
            Assert.That(_propertyRaised, Is.Null);

            _source.ClassicProperty = 8;
            Assert.That(_propertyRaised, Is.EqualTo("ClassicProperty"));
        }

        [Test]
        public void TestChangeSource()
        {
            var newSource = new TestPropertyChangedObject();
            Assert.That(_watcher.Source, Is.Not.SameAs(newSource));

            _watcher.Source = newSource;
            Assert.That(_watcher.Source, Is.SameAs(newSource));

            _source.ClassicProperty = 6;
            Assert.That(_propertyRaised, Is.Null);

            newSource.ClassicProperty = 6;
            Assert.That(_propertyRaised, Is.EqualTo("ClassicProperty"));
        }

        [Test]
        public void TestSourceSecondaryPropertyChanged()
        {
            _source.NewProperty = "Hello";
            Assert.That(_propertyRaised, Is.EqualTo("NewProperty"));
            Assert.That(_callbackData, Is.EqualTo("NewPropertyCallbackData"));

            _propertyRaised = null;
            _source.NewProperty = "Hello";
            Assert.That(_propertyRaised, Is.Null);

            _source.NewProperty = "Goodbye";
            Assert.That(_propertyRaised, Is.EqualTo("NewProperty"));
        }

        [Test]
        public void TestChangeSourceSecondaryProperty()
        {
            var newSource = new TestPropertyChangedObject();
            Assert.That(_watcher.Source, Is.Not.SameAs(newSource));

            _watcher.Source = newSource;
            Assert.That(_watcher.Source, Is.SameAs(newSource));

            _source.NewProperty = "Hello";
            Assert.That(_propertyRaised, Is.Null);

            newSource.NewProperty = "Hello";
            Assert.That(_propertyRaised, Is.EqualTo("NewProperty"));
        }

        [Test]
        public void TestRemoveHandler()
        {
            var newWatcher = _watcher.RemoveHandler("ClassicProperty");
            Assert.That(newWatcher.IsWatching("ClassicProperty"), Is.False);
            Assert.That(newWatcher.IsWatching("NewProperty"), Is.True);
        }

        [Test]
        public void TestRemoveSecondaryHandler()
        {
            var newWatcher = _watcher.RemoveHandler("NewProperty");
            Assert.That(newWatcher.IsWatching("NewProperty"), Is.False);
            Assert.That(newWatcher.IsWatching("ClassicProperty"), Is.True);
        }

        [Test]
        public void TestRemoveUnregisteredHandler()
        {
            var newWatcher = _watcher.RemoveHandler("Banana");
            Assert.That(newWatcher, Is.SameAs(_watcher));
            Assert.That(newWatcher.IsWatching("ClassicProperty"), Is.True);
            Assert.That(newWatcher.IsWatching("NewProperty"), Is.True);
        }

        [Test]
        public void TestIsWatching()
        {
            Assert.That(_watcher.IsWatching("ClassicProperty"), Is.True);
            Assert.That(_watcher.IsWatching("NewProperty"), Is.True);
            Assert.That(_watcher.IsWatching("Banana"), Is.False);
        }

        [Test]
        public void TestAddHandler()
        {
            var newWatcher = _watcher.AddHandler("Banana", null);
            Assert.That(newWatcher.IsWatching("ClassicProperty"), Is.True);
            Assert.That(newWatcher.IsWatching("NewProperty"), Is.True);
            Assert.That(newWatcher.IsWatching("Banana"), Is.True);
        }

        [Test]
        public void TestAddDuplicateHandler()
        {
            Assert.That(() => _watcher.AddHandler("ClassicProperty", null), Throws.InvalidOperationException);
        }

        [Test]
        public void TestGetCallbackData()
        {
            Assert.That(_watcher.GetCallbackData("ClassicProperty"), Is.EqualTo("ClassicPropertyCallbackData"));
            Assert.That(_watcher.GetCallbackData("NewProperty"), Is.EqualTo("NewPropertyCallbackData"));
            Assert.That(_watcher.GetCallbackData("Banana"), Is.Null);
        }

        [Test]
        public void TestWatchedProperties()
        {
            Assert.That(_watcher.WatchedProperties, Is.EquivalentTo(new string[] { "ClassicProperty", "NewProperty" }));
        }
    }
}
