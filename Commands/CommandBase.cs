using System;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Data;

namespace Jamiras.Commands
{
    public abstract class CommandBase : ICommand
    {
        #region ICommand Members

        bool ICommand.CanExecute(object parameter)
        {
            return CanExecute();
        }

        public virtual bool CanExecute()
        {
            return true;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        protected virtual void OnCanExecuteChanged(EventArgs e)
        {
            CommandManager.InvalidateRequerySuggested();
        }

        void ICommand.Execute(object parameter)
        {
            UpdateLastFocusedControl();
            Execute();
        }

        public abstract void Execute();

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

        #endregion
    }

    public abstract class CommandBase<TParameter> : ICommand
    {
        #region ICommand Members

        bool ICommand.CanExecute(object parameter)
        {
            if (parameter == null && typeof(TParameter).IsValueType)
                return false;

            return CanExecute((TParameter)parameter);
        }

        public virtual bool CanExecute(TParameter parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        protected virtual void OnCanExecuteChanged(EventArgs e)
        {
            CommandManager.InvalidateRequerySuggested();
        }

        void ICommand.Execute(object parameter)
        {
            CommandBase.UpdateLastFocusedControl();
            Execute((TParameter)parameter);
        }

        public abstract void Execute(TParameter parameter);

        #endregion
    }
}
