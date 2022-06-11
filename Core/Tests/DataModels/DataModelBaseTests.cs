using System.Collections.Generic;
using Jamiras.DataModels;
using NUnit.Framework;

namespace Jamiras.Core.Tests.DataModels
{
    [TestFixture]
    public class DataModelBaseTests
    {
        private class TestClass : DataModelBase
        {
            public static readonly ModelProperty StrProperty = ModelProperty.Register(typeof(TestClass), "Str", typeof(string), "Default");

            public string Str
            {
                get { return (string)GetValue(StrProperty); }
                set { SetValue(StrProperty, value); }
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
            Assert.That(_model.IsModified, Is.False);
            Assert.That(_model.UpdatedPropertyKeys, Is.EquivalentTo(new int[0]));
        }

        [Test]
        public void TestModifyField()
        {
            Assert.That(_model.IsModified, Is.False);
            Assert.That(_model.UpdatedPropertyKeys, Is.EquivalentTo(new int[0]));

            var changedProperties = new List<string>();
            _model.PropertyChanged += (o, e) => changedProperties.Add(e.PropertyName);

            _model.Str = "Test";
            
            Assert.That(changedProperties, Has.Member("Str"));
            Assert.That(_model.IsModified, Is.True);
            Assert.That(_model.UpdatedPropertyKeys, Is.EquivalentTo(new int[] { TestClass.StrProperty.Key }));
        }

        [Test]
        public void TestUnmodifyField()
        {
            _model.Str = "Test";

            Assert.That(_model.IsModified, Is.True);
            Assert.That(_model.UpdatedPropertyKeys, Is.EquivalentTo(new int[] { TestClass.StrProperty.Key }));

            var changedProperties = new List<string>();
            _model.PropertyChanged += (o, e) => changedProperties.Add(e.PropertyName);

            _model.Str = "Default";

            Assert.That(changedProperties, Has.Member("Str"));
            Assert.That(_model.IsModified, Is.False);
            Assert.That(_model.UpdatedPropertyKeys, Is.EquivalentTo(new int[0]));
        }

        [Test]
        public void TestRemodifyField()
        {
            _model.Str = "Test";

            Assert.That(_model.IsModified, Is.True);
            Assert.That(_model.UpdatedPropertyKeys, Is.EquivalentTo(new int[] { TestClass.StrProperty.Key }));

            var changedProperties = new List<string>();
            _model.PropertyChanged += (o, e) => changedProperties.Add(e.PropertyName);

            _model.Str = "Test2";

            Assert.That(changedProperties, Has.Member("Str"));
            Assert.That(_model.IsModified, Is.True);
            Assert.That(_model.UpdatedPropertyKeys, Is.EquivalentTo(new int[] { TestClass.StrProperty.Key }));

            _model.Str = "Default";

            Assert.That(_model.IsModified, Is.False);
            Assert.That(_model.UpdatedPropertyKeys, Is.EquivalentTo(new int[0]));
        }

        [Test]
        public void TestGetOriginalValue()
        {
            _model.Str = "Test";
            Assert.That(_model.GetValue(TestClass.StrProperty), Is.EqualTo("Test"));
            Assert.That(_model.GetOriginalValue(TestClass.StrProperty), Is.EqualTo("Default"));
        }

        [Test]
        public void TestAcceptChanges()
        {
            _model.Str = "Test";

            Assert.That(_model.IsModified, Is.True);
            Assert.That(_model.UpdatedPropertyKeys, Is.EquivalentTo(new int[] { TestClass.StrProperty.Key }));

            var changedProperties = new List<string>();
            _model.PropertyChanged += (o, e) => changedProperties.Add(e.PropertyName);

            _model.AcceptChanges();

            Assert.That(changedProperties, Is.Empty);
            Assert.That(_model.IsModified, Is.False);
            Assert.That(_model.UpdatedPropertyKeys, Is.EquivalentTo(new int[0]));
            Assert.That(_model.Str, Is.EqualTo("Test"));
        }

        [Test]
        public void TestDiscardChanges()
        {
            _model.Str = "Test";

            Assert.That(_model.IsModified, Is.True);
            Assert.That(_model.UpdatedPropertyKeys, Is.EquivalentTo(new int[] { TestClass.StrProperty.Key }));

            var changedProperties = new List<string>();
            _model.PropertyChanged += (o, e) => changedProperties.Add(e.PropertyName);

            _model.DiscardChanges();

            Assert.That(changedProperties, Has.Member("Str"));
            Assert.That(_model.IsModified, Is.False);
            Assert.That(_model.UpdatedPropertyKeys, Is.EquivalentTo(new int[0]));
            Assert.That(_model.Str, Is.EqualTo("Default"));
        }

        [Test]
        public void TestModelPropertyChanged()
        {
            ModelPropertyChangedEventArgs args = null;
            _model.AddPropertyChangedHandler(TestClass.StrProperty, (o, e) => args = e);

            _model.Str = "Default";
            Assert.That(args, Is.Null);

            _model.Str = "Test";
            Assert.That(args, Is.Not.Null);
            Assert.That(args.Property, Is.EqualTo(TestClass.StrProperty));
            Assert.That(args.OldValue, Is.EqualTo("Default"));
            Assert.That(args.NewValue, Is.EqualTo("Test"));

            args = null;
            _model.Str = "Test";
            Assert.That(args, Is.Null);

            _model.Str = "Test2";
            Assert.That(args, Is.Not.Null);
            Assert.That(args.Property, Is.EqualTo(TestClass.StrProperty));
            Assert.That(args.OldValue, Is.EqualTo("Test"));
            Assert.That(args.NewValue, Is.EqualTo("Test2"));

            _model.Str = "Default";
            Assert.That(args, Is.Not.Null);
            Assert.That(args.Property, Is.EqualTo(TestClass.StrProperty));
            Assert.That(args.OldValue, Is.EqualTo("Test2"));
            Assert.That(args.NewValue, Is.EqualTo("Default"));
        }

        [Test]
        public void TestSetOriginalValue()
        {
            Assert.That(_model.IsModified, Is.False);
            Assert.That(_model.UpdatedPropertyKeys, Is.EquivalentTo(new int[0]));

            var changedProperties = new List<string>();
            _model.PropertyChanged += (o, e) => changedProperties.Add(e.PropertyName);

            _model.SetOriginalValue(TestClass.StrProperty, "Test");

            Assert.That(_model.Str, Is.EqualTo("Test"));
            Assert.That(changedProperties, Has.Member("Str"));
            Assert.That(_model.IsModified, Is.False);
            Assert.That(_model.UpdatedPropertyKeys, Is.EquivalentTo(new int[0]));
        }

        [Test]
        public void TestSetOriginalWithModifiedValue()
        {
            Assert.That(_model.IsModified, Is.False);
            Assert.That(_model.UpdatedPropertyKeys, Is.EquivalentTo(new int[0]));
            _model.Str = "Test";
            Assert.That(_model.IsModified, Is.True);

            var changedProperties = new List<string>();
            _model.PropertyChanged += (o, e) => changedProperties.Add(e.PropertyName);

            _model.SetOriginalValue(TestClass.StrProperty, "Blah");

            Assert.That(_model.Str, Is.EqualTo("Test"));
            Assert.That(changedProperties, Has.No.Member("Str"));
            Assert.That(_model.IsModified, Is.True);
            Assert.That(_model.UpdatedPropertyKeys, Is.EquivalentTo(new int[] { TestClass.StrProperty.Key }));

            changedProperties.Clear();
            _model.Str = "Blah";
            Assert.That(_model.Str, Is.EqualTo("Blah"));
            Assert.That(changedProperties, Has.Member("Str"));
            Assert.That(_model.IsModified, Is.False);
            Assert.That(_model.UpdatedPropertyKeys, Is.EquivalentTo(new int[0]));
        }

        [Test]
        public void TestSetOriginalToModifiedValue()
        {
            Assert.That(_model.IsModified, Is.False);
            Assert.That(_model.UpdatedPropertyKeys, Is.EquivalentTo(new int[0]));
            _model.Str = "Test";
            Assert.That(_model.IsModified, Is.True);

            var changedProperties = new List<string>();
            _model.PropertyChanged += (o, e) => changedProperties.Add(e.PropertyName);

            _model.SetOriginalValue(TestClass.StrProperty, "Test");

            Assert.That(_model.Str, Is.EqualTo("Test"));
            Assert.That(changedProperties, Has.No.Member("Str"));
            Assert.That(_model.IsModified, Is.False);
            Assert.That(_model.UpdatedPropertyKeys, Is.EquivalentTo(new int[0]));
        }
    }
}
