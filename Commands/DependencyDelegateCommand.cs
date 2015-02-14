using System;
using System.Windows.Input;

namespace Jamiras.Commands
{
    /// <summary>
    /// Class for creating an <see cref="ICommand"/> that calls a delegate.
    /// </summary>
    /// <remarks>
    /// CanExecute will be re-evaluated any time a dependency property changes.
    /// </remarks>
    public sealed class DependencyDelegateCommand : DependencyCommandBase
    {
        /// <summary>
        /// Constructs a new <see cref="DependencyDelegateCommand"/>.
        /// </summary>
        /// <param name="executeMethod">Delegate to call when the command is executed.</param>
        /// <param name="canExecuteFunction">Delegate to call to determine if the command should be enabled.</param>
        /// <exception cref="ArgumentNullException"><paramref name="executeMethod"/> or <paramref name="canExecuteFunction"/> is null.</exception>
        public DependencyDelegateCommand(Action executeMethod, Func<bool> canExecuteFunction)
        {
            if (executeMethod == null)
                throw new ArgumentNullException("executeMethod");
            if (canExecuteFunction == null)
                throw new ArgumentNullException("canExecuteFunction");

            _executeMethod = executeMethod;
            _canExecuteFunction = canExecuteFunction;
        }

        private readonly Action _executeMethod;
        private readonly Func<bool> _canExecuteFunction;

        /// <summary>
        /// Gets whether or not the command can be executed.
        /// </summary>
        /// <returns>True if the command can be executed, false if not.</returns>
        public override bool CanExecute()
        {
            return _canExecuteFunction();
        }

        /// <summary>
        /// Executes the command.
        /// </summary>
        public override void Execute()
        {
            _executeMethod();
        }
    }

    /// <summary>
    /// Class for creating an <see cref="ICommand"/> that calls a delegate.
    /// </summary>
    /// <remarks>
    /// CanExecute will be re-evaluated any time a dependency property changes.
    /// </remarks>
    public sealed class DependencyDelegateCommand<T> : DependencyCommandBase<T>
    {
        /// <summary>
        /// Constructs a new <see cref="DependencyDelegateCommand"/>.
        /// </summary>
        /// <param name="executeMethod">Delegate to call when the command is executed.</param>
        /// <param name="canExecuteFunction">Delegate to call to determine if the command should be enabled.</param>
        /// <exception cref="ArgumentNullException"><paramref name="executeMethod"/> or <paramref name="canExecuteFunction"/> is null.</exception>
        public DependencyDelegateCommand(Action<T> executeMethod, Predicate<T> canExecuteFunction)
        {
            if (executeMethod == null)
                throw new ArgumentNullException("executeMethod");
            if (canExecuteFunction == null)
                throw new ArgumentNullException("canExecuteFunction");

            _executeMethod = executeMethod;
            _canExecuteFunction = canExecuteFunction;
        }

        private readonly Action<T> _executeMethod;
        private readonly Predicate<T> _canExecuteFunction;

        /// <summary>
        /// Executes the command.
        /// </summary>
        public override void Execute(T parameter)
        {
            _executeMethod(parameter);
        }

        /// <summary>
        /// Determines whether or not the command can be executed for a given parameter.
        /// </summary>
        /// <param name="parameter">Parameter to evaluate.</param>
        /// <returns>True if the command can be executed, false if not.</returns>
        public override bool CanExecute(T parameter)
        {
            return _canExecuteFunction(parameter);
        }
    }
}
