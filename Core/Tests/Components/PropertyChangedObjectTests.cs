using System.Collections.Generic;
using System.ComponentModel;
using NUnit.Framework;

namespace Jamiras.Core.Tests.Components
{
    [TestFixture]
    class PropertyChangedObjectTests
    {
        [Test]
        public void TestInheritance()
        {
            var obj = new TestPropertyChangedObject();
            Assert.That(obj, Is.InstanceOf<INotifyPropertyChanged>());
        }

        [Test]
        public void TestClassicProperty()
        {
            var obj = new TestPropertyChangedObject();

            List<string> propertiesChanged = new List<string>();
            obj.PropertyChanged += (o, e) => propertiesChanged.Add(e.PropertyName);

            obj.ClassicProperty = 99;
            Assert.That(propertiesChanged, Contains.Item("ClassicProperty"));
        }

        [Test]
        public void TestNewProperty()
        {
            var obj = new TestPropertyChangedObject();

            List<string> propertiesChanged = new List<string>();
            obj.PropertyChanged += (o, e) => propertiesChanged.Add(e.PropertyName);

            obj.NewProperty = "test";
            Assert.That(propertiesChanged, Contains.Item("NewProperty"));
        }
    }
}
