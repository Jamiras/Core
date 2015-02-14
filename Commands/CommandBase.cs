using System;
using System.Diagnostics;
using System.Windows.Input;

namespace Jamiras.Commands
{
    /// <summary>
    /// Base class for simple implementations of <see cref="ICommand"/>.
    /// </summary>
    public abstract class CommandBase : ICommand
    {
        /// <summary>
        /// Constructs a new <see cref="CommandBase"/>.
        /// </summary>
        protected CommandBase()
        {
            _canExecute = true;
        }

        #region ICommand Members

        /// <summary>
        /// Gets whether or not the command can be executed.
        /// </summary>
        /// <returns>True if the command can be executed, false if not.</returns>
        bool ICommand.CanExecute(object parameter)
        {
            return CanExecute;
        }

        /// <summary>
        /// Gets whether or not the command can be executed.
        /// </summary>
        /// <returns>True if the command can be executed, false if not.</returns>
        public bool CanExecute
        {
            get { return _canExecute; }
            protected set
            {
                if (_canExecute != value)
                {
                    _canExecute = value;
                    OnCanExecuteChanged(EventArgs.Empty);
                }
            }
        }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _canExecute;

        /// <summary>
        /// Raised when the CanExecute property changes.
        /// </summary>
        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// Called when the CanExecute property changes.
        /// </summary>
        protected virtual void OnCanExecuteChanged(EventArgs e)
        {
            var handler = CanExecuteChanged;
            if (handler != null)
                handler(this, e);
        }

        /// <summary>
        /// Executes the command.
        /// </summary>
        void ICommand.Execute(object parameter)
        {
            Execute();
        }

        /// <summary>
        /// Executes the command.
        /// </summary>
        public abstract void Execute();

        #endregion
    }

    /// <summary>
    /// Base class for implementations of <see cref="ICommand"/> that accept a parameter.
    /// </summary>
    /// <typeparam name="TParameter">The type of parameter passed to CanExecute and Execute.</typeparam>
    public abstract class CommandBase<TParameter> : ICommand
    {
        #region ICommand Members

        /// <summary>
        /// Determines whether or not the command can be executed for a given parameter.
        /// </summary>
        /// <param name="parameter">Parameter to evaluate.</param>
        /// <returns>True if the command can be executed, false if not.</returns>
        bool ICommand.CanExecute(object parameter)
        {
            if (!(parameter is TParameter))
            {
                if (parameter != null || default(TParameter) != null)
                    return false;
            }

            return CanExecute((TParameter)parameter);
        }

        /// <summary>
        /// Determines whether or not the command can be executed for a given parameter.
        /// </summary>
        /// <param name="parameter">Parameter to evaluate.</param>
        /// <returns>True if the command can be executed, false if not.</returns>
        public virtual bool CanExecute(TParameter parameter)
        {
            return true;
        }

        /// <summary>
        /// Raised when the CanExecute should be re-evaluated.
        /// </summary>
        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// Raises the CanExecuteChanged event.
        /// </summary>
        protected virtual void OnCanExecuteChanged(EventArgs e)
        {
            var handler = CanExecuteChanged;
            if (handler != null)
                handler(this, e);
        }

        /// <summary>
        /// Executes the command.
        /// </summary>
        void ICommand.Execute(object parameter)
        {
            Execute((TParameter)parameter);
        }

        /// <summary>
        /// Executes the command.
        /// </summary>
        public abstract void Execute(TParameter parameter);

        #endregion
    }
}
