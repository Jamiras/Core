using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Jamiras.ViewModels
{
    /// <summary>
    /// ViewModel for displaying the Windows TaskDialog.
    /// </summary>
    public sealed class TaskDialogViewModel : DialogViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TaskDialogViewModel"/> class.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="detail">Additional information to display.</param>
        public TaskDialogViewModel(string message, string detail)
        {
            Message = message;
            Detail = detail;
        }

        /// <summary>
        /// Gets or sets the message to display.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the additional detail message to display.
        /// </summary>
        public string Detail { get; set; }

        public enum Icon
        {
            None = 0,
            Info = -3, // TD_INFORMATION_ICON
            Warning = -1, // TD_WARNING_ICON
            Error = -2, // TD_ERROR_ICON
        }

        /// <summary>
        /// Specifies the icon to show in the dialog
        /// </summary>
        public void SetIcon(Icon icon)
        {
            _icon = icon;
        }
        private Icon _icon = Icon.None;

        public enum Buttons
        {
            OK = 1, // TDCBF_OK_BUTTON
            OKCancel = 9, // TDCBF_OK_BUTTON | TDCBF_CANCEL_BUTTON
            YesNo = 6, // TDCBF_YES_BUTTON | TDCBF_NO_BUTTON
            YesNoCancel = 14, // TDCBF_YES_BUTTON | TDCBF_NO_BUTTON | TDCBF_CANCEL_BUTTON
            RetryCancel = 24, // TDCBF_RETRY_BUTTON | TDCBF_CANCEL_BUTTON
        }

        /// <summary>
        /// Specifies the icon to show in the dialog
        /// </summary>
        public void SetButtons(Buttons buttons)
        {
            _buttons = buttons;
        }
        private Buttons _buttons = Buttons.OK;

        static private bool _isSupported = true;

        [DllImport("comctl32.dll", CharSet = CharSet.Unicode, EntryPoint = "TaskDialog")]
        static extern int TaskDialog(IntPtr hWndParent, IntPtr hInstance, string pszWindowTitle,
            string pszMainInstruction, string pszContent, int dwCommonButtons, IntPtr pszIcon,
            out int pnButton);

        private new bool ShowDialog()
        {
            if (_dialogService.HasDialogHandler(typeof(TaskDialogViewModel)))
            {
                _dialogService.ShowDialog(this);
                return true;
            }

            if (!_isSupported)
                return false;

            var ownerWindow = Application.Current.Windows.OfType<Window>().SingleOrDefault(w => w.IsActive);
            var hWndOwner = (ownerWindow != null) ? new WindowInteropHelper(ownerWindow).Handle : IntPtr.Zero;

            if (DialogTitle == null)
                DialogTitle = _dialogService.DefaultWindowTitle;

            int button;
            try
            {
                var result = TaskDialog(hWndOwner, IntPtr.Zero, DialogTitle, Message, Detail, (int)_buttons,
                    new IntPtr(UInt16.MaxValue + 1 + (short)_icon), out button);

                if (result != 0)
                {
                    DialogResult = DialogResult.None;
                    return false;
                }
            }
            catch (EntryPointNotFoundException)
            {
#if DEBUG
                MessageBox.Show("To use TaskDialogViewModel, your application must enable Common Controls v6.0 in it's app.manifest.");
#endif
                _isSupported = false;
                return false;
            }

            switch (button)
            {
                case 1: // IDOK
                    DialogResult = DialogResult.Ok;
                    break;
                case 2: // IDCANCEL
                    DialogResult = DialogResult.Cancel;
                    break;
                case 6: // IDYES
                    DialogResult = DialogResult.Yes;
                    break;
                case 7: // IDNO
                    DialogResult = DialogResult.No;
                    break;
                case 4: // IDRETRY
                    DialogResult = DialogResult.Retry;
                    break;
            }

            return true;
        }

        /// <summary>
        /// Shows the provided message in a simple dialog.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="detail">Additional information to display.</param>
        public static void ShowMessage(string message, string detail)
        {
            var vm = new TaskDialogViewModel(message, detail);

            if (!vm.ShowDialog())
                MessageBoxViewModel.ShowMessage(message + "\n\n" + detail);
        }

        /// <summary>
        /// Shows the provided message in a simple dialog with the specified buttons.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="detail">Additional information to display.</param>
        /// <param name="buttons">The buttons to display.</param>
        public static DialogResult ShowPrompt(string message, string detail, Buttons buttons = Buttons.YesNo)
        {
            var vm = new TaskDialogViewModel(message, detail);
            vm.SetButtons(buttons);

            if (!vm.ShowDialog())
            {
                var messageBoxViewModel = new MessageBoxViewModel(message + "\n\n" + detail);
                switch (buttons)
                {
                    case Buttons.YesNo: return messageBoxViewModel.ShowYesNoDialog();
                    case Buttons.YesNoCancel: return messageBoxViewModel.ShowYesNoCancelDialog();
                    case Buttons.RetryCancel: return messageBoxViewModel.ShowRetryCancelDialog();
                    default: throw new NotImplementedException(buttons.ToString());
                }
            }

            return vm.DialogResult;
        }

        /// <summary>
        /// Shows the provided message in a simple dialog with a warning icon.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="detail">Additional information to display.</param>
        public static void ShowWarningMessage(string message, string detail)
        {
            var vm = new TaskDialogViewModel(message, detail);
            vm.SetIcon(Icon.Warning);

            if (!vm.ShowDialog())
            {
                var messageBoxViewModel = new MessageBoxViewModel(message + "\n\n" + detail);
                messageBoxViewModel.DialogTitle = "Warning";
                messageBoxViewModel.ShowDialog();
            }
        }

        /// <summary>
        /// Shows the provided message in a simple dialog with a warning icon and the specified buttons.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="detail">Additional information to display.</param>
        /// <param name="buttons">The buttons to display.</param>
        public static DialogResult ShowWarningPrompt(string message, string detail, Buttons buttons = Buttons.YesNo)
        {
            var vm = new TaskDialogViewModel(message, detail);
            vm.SetIcon(Icon.Warning);
            vm.SetButtons(buttons);

            if (!vm.ShowDialog())
            {
                var messageBoxViewModel = new MessageBoxViewModel(message + "\n\n" + detail);
                messageBoxViewModel.DialogTitle = "Warning";
                switch (buttons)
                {
                    case Buttons.YesNo: return messageBoxViewModel.ShowYesNoDialog();
                    case Buttons.YesNoCancel: return messageBoxViewModel.ShowYesNoCancelDialog();
                    case Buttons.RetryCancel: return messageBoxViewModel.ShowRetryCancelDialog();
                    default: throw new NotImplementedException(buttons.ToString());
                }
            }

            return vm.DialogResult;
        }

        /// <summary>
        /// Shows the provided message in a simple dialog with an error icon.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="detail">Additional information to display.</param>
        public static void ShowErrorMessage(string message, string detail)
        {
            var vm = new TaskDialogViewModel(message, detail);
            vm.SetIcon(Icon.Error);

            if (!vm.ShowDialog())
            {
                var messageBoxViewModel = new MessageBoxViewModel(message + "\n\n" + detail);
                messageBoxViewModel.DialogTitle = "Error";
                messageBoxViewModel.ShowDialog();
            }
        }
    }
}
