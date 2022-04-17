using System;
using System.Diagnostics;
using System.Reflection;

namespace Jamiras.Components
{
    /// <summary>
    /// Reconstructs an Action into a delegate that doesn't hold a strong reference to the target.
    /// </summary>
    /// <typeparam name="T">Method parameter type</typeparam>
    [DebuggerDisplay("{Method}")]
    public sealed class WeakAction<T> : WeakReference
    {
        private Action<object, T> _openAction;

        /// <summary>
        /// Gets the method represented by the WeakAction.
        /// </summary>
        public MethodInfo Method { get; private set; }

        /// <summary>
        /// Constructs a new WeakAction from an existing Action
        /// </summary>
        /// <param name="action">Action to convert into a WeakAction.</param>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="action"/> points at a generated closure.</exception>
        public WeakAction(Action<T> action)
            : base(action != null ? action.Target : null)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            Method = action.Method;

            if (Method.IsStatic)
            {
                Target = Method.DeclaringType;
                _openAction = (target, param) => action(param);
            }
            else
            {
                // Closures are generated internal classes used to pass local variables to an anonymous delegate. The lifetime of 
                // the closure is determined by the lifetime of the delegate. Attempting to create a weak reference to the delegate
                // will not keep the closure around without some external reference. Since we can't validate the presence of an
                // external reference, we err on the side of caution and throw an exception. Closures are not generated if the
                // anonymous delegate only references variables passed in to it, or the "this" psuedo-variable.
                if (OpenAction.HasClosureReference(action))
                    throw new ArgumentException("Cannot create weak reference from generated closure");
            }
        }

        /// <summary>
        /// Constructs a new WeakAction from an existing Action
        /// </summary>
        /// <param name="method">Method represented by the WeakAction.</param>
        /// <param name="target">The object instance containing the <paramref name="method"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="method"/> is null.</exception>
        public WeakAction(MethodInfo method, object target)
            : base(target)
        {
            if (method == null)
                throw new ArgumentNullException("method");

            Method = method;

            if (Method.IsStatic)
            {
                Target = Method.DeclaringType;
                _openAction = (t, param1) => method.Invoke(null, new object[] { param1 });
            }
        }

        /// <summary>
        /// Calls the method pointed with the provided parameter.
        /// </summary>
        /// <param name="param">Parameter to pass to method.</param>
        /// <returns>True if the method was called, false if the target has been garbage collected.</returns>
        public bool Invoke(T param)
        {
            object target = Target;
            if (target == null || !IsAlive)
                return false;

            if (_openAction == null)
                _openAction = OpenAction.CreateOpenAction<T>(Method);

            _openAction(target, param);
            return true;
        }
    }

    /// <summary>
    /// Reconstructs an Action into a delegate that doesn't hold a strong reference to the target.
    /// </summary>
    public sealed class WeakAction<T1, T2> : WeakReference
    {
        private Action<object, T1, T2> _openAction;

        /// <summary>
        /// Gets the method represented by the WeakAction.
        /// </summary>
        public MethodInfo Method { get; private set; }

        /// <summary>
        /// Constructs a new WeakAction from an existing Action
        /// </summary>
        /// <param name="action">Action to convert into a WeakAction.</param>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="action"/> points at a generated closure.</exception>
        public WeakAction(Action<T1, T2> action)
            : base(action != null ? action.Target : null)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            Method = action.Method;

            if (Method.IsStatic)
            {
                Target = Method.DeclaringType;
                _openAction = (target, param1, param2) => action(param1, param2);
            }
            else
            {
                // Closures are generated internal classes used to pass local variables to an anonymous delegate. The lifetime of 
                // the closure is determined by the lifetime of the delegate. Attempting to create a weak reference to the delegate
                // will not keep the closure around without some external reference. Since we can't validate the presence of an
                // external reference, we err on the side of caution and throw an exception. Closures are not generated if the
                // anonymous delegate only references variables passed in to it, or the "this" psuedo-variable.
                if (OpenAction.HasClosureReference(action))
                    throw new ArgumentException("Cannot create weak reference from generated closure");
            }
        }

        /// <summary>
        /// Constructs a new WeakAction from an existing Action
        /// </summary>
        /// <param name="method">Method represented by the WeakAction.</param>
        /// <param name="target">The object instance containing the <paramref name="method"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="method"/> is null.</exception>
        public WeakAction(MethodInfo method, object target)
            : base(target)
        {
            if (method == null)
                throw new ArgumentNullException("method");

            Method = method;

            if (Method.IsStatic)
            {
                Target = Method.DeclaringType;
                _openAction = (t, param1, param2) => method.Invoke(null, new object[] { param1, param2 });
            }
        }

        /// <summary>
        /// Calls the method pointed with the provided parameter.
        /// </summary>
        /// <param name="param1">Parameter to pass to method.</param>
        /// <param name="param2">Parameter to pass to method.</param>
        /// <returns>True if the method was called, false if the target has been garbage collected.</returns>
        public bool Invoke(T1 param1, T2 param2)
        {
            object target = Target;
            if (target == null || !IsAlive)
                return false;

            if (_openAction == null)
                _openAction = OpenAction.CreateOpenAction<T1, T2>(Method);

            _openAction(target, param1, param2);
            return true;
        }
    }
}
