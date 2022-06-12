using System.ComponentModel;
using Jamiras.Components;
using NUnit.Framework;

namespace Jamiras.Core.Tests.Components
{
    [TestFixture]
    public class PropertyChangedPropagatorTests
    {
        [SetUp]
        public void Setup()
        {
            _source = new TestPropertyChangedObject();
            _propagator = new PropertyChangedPropagator<TestPropertyChangedObject>(_source, CallbackHandler);
            _propertyChanged = null;
        }

        private TestPropertyChangedObject _source;
        private PropertyChangedPropagator<TestPropertyChangedObject> _propagator;
        private string _propertyChanged;

        private void CallbackHandler(PropertyChangedEventArgs e)
        {
            _propertyChanged = e.PropertyName;
        }

        [Test]
        public void TestPropagation()
        {
            _propagator.RegisterPropertyPassThrough("ClassicProperty", "Banana");
            _source.ClassicProperty = 6;
            Assert.That(_propertyChanged, Is.EqualTo("Banana"));
        }

        [Test]
        public void TestUnpropagated()
        {
            _propagator.RegisterPropertyPassThrough("ClassicProperty", "Banana");
            _source.NewProperty = "Test";
            Assert.That(_propertyChanged, Is.Null);
        }

        [Test]
        public void TestSource()
        {
            _propagator.RegisterPropertyPassThrough("ClassicProperty", "Banana");
            Assert.That(_propagator.Source, Is.SameAs(_source));

            var newSource = new TestPropertyChangedObject();
            _propagator.Source = newSource;
            Assert.That(_propagator.Source, Is.SameAs(newSource));

            _source.ClassicProperty = 6;
            Assert.That(_propertyChanged, Is.Null);

            newSource.ClassicProperty = 6;
            Assert.That(_propertyChanged, Is.EqualTo("Banana"));
        }
    }
}
