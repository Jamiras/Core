using System.ComponentModel;
using Jamiras.Components;
using NUnit.Framework;

namespace Jamiras.Core.Tests.Components
{
    [TestFixture]
    class PropertyObserverTests
    {
        [SetUp]
        public void Setup()
        {
            _source = new TestPropertyChangedObject();
            _observer = new PropertyObserver<TestPropertyChangedObject>(_source);
            _observer.RegisterHandler("ClassicProperty", SourcePropertyChanged);
        }

        private void SourcePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Assert.That(sender, Is.SameAs(_observer.Source));
            _propertyChanged = e.PropertyName;
        }

        private TestPropertyChangedObject _source;
        private PropertyObserver<TestPropertyChangedObject> _observer;
        private string _propertyChanged;

        [Test]
        public void TestObserve()
        {
            _source.ClassicProperty = 6;
            Assert.That(_propertyChanged, Is.EqualTo("ClassicProperty"));
        }

        [Test]
        public void TestDetachedSource()
        {
            _observer.Source = null;
            _source.ClassicProperty = 8;
            Assert.That(_propertyChanged, Is.Null);

            _observer.Source = _source;
            Assert.That(_propertyChanged, Is.Null);

            _source.ClassicProperty = 6;
            Assert.That(_propertyChanged, Is.EqualTo("ClassicProperty"));
        }

        private class WeakReferenceHelper
        {
            public WeakReferenceHelper(PropertyObserverTests owner)
            {
                _owner = owner;
            }

            private readonly PropertyObserverTests _owner;

            public void SourcePropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                _owner.SourcePropertyChanged(sender, e);
            }
        }

        [Test]
        public void TestWeakReference()
        {
            // in .NET Core, WeakReferences have to be created in a different scope than
            // where they're used in order for the Garbage Collector to act on them.
            var createWeakReferenceTester = (PropertyObserverTests owner) =>
            {
                var tester = new WeakReferenceTester<WeakReferenceHelper>(() => new WeakReferenceHelper(owner));
                _observer.RegisterHandler("NewProperty", tester.Target.SourcePropertyChanged);
                return tester;
            };

            var tester = createWeakReferenceTester(this);

            _source.NewProperty = "Hello";
            Assert.That(_propertyChanged, Is.EqualTo("NewProperty"));

            tester.Expire();
            _propertyChanged = null;

            _source.NewProperty = "Goodbye";
            Assert.That(_propertyChanged, Is.Null);
        }
    }
}
