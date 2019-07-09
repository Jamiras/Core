using System;
using Jamiras.Commands;
using Jamiras.Components;
using Jamiras.DataModels;
using Jamiras.Services;

namespace Jamiras.ViewModels
{
    /// <summary>
    /// Base class for ViewModels that are shown in dialogs.
    /// </summary>
    public abstract class DialogViewModelBase : ValidatedViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DialogViewModelBase"/> class.
        /// </summary>
        protected DialogViewModelBase()
            : this(ServiceRepository.Instance.FindService<IDialogService>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DialogViewModelBase"/> class.
        /// </summary>
        /// <param name="dialogService">The dialog service.</param>
        protected DialogViewModelBase(IDialogService dialogService)
        {
            _dialogService = dialogService;
        }

        private readonly IDialogService _dialogService;

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="DialogTitle"/>
        /// </summary>
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

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="DialogResult"/>
        /// </summary>
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

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="OkButtonText"/>
        /// </summary>
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

        /// <summary>
        /// Executes the ok command.
        /// </summary>
        protected virtual void ExecuteOkCommand()
        {
            string errors = Validate();
            if (String.IsNullOrEmpty(errors))
            {
                Commit();
                DialogResult = DialogResult.Ok;
            }
            else
            {
                var viewModel = new MessageBoxViewModel(errors, _dialogService);
                viewModel.DialogTitle = "Corrections required";
                viewModel.ShowDialog();
            }
        }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="CancelButtonText"/>
        /// </summary>
        public static readonly ModelProperty CancelButtonTextProperty =
            ModelProperty.Register(typeof(DialogViewModelBase), "CancelButtonText", typeof(string), "Cancel");

        /// <summary>
        /// Gets or sets the Cancel button text.
        /// </summary>
        /// <remarks>
        /// If set to <c>null</c>, the Cancel button will not be shown.
        /// </remarks>
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

        /// <summary>
        /// Executes the cancel command.
        /// </summary>
        protected virtual void ExecuteCancelCommand()
        {
            DialogResult = DialogResult.Cancel;
        }

        /// <summary>
        /// Gets whether the dialog has a close button.
        /// </summary>
        public bool CanClose
        {
            get { return (bool)GetValue(CanCloseProperty); }
            protected set { SetValue(CanCloseProperty, value); }
        }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="CanClose"/>
        /// </summary>
        public static readonly ModelProperty CanCloseProperty =
            ModelProperty.Register(typeof(DialogViewModelBase), "CanClose", typeof(bool), false);

        /// <summary>
        /// Gets whether the dialog can be resized.
        /// </summary>
        public bool CanResize
        {
            get { return (bool)GetValue(CanResizeProperty); }
            protected set { SetValue(CanResizeProperty, value); }
        }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="CanResize"/>
        /// </summary>
        public static readonly ModelProperty CanResizeProperty =
            ModelProperty.Register(typeof(DialogViewModelBase), "CanResize", typeof(bool), false);

        /// <summary>
        /// Gets or sets whether the window remembers it's size/location
        /// </summary>
        protected bool IsLocationRemembered
        {
            get { return (bool)GetValue(IsLocationRememberedProperty); }
            set { SetValue(IsLocationRememberedProperty, value); }
        }

        internal static readonly ModelProperty IsLocationRememberedProperty =
            ModelProperty.Register(typeof(DialogViewModelBase), null, typeof(bool), false);

        /// <summary>
        /// Shows the dialog for the view model.
        /// </summary>
        /// <returns>How the dialog was closed.</returns>
        public DialogResult ShowDialog()
        {
            return _dialogService.ShowDialog(this);
        }
    }

    /// <summary>
    /// The selected action that closed the dialog.
    /// </summary>
    public enum DialogResult
    {
        /// <summary>
        /// No action has been selected.
        /// </summary>
        None,

        /// <summary>
        /// The dialog closed normally.
        /// </summary>
        Ok,

        /// <summary>
        /// The dialog was cancelled.
        /// </summary>
        Cancel,

        /// <summary>
        /// The user selected Yes.
        /// </summary>
        Yes,

        /// <summary>
        /// The user selected No.
        /// </summary>
        No,

        /// <summary>
        /// The user selected Retry.
        /// </summary>
        Retry,
    }
}
