using System;
using System.Windows.Input;

namespace Jamiras.Commands
{
    /// <summary>
    /// Class for creating an <see cref="ICommand"/> that calls a delegate.
    /// </summary>
    public class DelegateCommand : CommandBase
    {
        /// <summary>
        /// Constructs a new <see cref="DelegateCommand"/>.
        /// </summary>
        /// <param name="executeMethod">Delegate to call when the command is executed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="executeMethod"/> is null.</exception>
        public DelegateCommand(Action executeMethod)
        {
            if (executeMethod == null)
                throw new ArgumentNullException("executeMethod");

            _executeMethod = executeMethod;
        }

        private readonly Action _executeMethod;

        public override void Execute()
        {
            _executeMethod();
        }
    }

    /// <summary>
    /// Class for creating an <see cref="ICommand"/> that calls a delegate.
    /// </summary>
    public class DelegateCommand<T> : CommandBase<T>
    {
        /// <summary>
        /// Constructs a new <see cref="DelegateCommand"/>.
        /// </summary>
        /// <param name="executeMethod">Delegate to call when the command is executed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="executeMethod"/> is null.</exception>
        public DelegateCommand(Action<T> executeMethod)
            : this(executeMethod, null)
        {
        }

        /// <summary>
        /// Constructs a new <see cref="DelegateCommand"/>.
        /// </summary>
        /// <param name="executeMethod">Delegate to call when the command is executed.</param>
        /// <param name="canExecuteFunction">Delegate to call to determine if the command should be enabled.</param>
        /// <exception cref="ArgumentNullException"><paramref name="executeMethod"/> is null.</exception>
        public DelegateCommand(Action<T> executeMethod, Predicate<T> canExecuteFunction)
        {
            if (executeMethod == null)
                throw new ArgumentNullException("executeMethod");

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
            if (_canExecuteFunction != null)
                return _canExecuteFunction(parameter);

            return base.CanExecute(parameter);
        }
    }
}
