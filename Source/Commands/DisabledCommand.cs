using System;
using System.Windows.Input;

namespace Jamiras.Commands
{
    /// <summary>
    /// A <see cref="CommandBase"/> singleton that represents a permanently disabled command.
    /// </summary>
    public class DisabledCommand : CommandBase, ICommand
    {
        private DisabledCommand()
        {
            CanExecute = false;
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>
        /// The instance.
        /// </value>
        public static DisabledCommand Instance
        {
            get { return _instance ?? (_instance = new DisabledCommand()); }
        }
        private static DisabledCommand _instance;

        /// <summary>
        /// Raised when the CanExecute property changes.
        /// </summary>
        event EventHandler ICommand.CanExecuteChanged
        {
            add { }
            remove { }
        }

        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <exception cref="NotSupportedException">DisableCommand cannot be executed</exception>
        public override void Execute()
        {
            throw new NotSupportedException("DisableCommand cannot be executed");
        }
    }
}
