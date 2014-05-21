using System;
using System.Windows.Input;

namespace Jamiras.Commands
{
    public class DisabledCommand : ICommand
    {
        private DisabledCommand()
        {

        }

        public static DisabledCommand Instance
        {
            get { return _instance ?? (_instance = new DisabledCommand()); }
        }
        private static DisabledCommand _instance;

        #region ICommand Members

        public bool CanExecute(object parameter)
        {
            return false;
        }

        public event EventHandler CanExecuteChanged
        {
            add { }
            remove { }
        }

        public void Execute(object parameter)
        {
            throw new NotSupportedException("DisableCommand cannot be executed");
        }

        #endregion
    }
}
