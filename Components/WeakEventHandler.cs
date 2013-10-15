using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Jamiras.Components
{
    /// <summary>
    /// Helper class for subscribing to an event in a way that doesn't cause
    /// the event listener to be held in memory after it should have gone out of scope.
    /// </summary>
    /// <typeparam name="TEventHandler">EventHandler delegate type (use WeakEventHandler&lt;TEventArgs&gt; if EventHandler&lt;T&gt;)</typeparam>
    /// <typeparam name="TEventArgs">EventArgs passed through the event</typeparam>
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix",
        Justification="This is meant to be an extension of EventHandler, even though it doesn't inherit from it")]
    public class WeakEventHandler<TEventHandler, TEventArgs>
        where TEventHandler : class // Cannot implicitly require TEventHandler be Delegate
        where TEventArgs : EventArgs
    {
        /// <summary>
        /// Creates a weak EventHandler wrapper
        /// </summary>
        /// <param name="eventHandler">EventHandler to create a weak wrapper for</param>
        /// <exception cref="ArgumentNullException"><paramref name="eventHandler"/> is null.</exception>
        /// <exception cref="NotSupportedException"><typeparamref name="TEventHandler"/> is not a delegate type.</exception>
        /// <exception cref="ArgumentException"><paramref name="eventHandler"/> is a generated closure and may go out of scope prematurely.</exception>
        public WeakEventHandler(TEventHandler eventHandler)
            : this(eventHandler, null)
        {
        }

        /// <summary>
        /// Creates a weak EventHandler wrapper
        /// </summary>
        /// <param name="eventHandler">EventHandler to create a weak wrapper for</param>
        /// <param name="unregisterMethod">Lambda expression for unregistration (i.e. "h => obj.Event -= h")</param>
        /// <exception cref="ArgumentNullException"><paramref name="eventHandler"/> is null.</exception>
        /// <exception cref="NotSupportedException"><typeparamref name="TEventHandler"/> is not a delegate type.</exception>
        /// <exception cref="ArgumentException"><paramref name="eventHandler"/> is a generated closure and may go out of scope prematurely. -or- <paramref name="unregisterMethod"/> references the <paramref name="eventHandler" /> target.</exception>
        /// <remarks>
        /// The <paramref name="unregisterMethod"/> is held on to with a strong reference, so anything referenced in 
        /// the lambda expression will not be garbage collected until the <paramref name="eventHandler"/> target goes
        /// out of scope and the event is unregistered. It is recommended that you store the object containing the
        /// event in a local variable and use the local variable for unsubcribing to minimize object retention
        /// caused by property chains (i.e. "h => obj1.Property1.Property2.Event -= h" will hold on to obj1, which
        /// will presumably hold on to whatever is in Property1 and Property2. Furthermore, if Property1 or
        /// Property2 changes before the event is unsubscribed, the unsubscription will occur on the wrong 
        /// Property2 instance).
        /// </remarks>
        public WeakEventHandler(TEventHandler eventHandler, Action<TEventHandler> unregisterMethod)
        {
            if (eventHandler == null)
                throw new ArgumentNullException("eventHandler");

            // Make sure TEventHandler is a Delegate since we can't enforce it in a where clause
            Delegate eventHandlerDelegate = eventHandler as Delegate;
            if (eventHandlerDelegate == null)
                throw new NotSupportedException(typeof(TEventHandler).Name + " is not a delegate type.");

            // if there is no eventHandler target to create a weak reference to, the method will never go out of scope
            if (eventHandlerDelegate.Method.IsStatic)
                throw new ArgumentException("Cannot create weak event from static method, there is no object to reference that would go out of scope", "eventHandler");

            // we create a weak reference to the eventHandler target, but hold a strong reference to the unregisterMethod
            // if the unregisterMethod is on the eventHandler target, the target cannot be garbage collected and will be leaked.
            if (unregisterMethod != null && eventHandlerDelegate.Target != null && eventHandlerDelegate.Target == unregisterMethod.Target)
                throw new ArgumentException("Unregister method exists on same object as event handler, which will prevent the object from being garbage collected. This is typically the result of using a class variable instead of a local variable in the unregister method.", "unregisterMethod");

            // the lifetime of a generated closue is only the lifetime of the delegate itself. Closures are not
            // generated if the anonymous delegate only references variables passed in to it, or the "this" psuedo-variable.
            if (OpenAction.HasClosureReference(eventHandlerDelegate))
                throw new ArgumentException("Cannot create weak event from generated closure", "eventHandler");

            if (_dispatchEventMethod == null)
            {
                // expression is slightly faster than reflection, and prevents FxCop from saying DispatchEvent is not referenced.
                Expression<Action<object, TEventArgs>> expression = (o, e) => DispatchEvent(o, e);
                var dispatchEvent = expression.Body as MethodCallExpression;
                if (dispatchEvent == null)
                    throw new InvalidOperationException("Unable to locate DispatchEvent method");

                _dispatchEventMethod = dispatchEvent.Method;
            }

            // create a TEventHandler delegate pointing at DispatchEvent
            Handler = (TEventHandler)(object)Delegate.CreateDelegate(typeof(TEventHandler), this, _dispatchEventMethod);

            // create weak reference to listener
            _methodInfo = eventHandlerDelegate.Method;
            _target = new WeakReference(eventHandlerDelegate.Target);

            // create strong reference to unregister method - if it goes away, we can't unregister.
            // this requires that the unregister method does not reference the listener, or the listener
            // will never go out of scope!
            _unregisterMethod = unregisterMethod;
        }

        private readonly WeakReference _target;
        private readonly MethodInfo _methodInfo;
        private readonly Action<TEventHandler> _unregisterMethod;
        private Action<object, object, TEventArgs> _delegate;
        private static MethodInfo _dispatchEventMethod;

        private void DispatchEvent(object sender, TEventArgs e)
        {
            // if the target is still alive, call the event handler, otherwise detach from the event
            var target = _target.Target;
            if (target != null && _target.IsAlive)
            {
                if (_delegate == null)
                    _delegate = OpenAction.CreateOpenAction<object, TEventArgs>(_methodInfo);

                _delegate(target, sender, e);
            }
            else if (_unregisterMethod != null)
            {
                _unregisterMethod(Handler);
            }
        }

        /// <summary>
        /// Gets the delegate to strongly attach to the event.
        /// </summary>
        public TEventHandler Handler { get; private set; }
    }

    /// <summary>
    /// Helper class for subscribing to an event in a way that doesn't cause
    /// the event listener to be held in memory after it should have gone out of scope.
    /// </summary>
    /// <typeparam name="TEventArgs">EventArgs passed through the event</typeparam>
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix",
        Justification = "This is meant to be an extension of EventHandler, even though it doesn't inherit from it")]
    public class WeakEventHandler<TEventArgs> : WeakEventHandler<EventHandler<TEventArgs>, TEventArgs>
        where TEventArgs : EventArgs
    {
        /// <summary>
        /// Creates a weak EventHandler wrapper
        /// </summary>
        /// <param name="eventHandler">EventHandler to create a weak wrapper for</param>
        /// <exception cref="ArgumentNullException"><paramref name="eventHandler"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="eventHandler"/> is a generated closure and may go out of scope prematurely.</exception>
        public WeakEventHandler(EventHandler<TEventArgs> eventHandler)
            : base(eventHandler)
        {
        }

        /// <summary>
        /// Creates a weak EventHandler wrapper
        /// </summary>
        /// <param name="eventHandler">EventHandler to create a weak wrapper for</param>
        /// <param name="unregisterMethod">Lambda expression for unregistration (i.e. "h => obj.Event -= h")</param>
        /// <exception cref="ArgumentNullException"><paramref name="eventHandler"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="eventHandler"/> is a generated closure and may go out of scope prematurely. -or- <paramref name="unregisterMethod"/> references the <paramref name="eventHandler" /> target.</exception>
        /// <remarks>
        /// The <paramref name="unregisterMethod"/> is held on to with a strong reference, so anything referenced in 
        /// the lambda expression will not be garbage collected until the <paramref name="eventHandler"/> target goes
        /// out of scope and the event is unregistered. It is recommended that you store the object containing the
        /// event in a local variable and use the local variable for unsubcribing to minimize object retention
        /// caused by property chains (i.e. "h => obj1.Property1.Property2.Event -= h" will hold on to obj1, which
        /// will presumably hold on to whatever is in Property1 and Property2. Furthermore, if Property1 or
        /// Property2 changes before the event is unsubscribed, the unsubscription will occur on the wrong 
        /// Property2 instance).
        /// </remarks>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "caller will be providing a lambda expression, which is expessed as a Action<>")]
        public WeakEventHandler(EventHandler<TEventArgs> eventHandler, Action<EventHandler<TEventArgs>> unregisterMethod)
            : base(eventHandler, unregisterMethod)
        {
        }

        /// <summary>
        /// Allows the WeakEventHandler&lt;T&gt; to be directly added to an event without suffixing '.Handler'.
        /// </summary>
        /// <param name="weakEventHandler">WeakEventHandler to add to event</param>
        /// <returns>Actual EventHandler&lt;T&gt; to add to event</returns>
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates",
            Justification = "Languages that don't support implicit operators can use the full '.Handler' syntax")]
        public static implicit operator EventHandler<TEventArgs>(WeakEventHandler<TEventArgs> weakEventHandler)
        {
            return weakEventHandler.Handler;
        }
    }

    /// <summary>
    /// Helper class for subscribing to an event in a way that doesn't cause
    /// the event listener to be held in memory after it should have gone out of scope.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix",
        Justification = "This is meant to be an extension of EventHandler, even though it doesn't inherit from it")]
    public class WeakEventHandler : WeakEventHandler<EventHandler, EventArgs>
    {
        /// <summary>
        /// Creates a weak EventHandler wrapper
        /// </summary>
        /// <param name="eventHandler">EventHandler to create a weak wrapper for</param>
        /// <exception cref="ArgumentNullException"><paramref name="eventHandler"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="eventHandler"/> is a generated closure and may go out of scope prematurely.</exception>
        public WeakEventHandler(EventHandler eventHandler)
            : base(eventHandler)
        {
        }

        /// <summary>
        /// Creates a weak EventHandler wrapper
        /// </summary>
        /// <param name="eventHandler">EventHandler to create a weak wrapper for</param>
        /// <param name="unregisterMethod">Lambda expression for unregistration (i.e. "h => obj.Event -= h")</param>
        /// <exception cref="ArgumentNullException"><paramref name="eventHandler"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="eventHandler"/> is a generated closure and may go out of scope prematurely. -or- <paramref name="unregisterMethod"/> references the <paramref name="eventHandler" /> target.</exception>
        /// <remarks>
        /// The <paramref name="unregisterMethod"/> is held on to with a strong reference, so anything referenced in 
        /// the lambda expression will not be garbage collected until the <paramref name="eventHandler"/> target goes
        /// out of scope and the event is unregistered. It is recommended that you store the object containing the
        /// event in a local variable and use the local variable for unsubcribing to minimize object retention
        /// caused by property chains (i.e. "h => obj1.Property1.Property2.Event -= h" will hold on to obj1, which
        /// will presumably hold on to whatever is in Property1 and Property2. Furthermore, if Property1 or
        /// Property2 changes before the event is unsubscribed, the unsubscription will occur on the wrong 
        /// Property2 instance).
        /// </remarks>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "caller will be providing a lambda expression, which is expessed as a Action<>")]
        public WeakEventHandler(EventHandler eventHandler, Action<EventHandler> unregisterMethod)
            : base(eventHandler, unregisterMethod)
        {
        }

        /// <summary>
        /// Allows the WeakEventHandler&lt;T&gt; to be directly added to an event without suffixing '.Handler'.
        /// </summary>
        /// <param name="weakEventHandler">WeakEventHandler to add to event</param>
        /// <returns>Actual EventHandler&lt;T&gt; to add to event</returns>
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates",
            Justification = "Languages that don't support implicit operators can use the full '.Handler' syntax")]
        public static implicit operator EventHandler(WeakEventHandler weakEventHandler)
        {
            return weakEventHandler.Handler;
        }
    }

    /// <summary>
    /// Extension methods for the EventHandler class
    /// </summary>
    public static class EventHandlerExtensions
    {
        /// <summary>
        /// Extension method for EventHandler&lt;T&gt;
        /// </summary>
        /// <typeparam name="T">Type of EventArgs associated with the event</typeparam>
        /// <param name="eventHandler">Source EventHandler to create a WeakEvent from</param>
        /// <param name="unregisterMethod">Delegate to call to unregister the EventHandler when the event handler goes out of scope</param>
        /// <exception cref="ArgumentException"><paramref name="eventHandler"/> references a static method.</exception>
        /// <returns>EventHandler to add to subscription</returns>
        /// <remarks>Usage: obj.Event += new EventHandler&lt;T&gt;(Callback).MakeWeak(h => obj.Event -= h);</remarks>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", 
            Justification="caller will be providing a lambda expression, which is expessed as a Action<>")]
        public static EventHandler<T> MakeWeak<T>(this EventHandler<T> eventHandler, Action<EventHandler<T>> unregisterMethod) 
            where T : EventArgs
        {
            if (eventHandler.Method.IsStatic || eventHandler.Target == null)
                throw new ArgumentException("Only instance methods are supported.", "eventHandler");

            return new WeakEventHandler<T>(eventHandler, unregisterMethod).Handler;
        }
    }
}
