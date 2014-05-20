using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Jamiras.Components;
using Jamiras.ViewModels;
using System.Windows.Input;

namespace Jamiras.Services
{
    [Export(typeof(IDialogService))]
    internal class DialogService : IDialogService
    {
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

            window.SizeToContent = SizeToContent.WidthAndHeight;
            window.SnapsToDevicePixels = true;
            window.ResizeMode = ResizeMode.NoResize;
            window.ShowInTaskbar = false;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            window.SetBinding(Window.TitleProperty, "DialogTitle");

            CancelEventHandler preventCloseHandler = (o, e) =>
            {
                e.Cancel = true;
            };

            PropertyChangedEventHandler handler = (o, e) =>
            {
                if (e.PropertyName == "DialogResult" && viewModel.DialogResult != DialogResult.None)
                {
                    window.Closing -= preventCloseHandler;
                    window.Dispatcher.BeginInvoke(new Action(window.Close), null);
                }
            };

            window.Loaded += (o, e) =>
            {
                var hwnd = new WindowInteropHelper(window).Handle;
                SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);

                view.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            };

            viewModel.PropertyChanged += handler;
            window.Closing += preventCloseHandler;
            window.Owner = _dialogStack.Peek();
            _dialogStack.Push(window);

            window.ShowDialog();

            _dialogStack.Pop();
            viewModel.PropertyChanged -= handler;

            return viewModel.DialogResult;
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
