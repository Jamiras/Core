using System.Diagnostics;
using System.Windows.Input;
using Jamiras.Commands;
using Jamiras.Components;
using Jamiras.DataModels;
using Jamiras.Services;

namespace Jamiras.ViewModels
{
    /// <summary>
    /// ViewModel for displaying simple messages.
    /// </summary>
    public class MessageBoxViewModel : DialogViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageBoxViewModel"/> class.
        /// </summary>
        /// <param name="message">The message to display.</param>
        public MessageBoxViewModel(string message)
            : this(message, ServiceRepository.Instance.FindService<IDialogService>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageBoxViewModel"/> class.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="dialogService">The <see cref="IDialogService"/> to use to display the dialog.</param>
        public MessageBoxViewModel(string message, IDialogService dialogService)
            : base(dialogService)
        {
            _message = message;
            CancelButtonText = null;
        }

        /// <summary>
        /// Gets or sets the message to display.
        /// </summary>
        public string Message
        {
            get { return _message; }
            set
            {
                if (_message != value)
                {
                    _message = value;
                    OnPropertyChanged(() => Message);
                }
            }
        }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _message;

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="ExtraButtonText"/>
        /// </summary>
        public static readonly ModelProperty ExtraButtonTextProperty =
            ModelProperty.Register(typeof(DialogViewModelBase), "ExtraButtonText", typeof(string), null);

        /// <summary>
        /// Gets or sets the extra button text.
        /// </summary>
        public string ExtraButtonText
        {
            get { return (string)GetValue(ExtraButtonTextProperty); }
            set { SetValue(ExtraButtonTextProperty, value); }
        }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="ExtraButtonCommand"/>
        /// </summary>
        public static readonly ModelProperty ExtraButtonCommandProperty =
            ModelProperty.Register(typeof(DialogViewModelBase), "ExtraButtonCommand", typeof(ICommand), null);

        /// <summary>
        /// Gets or sets the command associated to the extra button.
        /// </summary>
        public ICommand ExtraButtonCommand
        {
            get { return (ICommand)GetValue(ExtraButtonCommandProperty); }
            set { SetValue(ExtraButtonCommandProperty, value); }
        }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="NoButtonText"/>
        /// </summary>
        public static readonly ModelProperty NoButtonTextProperty =
            ModelProperty.Register(typeof(DialogViewModelBase), "NoButtonText", typeof(string), null);

        /// <summary>
        /// Gets or sets the No button text.
        /// </summary>
        public string NoButtonText
        {
            get { return (string)GetValue(NoButtonTextProperty); }
            set { SetValue(NoButtonTextProperty, value); }
        }

        /// <summary>
        /// Gets the command associated to the OK button.
        /// </summary>
        public CommandBase NoCommand
        {
            get { return new DelegateCommand(ExecuteNoCommand); }
        }

        /// <summary>
        /// Executes the ok command.
        /// </summary>
        private void ExecuteNoCommand()
        {
            DialogResult = DialogResult.No;
        }

        /// <summary>
        /// Executes the ok command.
        /// </summary>
        protected override void ExecuteOkCommand()
        {
            if (NoButtonText != null)
            {
                DialogResult = DialogResult.Yes;
                return;
            }

            base.ExecuteOkCommand();
        }

        /// <summary>
        /// Shows the provided message in a simple dialog.
        /// </summary>
        /// <param name="message">The message to display.</param>
        public static void ShowMessage(string message)
        {
            var vm = new MessageBoxViewModel(message);
            vm.ShowDialog();
        }

        /// <summary>
        /// Shows the provided message in a simple ok/cancel dialog.
        /// </summary>
        /// <returns><see cref="DialogResult.Ok"/> if the OK button was pressed, <see cref="DialogResult.Cancel"/> if not.</returns>
        public DialogResult ShowOkCancelDialog()
        {
            CancelButtonText = "Cancel";
            return ShowDialog();
        }

        /// <summary>
        /// Shows the provided message in a simple yes/no dialog.
        /// </summary>
        /// <returns><see cref="DialogResult.Yes"/> if the Yes button was pressed, <see cref="DialogResult.No"/> if not.</returns>
        public DialogResult ShowYesNoDialog()
        {
            OkButtonText = "Yes";
            NoButtonText = "No";
            return ShowDialog();
        }

        /// <summary>
        /// Shows the provided message in a simple yes/no/cancel dialog.
        /// </summary>
        /// <returns>
        /// <see cref="DialogResult.Yes"/> if the Yes button was pressed, 
        /// <see cref="DialogResult.No"/> if the No button was pressed, or
        /// <see cref="DialogResult.Cancel"/> if the Cancel button was pressed.
        /// </returns>
        public DialogResult ShowYesNoCancelDialog()
        {
            OkButtonText = "Yes";
            NoButtonText = "No";
            CancelButtonText = "Cancel";
            return ShowDialog();
        }
    }
}
