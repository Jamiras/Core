using System;
using System.Reflection;
using Jamiras.Components;
using NUnit.Framework;

namespace Jamiras.Core.Tests.Components
{
    [TestFixture]
    class WeakEventHandlerTests
    {
        private class EventRaisingClass
        {
            public event EventHandler<EventArgs> Event;

            public void RaiseEvent(EventArgs e)
            {
                if (Event != null)
                    Event(this, e);
            }

            public int SubscriptionCount
            {
                get 
                {
                    if (Event == null)
                        return 0;

                    return Event.GetInvocationList().Length; 
                }
            }
        }

        private bool _eventRaised;

        [Test]
        public void TestSubscription_Simple()
        {
            // subscription should not occur until handler is registered
            EventRaisingClass observed = new EventRaisingClass();
            Assert.AreEqual(0, observed.SubscriptionCount);

            _eventRaised = false;
            observed.Event += new WeakEventHandler<EventArgs>((o, e) => _eventRaised = true).Handler;

            // subscription should have occurred
            Assert.AreEqual(1, observed.SubscriptionCount);

            observed.RaiseEvent(EventArgs.Empty);
            Assert.IsTrue(_eventRaised);
        }

        [Test]
        public void TestUnsubscription_Simple()
        {
            EventRaisingClass observed = new EventRaisingClass();

            _eventRaised = false;
            var handler = new WeakEventHandler<EventArgs>((o, e) => _eventRaised = true, h => observed.Event -= h);
            observed.Event += handler.Handler;
            Assert.AreEqual(1, observed.SubscriptionCount);

            // unregistration should detach event
            observed.Event -= handler.Handler;
            Assert.AreEqual(0, observed.SubscriptionCount);
            Assert.IsFalse(_eventRaised);
        }

        [Test]
        public void TestSubscription_Null()
        {
            Assert.That(() => new WeakEventHandler<EventArgs>(null), Throws.InstanceOf<ArgumentNullException>());
        }

        [Test]
        public void TestSubscription_Static()
        {
            Assert.That(() => new WeakEventHandler<EventArgs>(StaticHandler), Throws.ArgumentException);
        }

        [Test]
        public void TestSubscription_Closure()
        {
            int test = 0;
            Assert.That(() => new WeakEventHandler<EventArgs>((o, e) => test++), Throws.ArgumentException);
        }

        private static void InvalidateEventHandler(WeakEventHandler<EventArgs> weakEventHandler)
        {
            // use reflection to set WeakReference.Target to null (this simulates the target object going out of scope)
            FieldInfo targetField = weakEventHandler.GetType().BaseType.GetField("_target", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(targetField);

            WeakReference reference = targetField.GetValue(weakEventHandler) as WeakReference;
            Assert.IsNotNull(reference);

            reference.Target = null;
        }

        [Test]
        public void TestWeakEvent_Simple()
        {
            EventRaisingClass observed = new EventRaisingClass();

            _eventRaised = false;
            var weakEventHandler = new WeakEventHandler<EventArgs>((o, e) => _eventRaised = true, h => observed.Event -= h);
            observed.Event += weakEventHandler.Handler;
            Assert.AreEqual(1, observed.SubscriptionCount);

            // when target goes out of scope, event is not detached. the observer will be
            // temporarily leaked until the event fires
            InvalidateEventHandler(weakEventHandler);
            Assert.AreEqual(1, observed.SubscriptionCount);

            // if the target is no longer alive when the event is raised, the event handler should be
            // automatically unregistered, which will cause the observer to unsubscribe from the source
            observed.RaiseEvent(EventArgs.Empty);
            Assert.AreEqual(0, observed.SubscriptionCount);
            Assert.IsFalse(_eventRaised);
        }

        [Test]
        public void TestWeakEvent_ImplicitCast()
        {
            EventRaisingClass observed = new EventRaisingClass();

            _eventRaised = false;
            var weakEventHandler = new WeakEventHandler<EventArgs>((o, e) => _eventRaised = true, h => observed.Event -= h);
            observed.Event += weakEventHandler;
            Assert.AreEqual(1, observed.SubscriptionCount);

            // when target goes out of scope, event is not detached. the observer will be
            // temporarily leaked until the event fires
            InvalidateEventHandler(weakEventHandler);
            Assert.AreEqual(1, observed.SubscriptionCount);

            // if the target is no longer alive when the event is raised, the event handler should be
            // automatically unregistered, which will cause the observer to unsubscribe from the source
            observed.RaiseEvent(EventArgs.Empty);
            Assert.AreEqual(0, observed.SubscriptionCount);
            Assert.IsFalse(_eventRaised);
        }

        [Test]
        public void TestWeakEvent_Extension()
        {
            EventRaisingClass observed = new EventRaisingClass();

            _eventRaised = false;
            var handler = new EventHandler<EventArgs>((o, e) => _eventRaised = true).MakeWeak(h => observed.Event -= h);
            observed.Event += handler;
            Assert.AreEqual(1, observed.SubscriptionCount);

            // pull the WeakEventHandler instance out of the callback
            var weakEventHandler = handler.Target as WeakEventHandler<EventArgs>;
            Assert.IsNotNull(weakEventHandler);

            // when target goes out of scope, event is not detached. the observer will be
            // temporarily leaked until the event fires
            InvalidateEventHandler(weakEventHandler);
            Assert.AreEqual(1, observed.SubscriptionCount);

            // if the target is no longer alive when the event is raised, the event handler should be
            // automatically unregistered, which will cause the observer to unsubscribe from the source
            observed.RaiseEvent(EventArgs.Empty);
            Assert.AreEqual(0, observed.SubscriptionCount);
            Assert.IsFalse(_eventRaised);
        }

        [Test]
        public void TestWeakEvent_Extension_Static()
        {
            EventRaisingClass observed = new EventRaisingClass();

            var handler = new EventHandler<EventArgs>(StaticHandler);
            Assert.That(() => handler.MakeWeak(h => observed.Event -= h), Throws.ArgumentException);
        }

        private static void StaticHandler(object sender, EventArgs e)
        {
            // do nothing
        }

        [Test]
        public void TestSubscription_Complex()
        {
            // subscription should not occur until handler is registered
            EventRaisingClass observed = new EventRaisingClass();
            Assert.AreEqual(0, observed.SubscriptionCount);

            _eventRaised = false;
            observed.Event += new WeakEventHandler<EventHandler<EventArgs>, EventArgs>((o, e) => _eventRaised = true).Handler;

            // subscription should have occurred
            Assert.AreEqual(1, observed.SubscriptionCount);

            observed.RaiseEvent(EventArgs.Empty);
            Assert.IsTrue(_eventRaised);
        }

        [Test]
        public void TestUnsubscription_Complex()
        {
            EventRaisingClass observed = new EventRaisingClass();

            _eventRaised = false;
            var handler = new WeakEventHandler<EventHandler<EventArgs>, EventArgs>((o, e) => _eventRaised = true, h => observed.Event -= h);
            observed.Event += handler.Handler;
            Assert.AreEqual(1, observed.SubscriptionCount);

            // unregistration should detach event
            observed.Event -= handler.Handler;
            Assert.AreEqual(0, observed.SubscriptionCount);
            Assert.IsFalse(_eventRaised);
        }

        [Test]
        public void TestSubscription_ComplexNull()
        {
            Assert.That(() => new WeakEventHandler<EventHandler<EventArgs>, EventArgs>(null), Throws.InstanceOf<ArgumentNullException>());
        }

        [Test]
        public void TestSubscription_ComplexNonDelegate()
        {
            Assert.That(() => new WeakEventHandler<EventArgs, EventArgs>(EventArgs.Empty), Throws.InstanceOf<NotSupportedException>());
        }

        private static void InvalidateEventHandler(WeakEventHandler<EventHandler<EventArgs>, EventArgs> weakEventHandler)
        {
            // use reflection to set WeakReference.Target to null (this simulates the target object going out of scope)
            FieldInfo callbackField = weakEventHandler.GetType().GetField("_target", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(callbackField);

            WeakReference reference = callbackField.GetValue(weakEventHandler) as WeakReference;
            Assert.IsNotNull(reference);

            reference.Target = null;
        }

        [Test]
        public void TestWeakEvent_Complex()
        {
            EventRaisingClass observed = new EventRaisingClass();

            _eventRaised = false;
            var weakEventHandler = new WeakEventHandler<EventHandler<EventArgs>, EventArgs>((o, e) => _eventRaised = true, h => observed.Event -= h);
            observed.Event += weakEventHandler.Handler;
            Assert.AreEqual(1, observed.SubscriptionCount);

            // when target goes out of scope, event is not detached. the observer will be
            // temporarily leaked until the event fires
            InvalidateEventHandler(weakEventHandler);
            Assert.AreEqual(1, observed.SubscriptionCount);

            // if the target is no longer alive when the event is raised, the event handler should be
            // automatically unregistered, which will cause the observer to unsubscribe from the source
            observed.RaiseEvent(EventArgs.Empty);
            Assert.AreEqual(0, observed.SubscriptionCount);
            Assert.IsFalse(_eventRaised);
        }

        private class TestClass
        {
            public TestClass(EventRaisingClass observed)
            {
                _observed = observed;
                _observed.Event += new WeakEventHandler<EventArgs>(Callback, h => _observed.Event -= h).Handler;
            }

            private EventRaisingClass _observed;

            private void Callback(object sender, EventArgs e)
            {
                CallbackCalled = true;
            }

            public bool CallbackCalled { get; set; }
        }

        [Test]
        public void TestWeakEvent_UnregisterTargetsThis()
        {
            var observed = new EventRaisingClass();
            TestClass x;
            
            Assert.That(() => x = new TestClass(observed), Throws.ArgumentException);
        }
    }
}
