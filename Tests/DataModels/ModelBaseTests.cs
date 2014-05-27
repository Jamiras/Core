using System;
using System.Collections.Generic;
using Jamiras.DataModels;
using NUnit.Framework;

namespace Jamiras.Core.Tests.DataModels
{
    [TestFixture]
    public class ModelBaseTests
    {
        private class TestClass : ModelBase
        {
            public static readonly ModelProperty StrProperty = ModelProperty.Register(typeof(TestClass), "Str", typeof(string), null);

            public string Str
            {
                get { return (string)GetValue(StrProperty); }
                set { SetValue(StrProperty, value); }
            }

            public static readonly ModelProperty IntegerProperty = ModelProperty.Register(typeof(TestClass), "Integer", typeof(int), 85);

            public int Integer
            {
                get { return (int)GetValue(IntegerProperty); }
                set { SetValue(IntegerProperty, value); }
            }

            public static readonly ModelProperty PrivateProperty = ModelProperty.Register(typeof(TestClass), null, typeof(bool), false, OnPrivatePropertyChanged);

            public int Counter { get; private set; }

            private static void OnPrivatePropertyChanged(object sender, ModelPropertyChangedEventArgs e)
            {
                ((TestClass)sender).Counter++;
            }
        }

        [SetUp]
        public void Setup()
        {
            _model = new TestClass();
        }

        private TestClass _model;

        [Test]
        public void TestDefaults()
        {
            Assert.That(_model.Str, Is.Null);
            Assert.That(_model.Integer, Is.EqualTo(85));
        }

        [Test]
        public void TestModifiedValues()
        {
            _model.Str = "Value";
            Assert.That(_model.Str, Is.EqualTo("Value"));

            _model.Integer = 0;
            Assert.That(_model.Integer, Is.EqualTo(0));
        }

        [Test]
        public void TestPropertyChangedString()
        {
            var propertiesChanged = new List<string>();
            _model.PropertyChanged += (o, e) => propertiesChanged.Add(e.PropertyName);

            _model.Str = null;
            Assert.That(propertiesChanged, Is.Empty);

            _model.Str = "Banana";
            Assert.That(propertiesChanged, Has.Member("Str"));

            propertiesChanged.Clear();
            _model.Str = "Banana";
            Assert.That(propertiesChanged, Is.Empty);
        }

        [Test]
        public void TestPropertyChangedSetValue()
        {
            var propertiesChanged = new List<string>();
            _model.PropertyChanged += (o, e) => propertiesChanged.Add(e.PropertyName);

            _model.SetValue(TestClass.StrProperty, null);
            Assert.That(propertiesChanged, Is.Empty);

            _model.SetValue(TestClass.StrProperty, "Banana");
            Assert.That(propertiesChanged, Has.Member("Str"));

            propertiesChanged.Clear();
            _model.SetValue(TestClass.StrProperty, "Banana");
            Assert.That(propertiesChanged, Is.Empty);
        }

        [Test]
        public void TestPropertyChangedSetValueCore()
        {
            var propertiesChanged = new List<string>();
            _model.PropertyChanged += (o, e) => propertiesChanged.Add(e.PropertyName);

            _model.SetValueCore(TestClass.StrProperty, null);
            Assert.That(propertiesChanged, Is.Empty);

            _model.SetValueCore(TestClass.StrProperty, "Banana");
            Assert.That(propertiesChanged, Is.Empty);

            _model.SetValueCore(TestClass.StrProperty, "Banana");
            Assert.That(propertiesChanged, Is.Empty);
        }

        [Test]
        public void TestPropertyChangedInteger()
        {
            var propertiesChanged = new List<string>();
            _model.PropertyChanged += (o, e) => propertiesChanged.Add(e.PropertyName);

            _model.Integer = 85;
            Assert.That(propertiesChanged, Is.Empty);

            _model.Integer = 99;
            Assert.That(propertiesChanged, Has.Member("Integer"));

            propertiesChanged.Clear();
            _model.Integer = 99;
            Assert.That(propertiesChanged, Is.Empty);
        }

        [Test]
        public void TestPropertyChangedPrivate()
        {
            var propertiesChanged = new List<string>();
            _model.PropertyChanged += (o, e) => propertiesChanged.Add(e.PropertyName);

            _model.SetValue(TestClass.PrivateProperty, false);
            Assert.That(propertiesChanged, Is.Empty);

            _model.SetValue(TestClass.PrivateProperty, true);
            Assert.That(propertiesChanged, Is.Empty);

            _model.SetValue(TestClass.PrivateProperty, true);
            Assert.That(propertiesChanged, Is.Empty);
        }

        [Test]
        public void TestSetValueInvalid()
        {
            var propertiesChanged = new List<string>();
            _model.PropertyChanged += (o, e) => propertiesChanged.Add(e.PropertyName);

            Assert.That(() => _model.SetValue(TestClass.StrProperty, 0), Throws.InstanceOf<InvalidCastException>());
        }

        [Test]
        public void TestModelPropertyChangedHandler()
        {
            Assert.That(_model.Counter, Is.EqualTo(0));

            _model.SetValue(TestClass.PrivateProperty, false);
            Assert.That(_model.Counter, Is.EqualTo(0));

            _model.SetValue(TestClass.PrivateProperty, true);
            Assert.That(_model.Counter, Is.EqualTo(1));

            _model.SetValue(TestClass.PrivateProperty, true);
            Assert.That(_model.Counter, Is.EqualTo(1));

            _model.SetValue(TestClass.PrivateProperty, false);
            Assert.That(_model.Counter, Is.EqualTo(2));
        }

        [Test]
        public void TestAddPropertyChangedHandler()
        {
            bool handled = false;
            EventHandler<ModelPropertyChangedEventArgs> handler = (o, e) => { handled = true; };
            _model.AddPropertyChangedHandler(TestClass.IntegerProperty, handler);

            _model.Integer = 99;
            Assert.That(handled, Is.True, "handler not called");

            handled = false;
            _model.Integer = 12;
            Assert.That(handled, Is.True, "handler not called second time");

            handled = false;
            _model.Str = "Banana";
            Assert.That(handled, Is.False, "handler called for different property");

            Assert.That(handler, Is.Not.Null, "handler released");
        }

        [Test]
        public void TestAddPropertyChangedHandlerArgs()
        {
            int oldValue = 85;
            EventHandler<ModelPropertyChangedEventArgs> handler = (o, e) =>
            {
                Assert.That(o, Is.SameAs(_model));
                Assert.That(e.Property, Is.SameAs(TestClass.IntegerProperty));
                Assert.That(e.OldValue, Is.EqualTo(oldValue));
                Assert.That(e.NewValue, Is.Not.EqualTo(oldValue));
                oldValue = (int)e.NewValue;
            };

            _model.AddPropertyChangedHandler(TestClass.IntegerProperty, handler);

            _model.Integer = 99;
            Assert.That(oldValue, Is.EqualTo(99), "handler not called");

            _model.Integer = 12;
            Assert.That(oldValue, Is.EqualTo(12), "handler not called again");

            Assert.That(handler, Is.Not.Null, "handler released");
        }

        [Test]
        public void TestRemovePropertyChangedHandler()
        {
            bool handled = false;
            EventHandler<ModelPropertyChangedEventArgs> handler = (o, e) => { handled = true; };
            _model.AddPropertyChangedHandler(TestClass.IntegerProperty, handler);

            _model.Integer = 99;
            Assert.That(handled, Is.True, "handler not called");

            handled = false;
            _model.RemovePropertyChangedHandler(TestClass.IntegerProperty, handler);
            _model.Integer = 12;
            Assert.That(handled, Is.False, "handler called after unsubscribe");

            Assert.That(handler, Is.Not.Null, "handler released");
        }
    }
}
