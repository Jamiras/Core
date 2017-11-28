using System;
using Jamiras.DataModels;
using NUnit.Framework;

namespace Jamiras.Core.Tests.DataModels
{
    [TestFixture]
    public class ModelPropertyTests
    {
        private class TestClass1
        {
        }

        private class TestClass2 : TestClass1
        {
        }

        [SetUp]
        public void Setup()
        {
            ResetModelProperties();
        }

        [TestFixtureTearDown]
        public void FixtureTeardown()
        {
            ResetModelProperties();
        }

        public static void ResetModelProperties()
        {
            typeof(ModelProperty).GetField("_properties", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).SetValue(null, null);
            typeof(ModelProperty).GetField("_keyCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).SetValue(null, 0);
        }

        [Test]
        public void TestRegister()
        {
            EventHandler<ModelPropertyChangedEventArgs> handler = (o, e) => { };
            var property = ModelProperty.Register(typeof(TestClass1), "PropertyName", typeof(string), "Default", handler);

            Assert.That(property.OwnerType, Is.EqualTo(typeof(TestClass1)));
            Assert.That(property.PropertyName, Is.EqualTo("PropertyName"));
            Assert.That(property.PropertyType, Is.EqualTo(typeof(string)));
            Assert.That(property.DefaultValue, Is.EqualTo("Default"));
            Assert.That(property.PropertyChangedHandler, Is.EqualTo(handler));
            Assert.That(property.FullName, Is.EqualTo("TestClass1.PropertyName"));
            Assert.That(property.Key, Is.GreaterThan(0));

            var propertyLookup = ModelProperty.GetPropertyForKey(property.Key);
            Assert.That(propertyLookup, Is.Not.Null.And.SameAs(property));
        }

        [Test]
        public void TestInvalidDefaultValue()
        {
            Assert.That(() => ModelProperty.Register(typeof(TestClass1), "PropertyName", typeof(string), 0), Throws.InstanceOf<InvalidCastException>());
        }

        [Test]
        public void TestGetPropertyForKeyZero()
        {
            var property = ModelProperty.GetPropertyForKey(0);
            Assert.That(property, Is.Null);
        }

        [Test]
        public void TestGetPropertyForKeyNegative()
        {
            var property = ModelProperty.GetPropertyForKey(-1);
            Assert.That(property, Is.Null);
        }

        [Test]
        public void TestGetPropertyForKeyInvalid()
        {
            var property = ModelProperty.GetPropertyForKey(Int32.MaxValue);
            Assert.That(property, Is.Null);
        }

        [Test]
        public void TestGetPropertiesForType()
        {
            var property1 = ModelProperty.Register(typeof(TestClass1), "Property1", typeof(string), "Default");
            var property2 = ModelProperty.Register(typeof(TestClass1), "Property2", typeof(int), 0);
            var property3 = ModelProperty.Register(typeof(TestClass1), "Property3", typeof(bool), false);

            var properties = ModelProperty.GetPropertiesForType(typeof(TestClass1));
            Assert.That(properties, Is.Not.Null.And.EquivalentTo(new[] { property1, property2, property3 }));
        }

        [Test]
        public void TestGetPropertiesForSubType()
        {
            var property1 = ModelProperty.Register(typeof(TestClass1), "Property1", typeof(string), "Default");
            var property2 = ModelProperty.Register(typeof(TestClass2), "Property2", typeof(int), 0);
            var property3 = ModelProperty.Register(typeof(TestClass2), "Property3", typeof(bool), false);

            var properties = ModelProperty.GetPropertiesForType(typeof(TestClass1));
            Assert.That(properties, Is.Not.Null.And.EquivalentTo(new[] { property1 }));

            properties = ModelProperty.GetPropertiesForType(typeof(TestClass2));
            Assert.That(properties, Is.Not.Null.And.EquivalentTo(new[] { property1, property2, property3 }));
        }

        [Test]
        public void TestEquals()
        {
            var property1 = ModelProperty.Register(typeof(TestClass1), "Property1", typeof(string), "Default");
            var property2 = ModelProperty.Register(typeof(TestClass2), "Property2", typeof(int), 0);

            Assert.That(property1.Equals(property1), Is.True);
            Assert.That(property1.Equals(property2), Is.False);
            Assert.That(property2.Equals(property1), Is.False);
            Assert.That(property2.Equals(property2), Is.True);

#pragma warning disable CS1718 // Comparison made to same variable - testing equality operator
            Assert.That(property1 == property1, Is.True);
#pragma warning restore CS1718 // Comparison made to same variable
            Assert.That(property1 == property2, Is.False);

            Assert.That(property1.Equals("Property1"), Is.False);
            Assert.That(property1.Equals(typeof(string)), Is.False);
            Assert.That(property1.Equals(null), Is.False);
        }

        [Test]
        public void TestGetHashCode()
        {
            var property1 = ModelProperty.Register(typeof(TestClass1), "Property1", typeof(string), "Default");
            var property2 = ModelProperty.Register(typeof(TestClass2), "Property2", typeof(int), 0);

            Assert.That(property1.GetHashCode(), Is.EqualTo(property1.GetHashCode()));
            Assert.That(property2.GetHashCode(), Is.EqualTo(property2.GetHashCode()));
            Assert.That(property1.GetHashCode(), Is.Not.EqualTo(property2.GetHashCode()));
        }

        [Test]
        public void IsValueValidString()
        {
            var property = ModelProperty.Register(typeof(TestClass1), "Property", typeof(string), "Default");
            Assert.That(property.IsValueValid(null), Is.True);
            Assert.That(property.IsValueValid(String.Empty), Is.True);
            Assert.That(property.IsValueValid("Happy"), Is.True);
            Assert.That(property.IsValueValid("A really really really really really (ok not really) long string"), Is.True);
            Assert.That(property.IsValueValid(0), Is.False);
            Assert.That(property.IsValueValid(false), Is.False);
            Assert.That(property.IsValueValid(4.6), Is.False);
            Assert.That(property.IsValueValid('c'), Is.False);
            Assert.That(property.IsValueValid(DayOfWeek.Tuesday), Is.False);
        }

        [Test]
        public void IsValueValidInteger()
        {
            var property = ModelProperty.Register(typeof(TestClass1), "Property", typeof(int), 0);
            Assert.That(property.IsValueValid(0), Is.True);
            Assert.That(property.IsValueValid(1), Is.True);
            Assert.That(property.IsValueValid(-1), Is.True);
            Assert.That(property.IsValueValid(Int32.MaxValue), Is.True);
            Assert.That(property.IsValueValid(Int32.MinValue), Is.True);
            Assert.That(property.IsValueValid(Int64.MaxValue), Is.False, "long doesn't fit");
            Assert.That(property.IsValueValid(Int64.MinValue), Is.False);
            Assert.That(property.IsValueValid(DayOfWeek.Tuesday), Is.True, "enum can be cast");
            Assert.That(property.IsValueValid(null), Is.False);
            Assert.That(property.IsValueValid(String.Empty), Is.False);
            Assert.That(property.IsValueValid("Happy"), Is.False);
            Assert.That(property.IsValueValid(false), Is.False);
            Assert.That(property.IsValueValid(4.6), Is.False);
            Assert.That(property.IsValueValid('c'), Is.False);
        }

        [Test]
        public void IsValueValidEnum()
        {
            var property = ModelProperty.Register(typeof(TestClass1), "Property", typeof(DayOfWeek), DayOfWeek.Tuesday);
            Assert.That(property.IsValueValid(DayOfWeek.Tuesday), Is.True);
            Assert.That(property.IsValueValid(0), Is.True, "number can be cast");
            Assert.That(property.IsValueValid(18), Is.True, "number out of range can be cast");
            Assert.That(property.IsValueValid(null), Is.False);
            Assert.That(property.IsValueValid(String.Empty), Is.False);
            Assert.That(property.IsValueValid("Happy"), Is.False);
            Assert.That(property.IsValueValid(false), Is.False);
            Assert.That(property.IsValueValid(4.6), Is.False);
            Assert.That(property.IsValueValid('c'), Is.False);
        }

        [Test]
        public void IsValueValidClass()
        {
            var property = ModelProperty.Register(typeof(TestClass1), "Property", typeof(TestClass1), null);
            Assert.That(property.IsValueValid(null), Is.True);
            Assert.That(property.IsValueValid(new TestClass1()), Is.True);
            Assert.That(property.IsValueValid(new TestClass2()), Is.True, "subclass can be assigned");
            Assert.That(property.IsValueValid("Happy"), Is.False);
            Assert.That(property.IsValueValid(0), Is.False);
            Assert.That(property.IsValueValid(false), Is.False);
            Assert.That(property.IsValueValid(4.6), Is.False);
            Assert.That(property.IsValueValid('c'), Is.False);
            Assert.That(property.IsValueValid(DayOfWeek.Tuesday), Is.False);

            property = ModelProperty.Register(typeof(TestClass1), "Property", typeof(TestClass2), null);
            Assert.That(property.IsValueValid(null), Is.True);
            Assert.That(property.IsValueValid(new TestClass2()), Is.True);
            Assert.That(property.IsValueValid(new TestClass1()), Is.False, "superclass cannot be assigned");
        }

        [Test]
        public void TestRegisterDependant()
        {
            var property1 = ModelProperty.Register(typeof(TestClass1), null, typeof(string), null);
            var property = ModelProperty.RegisterDependant(typeof(TestClass1), "PropertyName", typeof(string),
                new[] { property1 }, model => "");

            Assert.That(property.OwnerType, Is.EqualTo(typeof(TestClass1)));
            Assert.That(property.PropertyName, Is.EqualTo("PropertyName"));
            Assert.That(property.PropertyType, Is.EqualTo(typeof(string)));
            Assert.That(property.DefaultValue, Is.InstanceOf<ModelProperty.UnitializedValue>());
            Assert.That(property.PropertyChangedHandler, Is.Null);
            Assert.That(property.FullName, Is.EqualTo("TestClass1.PropertyName"));
            Assert.That(property.Key, Is.GreaterThan(0));

            var propertyLookup = ModelProperty.GetPropertyForKey(property.Key);
            Assert.That(propertyLookup, Is.Not.Null.And.SameAs(property));

            Assert.That(property1.DependantProperties, Is.Not.Null.And.Contains(property.Key));
        }

        [Test]
        public void TestRegisterDependantSubclass()
        {
            var property1 = ModelProperty.Register(typeof(TestClass1), null, typeof(string), null);
            var property = ModelProperty.RegisterDependant(typeof(TestClass2), "PropertyName", typeof(string),
                new[] { property1 }, model => "");

            Assert.That(property.OwnerType, Is.EqualTo(typeof(TestClass2)));
            Assert.That(property1.DependantProperties, Is.Not.Null.And.Contains(property.Key));
        }

        [Test]
        public void TestRegisterDependantSuperclass()
        {
            var property1 = ModelProperty.Register(typeof(TestClass2), null, typeof(string), null);

            Assert.That(() => ModelProperty.RegisterDependant(typeof(TestClass1), "PropertyName", typeof(string),
                new[] { property1 }, model => ""), Throws.ArgumentException);
        }

        [Test]
        public void TestRegisterDependantUnrelated()
        {
            var property1 = ModelProperty.Register(typeof(TestClass1), null, typeof(string), null);

            Assert.That(() => ModelProperty.RegisterDependant(typeof(string), "PropertyName", typeof(string),
                new[] { property1 }, model => ""), Throws.ArgumentException);
        }

        [Test]
        public void TestRegisterDependantMultiple()
        {
            var property1 = ModelProperty.Register(typeof(TestClass1), null, typeof(string), null);
            var propertyA = ModelProperty.RegisterDependant(typeof(TestClass1), "PropertyNameA", typeof(string),
                new[] { property1 }, model => "");
            var propertyB = ModelProperty.RegisterDependant(typeof(TestClass1), "PropertyNameB", typeof(string),
                new[] { property1 }, model => "");

            Assert.That(property1.DependantProperties, Is.Not.Null.And.Contains(propertyA.Key).And.Contains(propertyB.Key));
        }
    }
}
