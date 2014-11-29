using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Jamiras.Components;
using Jamiras.ViewModels;

namespace Jamiras.Services
{
    [Export(typeof(IDialogService))]
    internal class DialogService : IDialogService
    {
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
            return new Jamiras.Controls.OkCancelView(textBlock);
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

            FrameworkElement view = GetView(viewModel, viewModel.GetType());
            if (view == null)
                throw new ArgumentException("No view registered for " + viewModel.GetType().Name, "viewModel");

            Window window = new Window();
            window.Content = view;
            window.DataContext = viewModel;

            if (String.IsNullOrEmpty(viewModel.DialogTitle))
                viewModel.DialogTitle = _mainWindow.Title;

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
                window.MaxHeight = System.Windows.SystemParameters.WorkArea.Height;
                window.MaxWidth = System.Windows.SystemParameters.WorkArea.Width;
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

        private FrameworkElement GetView(DialogViewModelBase viewModel, Type type)
        {
            Func<DialogViewModelBase, FrameworkElement> createViewDelegate;
            if (_createViewDelegates.TryGetValue(type, out createViewDelegate))
                return createViewDelegate(viewModel);

            type = type.BaseType;
            if (type != typeof(DialogViewModelBase))
                return GetView(viewModel, type);

            return null;
        }
    }
}
