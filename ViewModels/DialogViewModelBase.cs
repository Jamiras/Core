using System;
using System.Diagnostics;
using Jamiras.Commands;
using Jamiras.Components;
using Jamiras.Services;

namespace Jamiras.ViewModels
{
    public abstract class DialogViewModelBase : ViewModelBase
    {
        protected DialogViewModelBase()
            : this(ServiceRepository.Instance.FindService<IDialogService>())
        {
        }

        protected DialogViewModelBase(IDialogService dialogService)
        {
            _dialogService = dialogService;

            _okButtonText = "OK";
            _isCancelButtonVisible = true;
        }

        private readonly IDialogService _dialogService;

        /// <summary>
        /// Gets or sets the dialog caption.
        /// </summary>
        public string DialogTitle
        {
            get { return _dialogTitle; }
            set
            {
                if (_dialogTitle != value)
                {
                    _dialogTitle = value;
                    OnPropertyChanged(() => DialogTitle);
                }
            }
        }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _dialogTitle;

        /// <summary>
        /// Gets an indicator of how the dialog was closed.
        /// </summary>
        public DialogResult DialogResult
        {
            get { return _dialogResult; }
            protected set
            {
                if (_dialogResult != value)
                {
                    _dialogResult = value;
                    OnPropertyChanged(() => DialogResult);
                }
            }
        }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DialogResult _dialogResult;

        /// <summary>
        /// Gets or sets the OK button text.
        /// </summary>
        public string OkButtonText
        {
            get { return _okButtonText; }
            set
            {
                if (_okButtonText != value)
                {
                    _okButtonText = value;
                    OnPropertyChanged(() => OkButtonText);
                }
            }
        }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _okButtonText;

        /// <summary>
        /// Gets the command associated to the OK button.
        /// </summary>
        public CommandBase OkCommand
        {
            get { return new DelegateCommand(ExecuteOkCommand); }
        }

        protected virtual void ExecuteOkCommand()
        {
            string errors = Validate();
            if (String.IsNullOrEmpty(errors))
            {
                DialogResult = DialogResult.OK;
            }
            else
            {
                var viewModel = new MessageBoxViewModel(errors, _dialogService);
                viewModel.ShowDialog();
            }
        }

        /// <summary>
        /// Gets or sets whether the Cancel button should be shown.
        /// </summary>
        public bool IsCancelButtonVisible
        {
            get { return _isCancelButtonVisible; }
            set
            {
                if (_isCancelButtonVisible != value)
                {
                    _isCancelButtonVisible = value;
                    OnPropertyChanged(() => IsCancelButtonVisible);
                }
            }
        }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _isCancelButtonVisible;

        /// <summary>
        /// Gets the command associated to the Cancel button.
        /// </summary>
        public CommandBase CancelCommand
        {
            get { return new DelegateCommand(ExecuteCancelCommand); }
        }

        protected virtual void ExecuteCancelCommand()
        {
            DialogResult = DialogResult.Cancel;
        }

        /// <summary>
        /// Shows the dialog for the view model.
        /// </summary>
        /// <returns>How the dialog was closed.</returns>
        public DialogResult ShowDialog()
        {
            return _dialogService.ShowDialog(this);
        }
    }

    public enum DialogResult
    {
        None,
        OK,
        Cancel,
        Yes,
        No,
    }
}
