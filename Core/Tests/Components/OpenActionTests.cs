using System;
using Jamiras.Components;
using NUnit.Framework;

namespace Jamiras.Core.Tests.Components
{
    [TestFixture]
    class OpenActionTests
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

            public static void SetStaticValue(int newValue)
            {
                throw new NotSupportedException();
            }

            public static void SetStaticValues(int newValue1, int newValue2)
            {
                throw new NotSupportedException();
            }
        }

        private void LocalMethod(int newValue)
        {
        }

        [Test]
        public void TestCreateOpenAction()
        {
            var target = new TestClass();
            Action<object, int> openAction = OpenAction.CreateOpenAction<int>(target.SetValue);
            Assert.That(openAction, Is.Not.Null, "failed to create openAction");

            openAction(target, 3);
            Assert.That(target.Value, Is.EqualTo(3));
        }

        [Test]
        public void TestOpenActionSecondTarget()
        {
            var target = new TestClass();
            Action<object, int> openAction = OpenAction.CreateOpenAction<int>(target.SetValue);
            Assert.That(openAction, Is.Not.Null, "failed to create openAction");

            var target2 = new TestClass();
            openAction(target2, 3);
            Assert.That(target2.Value, Is.EqualTo(3));
        }

        [Test]
        public void TestOpenActionTargetAsObject()
        {
            var target = new TestClass();
            Action<object, int> openAction = OpenAction.CreateOpenAction<int>(target.SetValue);
            Assert.That(openAction, Is.Not.Null, "failed to create openAction");

            openAction(target, 3);
            Assert.That(target.Value, Is.EqualTo(3));
        }

        [Test]
        public void TestOpenActionWrongTargetType()
        {
            var target = new TestClass();
            Action<object, int> openAction = OpenAction.CreateOpenAction<int>(target.SetValue);
            Assert.That(openAction, Is.Not.Null, "failed to create openAction");

            Assert.That(() => openAction(this, 3), Throws.InstanceOf<InvalidCastException>());
        }

        [Test]
        public void TestCreateOpenActionArgumentNull()
        {
            Assert.That(() => OpenAction.CreateOpenAction((Action<int>)null), Throws.InstanceOf<ArgumentNullException>());
        }

        [Test]
        public void TestCreateOpenActionStaticMethod()
        {
            Assert.That(() => OpenAction.CreateOpenAction<int>(TestClass.SetStaticValue), Throws.ArgumentException);

            Action<int,int> method = TestClass.SetStaticValues;
            Assert.That(() => OpenAction.CreateOpenAction<int, int>(method.Method), Throws.ArgumentException);
        }

        [Test]
        public void TestHasClosureReferenceStaticMethod()
        {
            Action<int> action = TestClass.SetStaticValue;
            Assert.That(OpenAction.HasClosureReference(action), Is.False);
        }

        [Test]
        public void TestHasClosureReferenceStaticMethodLambda()
        {
            Action<int> action = i => TestClass.SetStaticValue(i + 1);
            Assert.That(OpenAction.HasClosureReference(action), Is.False);
        }

        [Test]
        public void TestHasClosureReferenceClassMethod()
        {
            var target = new TestClass();
            Action<int> action = target.SetValue;
            Assert.That(OpenAction.HasClosureReference(action), Is.False);
        }

        [Test]
        public void TestHasClosureReferenceClassMethodLambda()
        {
            var target = new TestClass();
            Action<int> action = i => target.SetValue(i + 1);
            Assert.That(OpenAction.HasClosureReference(action), Is.True);
        }

        [Test]
        public void TestHasClosureReferenceThisMethod()
        {
            Action<int> action = LocalMethod;
            Assert.That(OpenAction.HasClosureReference(action), Is.False);
        }

        [Test]
        public void TestHasClosureReferenceThisMethodLambda()
        {
            Action<int> action = i => LocalMethod(i + 1);
            Assert.That(OpenAction.HasClosureReference(action), Is.False);
        }

        [Test]
        public void TestHasClosureReferenceLocalVariableLambda()
        {
            int value = 0;
            Action<int> action = i => value = i;
            Assert.That(OpenAction.HasClosureReference(action), Is.True);
            Assert.That(value, Is.EqualTo(0)); // prevent unused variable
        }

        [Test]
        public void TestCreateOpenActionTwoParameters()
        {
            var target = new TestClass();
            Action<int,int> method = target.SetValues;
            Action<object, int, int> openAction = OpenAction.CreateOpenAction<int, int>(method.Method);
            Assert.That(openAction, Is.Not.Null, "failed to create openAction");

            openAction(target, 3, 4);
            Assert.That(target.Value, Is.EqualTo(7));
        }
    }
}
