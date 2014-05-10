using System;
using Jamiras.Commands;
using Jamiras.Components;
using Jamiras.DataModels;
using Jamiras.Services;

namespace Jamiras.ViewModels
{
    public abstract class DialogViewModelBase : ValidatedViewModelBase
    {
        protected DialogViewModelBase()
            : this(ServiceRepository.Instance.FindService<IDialogService>())
        {
        }

        protected DialogViewModelBase(IDialogService dialogService)
        {
            _dialogService = dialogService;
        }

        private readonly IDialogService _dialogService;

        public static readonly ModelProperty DialogTitleProperty =
            ModelProperty.Register(typeof(DialogViewModelBase), "DialogTitle", typeof(string), null);

        /// <summary>
        /// Gets or sets the dialog caption.
        /// </summary>
        public string DialogTitle
        {
            get { return (string)GetValue(DialogTitleProperty); }
            set { SetValue(DialogTitleProperty, value); }
        }

        public static readonly ModelProperty DialogResultProperty =
            ModelProperty.Register(typeof(DialogViewModelBase), "DialogResult", typeof(DialogResult), DialogResult.None);

        /// <summary>
        /// Gets an indicator of how the dialog was closed.
        /// </summary>
        public DialogResult DialogResult
        {
            get { return (DialogResult)GetValue(DialogResultProperty); }
            protected set { SetValue(DialogResultProperty, value); }
        }

        public static readonly ModelProperty OkButtonTextProperty =
            ModelProperty.Register(typeof(DialogViewModelBase), "OkButtonText", typeof(string), "OK");

        /// <summary>
        /// Gets or sets the OK button text.
        /// </summary>
        public string OkButtonText
        {
            get { return (String)GetValue(OkButtonTextProperty); }
            set { SetValue(OkButtonTextProperty, value); }
        }

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
                DialogResult = DialogResult.Ok;
            }
            else
            {
                var viewModel = new MessageBoxViewModel(errors, _dialogService);
                viewModel.ShowDialog();
            }
        }

        public static readonly ModelProperty CancelButtonTextProperty =
            ModelProperty.Register(typeof(DialogViewModelBase), "CancelButtonText", typeof(string), "Cancel");

        /// <summary>
        /// Gets or sets the Cancel button text.
        /// </summary>
        public string CancelButtonText
        {
            get { return (string)GetValue(CancelButtonTextProperty); }
            set { SetValue(CancelButtonTextProperty, value); }
        }

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
        Ok,
        Cancel,
        Yes,
        No,
    }
}
