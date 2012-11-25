using System.Diagnostics;
using Jamiras.Components;
using Jamiras.Services;

namespace Jamiras.ViewModels
{
    public class MessageBoxViewModel : DialogViewModelBase
    {
        public MessageBoxViewModel(string message)
            : this(message, ServiceRepository.Instance.FindService<IDialogService>())
        {
        }

        public MessageBoxViewModel(string message, IDialogService dialogService)
            : base(dialogService)
        {
            _message = message;
            IsCancelButtonVisible = false;
        }

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
    }
}
