using System;

namespace Jamiras.Core.Tests
{
    /// <summary>
    /// Helper class for testing that objects held on to by a WeakReference can be garbage collected. 
    /// </summary>
    /// <remarks>
    /// The WeakReferenceTester will hold a strong reference to the class until the Expire() method 
    /// is called. This prevents complications caused by premature garbage collection.
    /// </remarks>
    /// <example>
    /// <code>
    /// public void TestWeakReference()
    /// {
    ///     var weakTarget = new WeakReferenceTester&lt;TestClass&gt;(() =&gt; new TestClass());
    ///     var weakAction = new WeakAction&lt;int&gt;(weakTarget.Target.SetValue)
    ///     Assert.That(weakTarget.Expire(), Is.True, "Could not garbage collect target");
    ///     Assert.That(weakAction.Invoke(3), Is.False, "Invoke did not indicate target death");
    /// }
    /// </code>
    /// </example>
    public class WeakReferenceTester<T>
        where T : class
    {
        /// <summary>
        /// Constructs a new WeakReferenceTester object.
        /// </summary>
        /// <param name="createFunction">Function that creates the instance of <typeparamref name="T"/> to be tested for garbage collection.</param>
        /// <remarks>
        /// <paramref name="createFunction"/> should create a new instance. If you need to reference the new object, use
        /// the <see cref="Target"/> property of the WeakReferenceTester object. You should not store the tested object in
        /// a local variable within your test or <see cref="Expire"/> may fail simply because the object is still on the 
        /// stack. The WeakReferenceTester will hold a strong reference to the tested object until Expire is called to
        /// prevent any premature garbage collection of the tested object.
        /// </remarks>
        public WeakReferenceTester(Func<T> createFunction)
        {
            _strongReference = createFunction(); // keep strong reference to prevent premature garbage collection
            _reference = new WeakReference(_strongReference);
        }

        private readonly WeakReference _reference;
        private T _strongReference;

        /// <summary>
        /// Attempts to expire the referenced object.
        /// </summary>
        /// <returns>True if the referenced object was successfully garbage collected, false if not.</returns>
        public bool Expire()
        {
            _strongReference = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();

            return !IsAlive;
        }

        /// <summary>
        /// Gets the object (the target) referenced by the current WeakReferenceTester object.
        /// </summary>
        /// <remarks>May return null if the referenced object has been garbage collected.</remarks>
        public T Target 
        {
            get { return (T)_reference.Target; }
        }

        /// <summary>
        /// Gets an indication whether the object referenced by the current WeakReferenceTester object has been garbage collected.
        /// </summary>
        public bool IsAlive 
        {
            get { return _reference.IsAlive; }
        }
    }
}
