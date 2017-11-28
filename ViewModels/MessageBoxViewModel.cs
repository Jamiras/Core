using System.Diagnostics;
using Jamiras.Components;
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
        /// Shows the provided message in a simple dialog.
        /// </summary>
        /// <param name="message">The message to display.</param>
        public static void ShowMessage(string message)
        {
            var vm = new MessageBoxViewModel(message);
            vm.ShowDialog();
        }
    }
}
