using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Jamiras.ViewModels
{
    public class MessageBoxViewModel : DialogViewModelBase
    {
        public MessageBoxViewModel(string message)
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
