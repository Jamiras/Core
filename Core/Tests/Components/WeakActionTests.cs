using System;
using System.Runtime.CompilerServices;
using Jamiras.Components;
using NUnit.Framework;

namespace Jamiras.Core.Tests.Components
{
    [TestFixture]
    class WeakActionTests
    {
        private class TestClass
        {
            public int Value { get; private set; }

            public void SetValue(int newValue)
            {
                Value = newValue;
            }

            public void SetValues(int newValue1, int newValue2)
            {
                Value = newValue1 + newValue2;
            }

            public static int StaticValue { get; private set; }

            public static void SetStaticValue(int newValue)
            {
                StaticValue = newValue;
            }

            public static void SetStaticValues(int newValue1, int newValue2)
            {
                StaticValue = newValue1 + newValue2;
            }
        }

        [Test]
        public void TestSimple()
        {
            var target = new TestClass();
            WeakAction<int> weakAction = new WeakAction<int>(target.SetValue);
            Assert.That(weakAction.Target, Is.Not.Null, "Target not set");
            Assert.That(weakAction.IsAlive, Is.True, "Not alive");
            Assert.That(weakAction.Method, Is.EqualTo(typeof(TestClass).GetMethod("SetValue")));

            Assert.That(weakAction.Invoke(3), Is.True, "Invoke");
            Assert.That(target.Value, Is.EqualTo(3));
        }

        [Test]
        public void TestStatic()
        {
            WeakAction<int> weakAction = new WeakAction<int>(TestClass.SetStaticValue);
            Assert.That(weakAction.Target, Is.EqualTo(typeof(TestClass)), "Static method target should be declaring type");
            Assert.That(weakAction.IsAlive, Is.True, "Static method has no target, so should always be considered Alive");
            Assert.That(weakAction.Method, Is.EqualTo(typeof(TestClass).GetMethod("SetStaticValue")));

            TestClass.SetStaticValue(0);
            Assert.That(TestClass.StaticValue, Is.EqualTo(0));

            Assert.That(weakAction.Invoke(3), Is.True, "Invoke");
            Assert.That(TestClass.StaticValue, Is.EqualTo(3));
        }

        [Test]
        public void TestDeath()
        {
            var target = new TestClass();
            WeakAction<int> weakAction = new WeakAction<int>(target.SetValue);
            Assert.That(weakAction.Target, Is.Not.Null, "Target not set");
            Assert.That(weakAction.IsAlive, Is.True, "Not alive");

            weakAction.Target = null;
            Assert.That(weakAction.Target, Is.Null, "Target still set");
            Assert.That(weakAction.IsAlive, Is.False, "Still alive");

            Assert.That(weakAction.Invoke(3), Is.False, "Invoke");
            Assert.That(target.Value, Is.EqualTo(0), "Value updated after weak event died");
        }

        [Test]
        public void TestArgumentNull()
        {
            Assert.That(() => new WeakAction<int>(null), Throws.InstanceOf<ArgumentNullException>());
        }

        [Test]
        public void TestClosureLambda()
        {
            int localVariable = 0;
            Assert.That(() => new WeakAction<int>(value => localVariable = value), Throws.ArgumentException);
            Assert.That(localVariable, Is.EqualTo(0)); // to prevent unused variable compilation error
        }

        [Test]
        public void TestNonClosureLambda()
        {
            var target = new TestClass();
            var weakAction = new WeakAction<TestClass>(instance => instance.SetValue(3));

            Assert.That(weakAction.Invoke(target), Is.True);
            Assert.That(target.Value, Is.EqualTo(3), "Value not updated on target");
        }


        [Test]
        public void TestWeakReference()
        {
            // in .NET Core, WeakReferences have to be created in a different scope than
            // where they're used in order for the Garbage Collector to act on them.
            Func<WeakReferenceTester<TestClass>> createWeakReferenceTester = () =>
            {
                return new WeakReferenceTester<TestClass>(() => new TestClass());
            };

            Func<WeakReferenceTester<TestClass>,WeakAction<int>> createWeakAction = (WeakReferenceTester<TestClass> weakTarget) =>
            {
                return new WeakAction<int>(weakTarget.Target.SetValue);
            };

            var weakTarget = createWeakReferenceTester();
            var weakAction = createWeakAction(weakTarget);

            Assert.That(weakTarget.Expire(), Is.True, "Could not garbage collect target");
            Assert.That(weakAction.Invoke(3), Is.False, "Invoke did not indicate target death");
        }

        [Test]
        public void TestSimpleTwoParameters()
        {
            var target = new TestClass();
            WeakAction<int,int> weakAction = new WeakAction<int,int>(target.SetValues);
            Assert.That(weakAction.Target, Is.Not.Null, "Target not set");
            Assert.That(weakAction.IsAlive, Is.True, "Not alive");
            Assert.That(weakAction.Method, Is.EqualTo(typeof(TestClass).GetMethod("SetValues")));

            Assert.That(weakAction.Invoke(3, 4), Is.True, "Invoke");
            Assert.That(target.Value, Is.EqualTo(7));
        }

        [Test]
        public void TestStaticTwoParameters()
        {
            WeakAction<int,int> weakAction = new WeakAction<int,int>(TestClass.SetStaticValues);
            Assert.That(weakAction.Target, Is.EqualTo(typeof(TestClass)), "Static method target should be declaring type");
            Assert.That(weakAction.IsAlive, Is.True, "Static method has no target, so should always be considered Alive");
            Assert.That(weakAction.Method, Is.EqualTo(typeof(TestClass).GetMethod("SetStaticValues")));

            TestClass.SetStaticValues(0, 1);
            Assert.That(TestClass.StaticValue, Is.EqualTo(1));

            Assert.That(weakAction.Invoke(3, 4), Is.True, "Invoke");
            Assert.That(TestClass.StaticValue, Is.EqualTo(7));
        }

        [Test]
        public void TestDeathTwoParameters()
        {
            var target = new TestClass();
            WeakAction<int,int> weakAction = new WeakAction<int,int>(target.SetValues);
            Assert.That(weakAction.Target, Is.Not.Null, "Target not set");
            Assert.That(weakAction.IsAlive, Is.True, "Not alive");

            weakAction.Target = null;
            Assert.That(weakAction.Target, Is.Null, "Target still set");
            Assert.That(weakAction.IsAlive, Is.False, "Still alive");

            Assert.That(weakAction.Invoke(3, 4), Is.False, "Invoke");
            Assert.That(target.Value, Is.EqualTo(0), "Value updated after weak event died");
        }

        [Test]
        public void TestArgumentNullTwoParameters()
        {
            Assert.That(() => new WeakAction<int,int>(null), Throws.InstanceOf<ArgumentNullException>());
        }

        [Test]
        public void TestClosureLambdaTwoParameters()
        {
            int localVariable = 0;
            Assert.That(() => new WeakAction<int,int>((value1, value2) => localVariable = value1 + value2), Throws.ArgumentException);
            Assert.That(localVariable, Is.EqualTo(0)); // to prevent unused variable compilation error
        }

        [Test]
        public void TestNonClosureLambdaTwoParameters()
        {
            var target = new TestClass();
            var weakAction = new WeakAction<TestClass,int>((instance,value) => instance.SetValue(value));

            Assert.That(weakAction.Invoke(target, 3), Is.True);
            Assert.That(target.Value, Is.EqualTo(3), "Value not updated on target");
        }

        [Test]
        public void TestWeakReferenceTwoParameters()
        {
            // in .NET Core, WeakReferences have to be created in a different scope than
            // where they're used in order for the Garbage Collector to act on them.
            Func<WeakReferenceTester<TestClass>> createWeakReferenceTester = () =>
            {
                return new WeakReferenceTester<TestClass>(() => new TestClass());
            };

            Func<WeakReferenceTester<TestClass>,WeakAction<int,int>> createWeakAction = (WeakReferenceTester<TestClass> weakTarget) =>
            {
                return new WeakAction<int, int>(weakTarget.Target.SetValues);
            };

            var weakTarget = createWeakReferenceTester();
            var weakAction = createWeakAction(weakTarget);

            Assert.That(weakTarget.Expire(), Is.True, "Could not garbage collect target");
            Assert.That(weakAction.Invoke(3, 4), Is.False, "Invoke did not indicate target death");
        }
    }
}
