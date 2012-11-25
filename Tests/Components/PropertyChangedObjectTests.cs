using System.Collections.Generic;
using System.ComponentModel;
using Jamiras.Components;
using NUnit.Framework;

namespace Jamiras.Core.Tests.Components
{
    [TestFixture]
    class PropertyChangedObjectTests
    {
        private class TestObject : PropertyChangedObject
        {
            public int ClassicProperty
            {
                get { return _intValue; }
                set
                {
                    if (_intValue != value)
                    {
                        _intValue = value;
                        OnPropertyChanged(new PropertyChangedEventArgs("ClassicProperty"));
                    }
                }
            }
            private int _intValue;

            public string NewProperty
            {
                get { return _stringValue; }
                set
                {
                    if (_stringValue != value)
                    {
                        _stringValue = value;
                        OnPropertyChanged(() => NewProperty);
                    }
                }
            }
            private string _stringValue;
        }

        [Test]
        public void TestInheritance()
        {
            var obj = new TestObject();
            Assert.That(obj, Is.InstanceOf<INotifyPropertyChanged>());
        }

        [Test]
        public void TestClassicProperty()
        {
            var obj = new TestObject();

            List<string> propertiesChanged = new List<string>();
            obj.PropertyChanged += (o, e) => propertiesChanged.Add(e.PropertyName);

            obj.ClassicProperty = 99;
            Assert.That(propertiesChanged, Contains.Item("ClassicProperty"));
        }

        [Test]
        public void TestNewProperty()
        {
            var obj = new TestObject();

            List<string> propertiesChanged = new List<string>();
            obj.PropertyChanged += (o, e) => propertiesChanged.Add(e.PropertyName);

            obj.NewProperty = "test";
            Assert.That(propertiesChanged, Contains.Item("NewProperty"));
        }
    }
}
