using System;
using System.Reflection;

namespace Jamiras.Components
{
    /// <summary>
    /// Helper class for generating open action delegates. An open action allows the caller to provide the "this" parameter to a method on a class.
    /// </summary>
    internal static class OpenAction
    {
        internal interface IOpenActionFactory
        {
            /// <summary>
            /// Creates a static delegate from an Action, allowing the action's target to be provided at a later time.
            /// </summary>
            /// <param name="methodInfo">The method to create an OpenAction from.</param>
            /// <returns>An OpenAction not tied to any specific target.</returns>
            Action<object, TParam> CreateOpenAction<TParam>(MethodInfo methodInfo);

            /// <summary>
            /// Creates a static delegate from an Action, allowing the action's target to be provided at a later time.
            /// </summary>
            /// <param name="methodInfo">The method to create an OpenAction from.</param>
            /// <returns>An OpenAction not tied to any specific target.</returns>
            Action<object, TParam1, TParam2> CreateOpenAction<TParam1, TParam2>(MethodInfo methodInfo);
        }

        private static IOpenActionFactory _lastFactory;
        private static Type _lastFactoryTargetType;

        private static IOpenActionFactory CreateOpenActionFactory(Type targetType)
        {
            lock (typeof(OpenAction))
            {
                if (_lastFactoryTargetType != targetType)
                {
                    Type openActionFactoryType = typeof(OpenActionFactory<>).MakeGenericType(targetType);
                    _lastFactory = (IOpenActionFactory)Activator.CreateInstance(openActionFactoryType);
                    _lastFactoryTargetType = targetType;
                }

                return _lastFactory;
            }
        }

        /// <summary>
        /// Creates a static delegate from an Action, allowing the action's target to be provided at a later time.
        /// </summary>
        /// <param name="action">Action to construct an OpenAction from.</param>
        /// <returns>An OpenAction not ties to any specific target.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="action"/> is referencing a static method.</exception>
        public static Action<object, TParam> CreateOpenAction<TParam>(Action<TParam> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            return CreateOpenAction<TParam>(action.Method);
        }

        /// <summary>
        /// Creates a static delegate from an Action, allowing the action's target to be provided at a later time.
        /// </summary>
        /// <param name="methodInfo">The method to create an OpenAction from.</param>
        /// <returns>An OpenAction not ties to any specific target.</returns>
        /// <exception cref="ArgumentException"><paramref name="methodInfo"/> is referencing a static method.</exception>
        public static Action<object, TParam> CreateOpenAction<TParam>(MethodInfo methodInfo)
        {
            if (methodInfo.IsStatic)
                throw new ArgumentException("Cannot create open action from static method", "methodInfo");

            IOpenActionFactory factory = CreateOpenActionFactory(methodInfo.DeclaringType);
            return factory.CreateOpenAction<TParam>(methodInfo);
        }

        /// <summary>
        /// Creates a static delegate from an Action, allowing the action's target to be provided at a later time.
        /// </summary>
        /// <param name="methodInfo">The method to create an OpenAction from.</param>
        /// <returns>An OpenAction not ties to any specific target.</returns>
        /// <exception cref="ArgumentException"><paramref name="methodInfo"/> is referencing a static method.</exception>
        public static Action<object, TParam1, TParam2> CreateOpenAction<TParam1, TParam2>(MethodInfo methodInfo)
        {
            if (methodInfo.IsStatic)
                throw new ArgumentException("Cannot create open action from static method", "methodInfo");

            IOpenActionFactory factory = CreateOpenActionFactory(methodInfo.DeclaringType);
            return factory.CreateOpenAction<TParam1, TParam2>(methodInfo);
        }

        /// <summary>
        /// Determines whether a delegate is pointing at a generated closure.
        /// </summary>
        /// <param name="d">The delegate to test.</param>
        /// <returns>True if the delegate is pointing at a generated closure, false if not.</returns>
        public static bool HasClosureReference(Delegate d)
        {
            var target = d.Target;
            if (target == null)
                return false;

            Type targetType = target.GetType();
            return (targetType.IsNestedPrivate && targetType.Name.StartsWith("<>c__DisplayClass", StringComparison.Ordinal));
        }
    }

    /// <summary>
    /// Helper class for generating open action delegates
    /// </summary>
    /// <typeparam name="TTarget">The action's target type.</typeparam>
    internal class OpenActionFactory<TTarget> : OpenAction.IOpenActionFactory
        where TTarget : class
    {
        private Delegate _lastDelegate;
        private MethodInfo _lastMethod;

        /// <summary>
        /// Creates a static delegate from an Action, allowing the action's target to be provided at a later time.
        /// </summary>
        /// <param name="methodInfo">The method to create an OpenAction from.</param>
        /// <returns>An OpenAction not ties to any specific target.</returns>
        public Action<object, TParam> CreateOpenAction<TParam>(MethodInfo methodInfo)
        {
            Action<object, TParam> handler = null;

            if (_lastMethod == methodInfo)
                handler = _lastDelegate as Action<object, TParam>;

            if (handler == null)
            {
                var helper = new OpenAction<TTarget, TParam>(methodInfo);
                handler = helper.Dispatch;

                _lastMethod = methodInfo;
                _lastDelegate = handler;
            }

            return handler;
        }

        /// <summary>
        /// Creates a static delegate from an Action, allowing the action's target to be provided at a later time.
        /// </summary>
        /// <param name="methodInfo">The method to create an OpenAction from.</param>
        /// <returns>An OpenAction not ties to any specific target.</returns>
        public Action<object, TParam1, TParam2> CreateOpenAction<TParam1, TParam2>(MethodInfo methodInfo)
        {
            Action<object, TParam1, TParam2> handler = null;

            if (_lastMethod == methodInfo)
                handler = _lastDelegate as Action<object, TParam1, TParam2>;

            if (handler == null)
            {
                var helper = new OpenAction<TTarget, TParam1, TParam2>(methodInfo);
                handler = helper.Dispatch;

                _lastMethod = methodInfo;
                _lastDelegate = handler;
            }

            return handler;
        }
    }

    /// <summary>
    /// An OpenAction instance.
    /// </summary>
    internal class OpenAction<TTarget, TParam>
    {
        public OpenAction(MethodInfo methodInfo)
        {
            _methodInfo = methodInfo;
        }

        private readonly MethodInfo _methodInfo;
        private Action<TTarget, TParam> _delegate;

        public void Dispatch(object target, TParam param)
        {
            if (_delegate == null)
                _delegate = (Action<TTarget, TParam>)Delegate.CreateDelegate(typeof(Action<TTarget, TParam>), _methodInfo);

            _delegate((TTarget)target, param);
        }
    }

    /// <summary>
    /// An OpenAction instance.
    /// </summary>
    internal class OpenAction<TTarget, TParam1, TParam2>
    {
        public OpenAction(MethodInfo methodInfo)
        {
            _methodInfo = methodInfo;
        }

        private readonly MethodInfo _methodInfo;
        private Action<TTarget, TParam1, TParam2> _delegate;

        public void Dispatch(object target, TParam1 param1, TParam2 param2)
        {
            if (_delegate == null)
                _delegate = (Action<TTarget, TParam1, TParam2>)Delegate.CreateDelegate(typeof(Action<TTarget, TParam1, TParam2>), _methodInfo);

            _delegate((TTarget)target, param1, param2);
        }
    }
}