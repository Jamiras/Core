using System;
using System.Windows.Input;

namespace Jamiras.Commands
{
    public class DisabledCommand : CommandBase, ICommand
    {
        private DisabledCommand()
        {
            CanExecute = false;
        }

        public static DisabledCommand Instance
        {
            get { return _instance ?? (_instance = new DisabledCommand()); }
        }
        private static DisabledCommand _instance;

        event EventHandler ICommand.CanExecuteChanged
        {
            add { }
            remove { }
        }

        public override void Execute()
        {
            throw new NotSupportedException("DisableCommand cannot be executed");
        }
    }
}
