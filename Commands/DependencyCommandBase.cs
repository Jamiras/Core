using System;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Data;
using System.Diagnostics;

namespace Jamiras.Commands
{
    /// <summary>
    /// Base class for simple implementations of <see cref="ICommand"/>.
    /// </summary>
    /// <remarks>
    /// CanExecute will be re-evaluated any time a dependency property changes.
    /// </remarks>
    public abstract class DependencyCommandBase : ICommand
    {
        #region ICommand Members

        /// <summary>
        /// Gets whether or not the command can be executed.
        /// </summary>
        /// <returns>True if the command can be executed, false if not.</returns>
        bool ICommand.CanExecute(object parameter)
        {
            return CanExecute();
        }

        /// <summary>
        /// Gets whether or not the command can be executed.
        /// </summary>
        /// <returns>True if the command can be executed, false if not.</returns>
        public virtual bool CanExecute()
        {
            return true;
        }

        /// <summary>
        /// Raised when the CanExecute should be re-evaluated.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// Called when the CanExecute property changes.
        /// </summary>
        protected virtual void OnCanExecuteChanged(EventArgs e)
        {
            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// Executes the command.
        /// </summary>
        void ICommand.Execute(object parameter)
        {
            UpdateLastFocusedControl();
            Execute();
        }

        /// <summary>
        /// Executes the command.
        /// </summary>
        public abstract void Execute();

        #endregion

        internal static void UpdateLastFocusedControl()
        {
            TextBox textBox = Keyboard.FocusedElement as TextBox;
            if (textBox != null)
            {
                BindingExpression be = textBox.GetBindingExpression(TextBox.TextProperty);
                if (be != null)
                    be.UpdateSource();
            }
        }
    }

    /// <summary>
    /// Base class for implementations of <see cref="ICommand"/> that accept a parameter.
    /// </summary>
    /// <typeparam name="TParameter">The type of parameter passed to CanExecute and Execute.</typeparam>
    /// <remarks>
    /// CanExecute will be re-evaluated any time a bound property changes 
    /// (particularly useful when binding CommandParameters to the command)
    /// </remarks>
    public abstract class DependencyCommandBase<TParameter> : ICommand
    {
        #region ICommand Members

        /// <summary>
        /// Determines whether or not the command can be executed for a given parameter.
        /// </summary>
        /// <param name="parameter">Parameter to evaluate.</param>
        /// <returns>True if the command can be executed, false if not.</returns>
        bool ICommand.CanExecute(object parameter)
        {
            if (parameter == null && typeof(TParameter).IsValueType)
                return false;

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
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// Raises the CanExecuteChanged event.
        /// </summary>
        protected virtual void OnCanExecuteChanged(EventArgs e)
        {
            CommandManager.InvalidateRequerySuggested();
        }

        void ICommand.Execute(object parameter)
        {
            DependencyCommandBase.UpdateLastFocusedControl();
            Execute((TParameter)parameter);
        }

        /// <summary>
        /// Executes the command.
        /// </summary>
        public abstract void Execute(TParameter parameter);

        #endregion
    }
}
