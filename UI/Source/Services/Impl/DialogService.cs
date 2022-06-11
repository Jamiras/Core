using Jamiras.Components;
using Jamiras.Services;
using Jamiras.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Jamiras.UI.Services.Impl
{
    [Export(typeof(IDialogService))]
    internal class DialogService : IDialogService
    {
        private readonly ILogger _logger = Logger.GetLogger("DialogService");

        public DialogService()
        {
            RegisterDialogHandler(typeof(MessageBoxViewModel), CreateMessageBoxView);
        }

        private FrameworkElement CreateMessageBoxView(DialogViewModelBase viewModel)
        {
            var textBlock = new System.Windows.Controls.TextBlock();
            textBlock.Margin = new Thickness(4);
            textBlock.SetBinding(System.Windows.Controls.TextBlock.TextProperty, "Message");
            textBlock.TextWrapping = TextWrapping.Wrap;
            return new Controls.OkCancelView(textBlock);
        }

        /// <summary>
        /// Gets or sets the main window of the application.
        /// </summary>
        public Window MainWindow
        {
            get { return _mainWindow; }
            set
            {
                if (_mainWindow != value)
                {
                    _mainWindow = value;

                    _dialogStack = new Stack<Window>();
                    _dialogStack.Push(_mainWindow);
                }
            }
        }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Window _mainWindow;

        private Stack<Window> _dialogStack;

        private Dictionary<Type, Func<DialogViewModelBase, FrameworkElement>> _createViewDelegates;

        public string DefaultWindowTitle
        {
            get
            {
                if (!string.IsNullOrEmpty(_defaultWindowTitle))
                    return _defaultWindowTitle;

                if (_mainWindow != null)
                    return _mainWindow.Title;

                return null;
            }
            set
            {
                _defaultWindowTitle = value;
            }
        }
        private string _defaultWindowTitle;

        /// <summary>
        /// Registers a callback that creates the View for a ViewModel.
        /// </summary>
        /// <param name="viewModelType">Type of ViewModel to create View for (must inherit from DialogViewModelBase)</param>
        /// <param name="createViewDelegate">Delegate that returns a View instance.</param>
        public void RegisterDialogHandler(Type viewModelType, Func<DialogViewModelBase, FrameworkElement> createViewDelegate)
        {
            if (!typeof(DialogViewModelBase).IsAssignableFrom(viewModelType))
                throw new ArgumentException(viewModelType.Name + " does not inherit from DialogViewModelBase", "viewModelType");

            if (_createViewDelegates == null)
                _createViewDelegates = new Dictionary<Type, Func<DialogViewModelBase, FrameworkElement>>();

            _createViewDelegates[viewModelType] = createViewDelegate;
        }

        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        /// <summary>
        /// Shows the dialog for the provided ViewModel.
        /// </summary>
        /// <param name="viewModel">ViewModel to show dialog for.</param>
        /// <returns>How the dialog was dismissed.</returns>
        public DialogResult ShowDialog(DialogViewModelBase viewModel)
        {
            if (_mainWindow == null)
                throw new InvalidOperationException("Cannot show dialog without setting MainWindow");

            _logger.Write("Showing {0} dialog: {1}", viewModel.GetType().Name, viewModel.DialogTitle);

            var createViewDelegate = GetHandler(viewModel.GetType());
            if (createViewDelegate == null)
                throw new ArgumentException("No view registered for " + viewModel.GetType().Name, "viewModel");
            var view = createViewDelegate(viewModel);
            if (view == null)
                throw new InvalidOperationException("Handler for " + viewModel.GetType().Name + " did not generate a view");

            Window window = new Window();
            window.Content = view;
            window.DataContext = viewModel;

            if (string.IsNullOrEmpty(viewModel.DialogTitle))
                viewModel.DialogTitle = DefaultWindowTitle;

            if (!viewModel.CanResize)
            {
                window.ResizeMode = ResizeMode.NoResize;
                window.SizeToContent = SizeToContent.WidthAndHeight;
            }
            else if ((bool)viewModel.GetValue(DialogViewModelBase.IsLocationRememberedProperty))
            {
                var windowSettingsRepository = ServiceRepository.Instance.FindService<IWindowSettingsRepository>();
                windowSettingsRepository.RestoreSettings(window);

                window.Closed += (o, e) => windowSettingsRepository.RememberSettings((Window)o);
            }

            window.SnapsToDevicePixels = true;
            window.ShowInTaskbar = viewModel.CanClose;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            window.SetBinding(Window.TitleProperty, "DialogTitle");

            if (viewModel.CanResize)
            {
                window.MaxHeight = SystemParameters.WorkArea.Height;
                window.MaxWidth = SystemParameters.WorkArea.Width;
            }

            EventHandler closeHandler = null;
            CancelEventHandler preventCloseHandler = (o, e) =>
            {
                e.Cancel = true;
            };

            PropertyChangedEventHandler propertyChangedHandler = (o, e) =>
            {
                if (e.PropertyName == "DialogResult" && viewModel.DialogResult != DialogResult.None)
                {
                    _logger.Write("Closing dialog ({0}): {1}", viewModel.DialogResult, viewModel.DialogTitle);

                    window.Closing -= preventCloseHandler;
                    window.Closed -= closeHandler;
                    window.Dispatcher.BeginInvoke(new Action(window.Close), null);
                }
            };

            closeHandler = (o, e) =>
            {
                if (viewModel.DialogResult == DialogResult.None)
                {
                    viewModel.PropertyChanged -= propertyChangedHandler;
                    viewModel.SetValue(DialogViewModelBase.DialogResultProperty, DialogResult.Cancel);
                }

                _logger.Write("Dialog closed ({0}): {1}", viewModel.DialogResult, viewModel.DialogTitle);
            };

            window.Loaded += (o, e) =>
            {
                if (!viewModel.CanClose && !viewModel.CanResize)
                {
                    var hwnd = new WindowInteropHelper(window).Handle;
                    SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
                }

                window.Dispatcher.BeginInvoke(new Action(() =>
                {
                    EnsureVisible(window);

                    _logger.WriteVerbose("Focusing first field");
                    view.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
                }));
            };

            viewModel.PropertyChanged += propertyChangedHandler;

            if (!viewModel.CanClose)
                window.Closing += preventCloseHandler;
            else
                window.Closed += closeHandler;

            window.Owner = _dialogStack.Peek();
            _dialogStack.Push(window);

            window.ShowDialog();

            _dialogStack.Pop();
            viewModel.PropertyChanged -= propertyChangedHandler;

            return viewModel.DialogResult;
        }

        public Window GetTopMostDialog()
        {
            if (_dialogStack.Count == 0)
                return null;

            return _dialogStack.Peek();
        }

        private static void EnsureVisible(Window window)
        {
            RECT winRect = new RECT((int)window.Left - 4, (int)window.Top - 4, (int)(window.Left + window.ActualWidth) + 8, (int)(window.Top + window.ActualHeight) + 8);
            IntPtr hMonitor = MonitorFromRect(ref winRect, MONITOR_DEFAULTTONULL);
            if (hMonitor != IntPtr.Zero)
            {
                var monitorInfo = new MONITORINFO();
                if (GetMonitorInfo(hMonitor, monitorInfo))
                {
                    if (winRect.top < monitorInfo.rcWork.top)
                        window.Top = monitorInfo.rcWork.top + 4;
                    else if (winRect.bottom > monitorInfo.rcWork.bottom)
                        window.Top = monitorInfo.rcWork.bottom - window.ActualHeight - 8;

                    if (winRect.left < monitorInfo.rcWork.left)
                        window.Left = monitorInfo.rcWork.left + 4;
                    else if (winRect.right > monitorInfo.rcWork.right)
                        window.Left = monitorInfo.rcWork.right - window.ActualWidth - 8;
                }
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromRect([In] ref RECT lprc, uint dwFlags);

        private const uint MONITOR_DEFAULTTONULL = 0;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public RECT(int x, int y, int cx, int cy)
            {
                left = x;
                top = y;
                right = cx;
                bottom = cy;
            }

            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, MONITORINFO lpmi);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MONITORINFO
        {
            public int cbSize = Marshal.SizeOf(typeof(MONITORINFO));
            public RECT rcMonitor = new RECT();
            public RECT rcWork = new RECT();
            public int dwFlags = 0;
        }

        private Func<DialogViewModelBase, FrameworkElement> GetHandler(Type type)
        {
            Func<DialogViewModelBase, FrameworkElement> createViewDelegate;
            if (_createViewDelegates.TryGetValue(type, out createViewDelegate))
                return createViewDelegate;

            type = type.BaseType;
            if (type != typeof(DialogViewModelBase))
                return GetHandler(type);

            return null;
        }

        /// <summary>
        /// Gets whether or not a handler is registered for the provided type.
        /// </summary>
        /// <param name="type">Type of ViewModel to query for (must inherit from DialogViewModelBase)</param>
        /// <returns><c>true</c> if a handler is registered, <c>false</c> if not.</returns>
        public bool HasDialogHandler(Type type)
        {
            return GetHandler(type) != null;
        }
    }
}
