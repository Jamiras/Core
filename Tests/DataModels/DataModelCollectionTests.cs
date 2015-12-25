using System.Collections.Generic;
using System.Data;
using Jamiras.DataModels;
using NUnit.Framework;

namespace Jamiras.Core.Tests.DataModels
{
    [TestFixture]
    public class DataModelCollectionTests
    {
        private class TestClass : DataModelBase
        {
        }

        [SetUp]
        public void Setup()
        {
            _model = new DataModelCollection<TestClass>();
        }

        private DataModelCollection<TestClass> _model;

        [Test]
        public void TestInitialization()
        {
            Assert.That(_model.Count, Is.EqualTo(0));
            Assert.That(_model.IsModified, Is.False);
        }

        [Test]
        public void TestAdd()
        {
            var changedProperties = new List<string>();
            _model.PropertyChanged += (o, e) => changedProperties.Add(e.PropertyName);

            _model.Add(new TestClass());

            Assert.That(_model.Count, Is.EqualTo(1));
            Assert.That(_model.IsModified, Is.True);
            Assert.That(changedProperties, Has.Member("Count"), "Count may be passed through the view layer for binding and should raise an event.");
            Assert.That(changedProperties, Has.No.Member("IsCollectionChanged"), "IsCollectionChanged is similar to ModelBase.IsModified and should not raise an event.");
        }

        [Test]
        public void TestRemove()
        {
            var c = new TestClass();
            ((IDataModelCollection)_model).Add(c);
            Assert.That(_model.Count, Is.EqualTo(1));
            Assert.That(_model.IsModified, Is.False);

            var changedProperties = new List<string>();
            _model.PropertyChanged += (o, e) => changedProperties.Add(e.PropertyName);

            _model.Remove(c);

            Assert.That(_model.Count, Is.EqualTo(0));
            Assert.That(_model.IsModified, Is.True);
            Assert.That(changedProperties, Has.Member("Count"), "Count may be passed through the view layer for binding and should raise an event.");
            Assert.That(changedProperties, Has.No.Member("IsCollectionChanged"), "IsCollectionChanged is similar to ModelBase.IsModified and should not raise an event.");
        }

        [Test]
        public void TestAddAndRemove()
        {
            var c = new TestClass();

            var changedProperties = new List<string>();
            _model.PropertyChanged += (o, e) => changedProperties.Add(e.PropertyName);

            _model.Add(c);

            Assert.That(_model.Count, Is.EqualTo(1));
            Assert.That(_model.IsModified, Is.True);
            Assert.That(changedProperties, Has.Member("Count"));

            changedProperties.Clear();
            _model.Remove(c);

            Assert.That(_model.Count, Is.EqualTo(0));
            Assert.That(_model.IsModified, Is.False);
            Assert.That(changedProperties, Has.Member("Count"));
        }

        [Test]
        public void TestRemoveAndAdd()
        {
            var c = new TestClass();
            ((IDataModelCollection)_model).Add(c);
            Assert.That(_model.Count, Is.EqualTo(1));
            Assert.That(_model.IsModified, Is.False);

            var changedProperties = new List<string>();
            _model.PropertyChanged += (o, e) => changedProperties.Add(e.PropertyName);

            _model.Remove(c);

            Assert.That(_model.Count, Is.EqualTo(0));
            Assert.That(_model.IsModified, Is.True);
            Assert.That(changedProperties, Has.Member("Count"));

            changedProperties.Clear();
            _model.Add(c);

            Assert.That(_model.Count, Is.EqualTo(1));
            Assert.That(_model.IsModified, Is.False);
            Assert.That(changedProperties, Has.Member("Count"));
        }

        [Test]
        public void TestClear()
        {
            var c = new TestClass();
            ((IDataModelCollection)_model).Add(c);
            Assert.That(_model.Count, Is.EqualTo(1));
            Assert.That(_model.IsModified, Is.False);

            var changedProperties = new List<string>();
            _model.PropertyChanged += (o, e) => changedProperties.Add(e.PropertyName);

            _model.Clear();

            Assert.That(_model.Count, Is.EqualTo(0));
            Assert.That(_model.IsModified, Is.True);
            Assert.That(changedProperties, Has.Member("Count"));
        }

        [Test]
        public void TestAddAndClear()
        {
            var c = new TestClass();

            var changedProperties = new List<string>();
            _model.PropertyChanged += (o, e) => changedProperties.Add(e.PropertyName);

            _model.Add(c);

            Assert.That(_model.Count, Is.EqualTo(1));
            Assert.That(_model.IsModified, Is.True);
            Assert.That(changedProperties, Has.Member("Count"));

            changedProperties.Clear();
            _model.Clear();

            Assert.That(_model.Count, Is.EqualTo(0));
            Assert.That(_model.IsModified, Is.False);
            Assert.That(changedProperties, Has.Member("Count"));
        }

        [Test]
        public void TestAddAndAccept()
        {
            var c = new TestClass();
            _model.Add(c);

            Assert.That(_model.Count, Is.EqualTo(1));
            Assert.That(_model.IsModified, Is.True);

            _model.AcceptChanges();

            Assert.That(_model.Count, Is.EqualTo(1));
            Assert.That(_model.IsModified, Is.False);

            _model.Remove(c);

            Assert.That(_model.Count, Is.EqualTo(0));
            Assert.That(_model.IsModified, Is.True);
        }

        [Test]
        public void TestAddAndDiscard()
        {
            var c = new TestClass();
            _model.Add(c);

            Assert.That(_model.Count, Is.EqualTo(1));
            Assert.That(_model.IsModified, Is.True);

            _model.DiscardChanges();

            Assert.That(_model.Count, Is.EqualTo(0));
            Assert.That(_model.IsModified, Is.False);
        }

        [Test]
        public void TestRemoveAndAccept()
        {
            var c = new TestClass();
            ((IDataModelCollection)_model).Add(c);

            _model.Remove(c);

            Assert.That(_model.Count, Is.EqualTo(0));
            Assert.That(_model.IsModified, Is.True);

            _model.AcceptChanges();

            Assert.That(_model.Count, Is.EqualTo(0));
            Assert.That(_model.IsModified, Is.False);

            _model.Add(c);

            Assert.That(_model.Count, Is.EqualTo(1));
            Assert.That(_model.IsModified, Is.True);
        }

        [Test]
        public void TestRemoveAndDiscard()
        {
            var c = new TestClass();
            ((IDataModelCollection)_model).Add(c);

            _model.Remove(c);

            Assert.That(_model.Count, Is.EqualTo(0));
            Assert.That(_model.IsModified, Is.True);

            _model.DiscardChanges();

            Assert.That(_model.Count, Is.EqualTo(1));
            Assert.That(_model[0], Is.SameAs(c));
            Assert.That(_model.IsModified, Is.False);
        }

        [Test]
        public void TestIsReadOnly()
        {
            var c = new TestClass();

            Assert.That(_model.IsReadOnly, Is.False);
            ((IDataModelCollection)_model).MakeReadOnly();
            Assert.That(_model.IsReadOnly, Is.True);
            Assert.That(() => ((IDataModelCollection)_model).MakeReadOnly(), Throws.InvalidOperationException);

            Assert.That(_model.IsModified, Is.False);
            Assert.That(_model.Count, Is.EqualTo(0));
            Assert.That(_model.Contains(c), Is.False);
            Assert.That(() => _model.Add(c), Throws.InstanceOf<ReadOnlyException>());
            Assert.That(() => _model.Remove(c), Throws.InstanceOf<ReadOnlyException>());
            Assert.That(() => _model.Clear(), Throws.InstanceOf<ReadOnlyException>());
        }
    }
}
