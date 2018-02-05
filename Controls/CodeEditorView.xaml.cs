using Jamiras.DataModels;
using Jamiras.ViewModels.CodeEditor;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Jamiras.Controls
{
    /// <summary>
    /// Interaction logic for CodeEditorView.xaml
    /// </summary>
    public partial class CodeEditorView : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CodeEditorView"/> class.
        /// </summary>
        public CodeEditorView()
        {
            InitializeComponent();

            var descriptor = DependencyPropertyDescriptor.FromProperty(FrameworkElement.DataContextProperty, typeof(CodeEditorView));
            if (descriptor != null)
                descriptor.AddValueChanged(this, OnDataContextChanged);

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var descriptor = DependencyPropertyDescriptor.FromProperty(ScrollViewer.ViewportHeightProperty, typeof(ScrollViewer));
            if (descriptor != null)
                descriptor.AddValueChanged(CodeLinesScrollViewer, OnViewportHeightChanged);

            descriptor = DependencyPropertyDescriptor.FromProperty(ScrollViewer.VerticalOffsetProperty, typeof(ScrollViewer));
            if (descriptor != null)
                descriptor.AddValueChanged(CodeLinesScrollViewer, OnVerticalOffsetChanged);

            OnViewportHeightChanged(CodeLinesScrollViewer, EventArgs.Empty);
            RestoreScrollOffset();
            EnsureCursorVisible();
        }

        private ScrollViewer CodeLinesScrollViewer
        {
            get { return (ScrollViewer)VisualTreeHelper.GetChild(codeEditorLines, 0); }
        }

        private void RestoreScrollOffset()
        {
            if (IsLoaded && ViewModel != null)
            {
                double scrollOffset = (double)ViewModel.GetValue(EditorScrollOffsetProperty);
                CodeLinesScrollViewer.ScrollToVerticalOffset(scrollOffset);
            }
        }

        private void OnViewportHeightChanged(object sender, EventArgs e)
        {
            if (ViewModel != null)
                ViewModel.VisibleLines = (int)CodeLinesScrollViewer.ViewportHeight;
        }

        private static readonly ModelProperty EditorScrollOffsetProperty = ModelProperty.Register(typeof(CodeEditorView), null, typeof(double), 0.0);

        private void OnVerticalOffsetChanged(object sender, EventArgs e)
        {
            if (ViewModel != null)
                ViewModel.SetValueCore(EditorScrollOffsetProperty, ((ScrollViewer)sender).VerticalOffset);
        }

        private void OnDataContextChanged(object sender, EventArgs e)
        {
            ViewModel = DataContext as CodeEditorViewModel;
        }

        private CodeEditorViewModel ViewModel
        {
            get { return _viewModel; }
            set
            {
                if (!ReferenceEquals(value, _viewModel))
                {
                    if (_viewModel != null)
                    {
                        _viewModel.RemovePropertyChangedHandler(CodeEditorViewModel.CursorLineProperty, OnCursorLineChanged);
                        _viewModel.RemovePropertyChangedHandler(CodeEditorViewModel.CursorColumnProperty, OnCursorColumnChanged);
                    }

                    _viewModel = value;

                    if (_viewModel != null)
                    {
                        _viewModel.AddPropertyChangedHandler(CodeEditorViewModel.CursorLineProperty, OnCursorLineChanged);
                        _viewModel.AddPropertyChangedHandler(CodeEditorViewModel.CursorColumnProperty, OnCursorColumnChanged);

                        RestoreScrollOffset();
                        EnsureCursorVisible();
                    }
                }
            }
        }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private CodeEditorViewModel _viewModel;

        private void OnCursorLineChanged(object sender, ModelPropertyChangedEventArgs e)
        {
            EnsureCursorVisible();
        }

        private void OnCursorColumnChanged(object sender, ModelPropertyChangedEventArgs e)
        {
            EnsureCursorVisible();
        }

        private void EnsureCursorVisible()
        {
            if (!IsLoaded)
                return;

            // do this asynchronously in case the cursor position is being updated rapidly
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var scrollViewer = CodeLinesScrollViewer;
                var newOffset = (double)(ViewModel.CursorLine - 1);
                var firstVisibleOffset = scrollViewer.VerticalOffset;
                if (newOffset >= firstVisibleOffset)
                {
                    var lastVisibleOffset = firstVisibleOffset + scrollViewer.ViewportHeight - 1;
                    if (newOffset < lastVisibleOffset)
                        return;

                    newOffset = newOffset - scrollViewer.ViewportHeight + 1;
                }

                scrollViewer.ScrollToVerticalOffset(newOffset);
            }));
        }

        private LineViewModel GetLine(Point point)
        {
            var item = VisualTreeHelper.HitTest(this, point).VisualHit;

            do
            {
                var frameworkElement = item as FrameworkElement;
                if (frameworkElement != null)
                {
                    var line = frameworkElement.DataContext as LineViewModel;
                    if (line != null)
                        return line;
                }

                item = VisualTreeHelper.GetParent(item);
            } while (item != null);

            return null;
        }

        private static int GetColumn(CodeEditorViewModel viewModel, Point mousePosition)
        {
            var characterWidth = viewModel.CharacterWidth;
            int column = (int)((mousePosition.X - 2 - viewModel.LineNumberColumnWidth + 1) / characterWidth) + 1;
            return column;
        }

        private DateTime doubleClickTime;

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1 && ViewModel != null)
            {
                var position = e.GetPosition(this);
                var line = GetLine(position);
                if (line != null)
                {
                    var moveCursorFlags = ((Keyboard.Modifiers & ModifierKeys.Shift) != 0) ? CodeEditorViewModel.MoveCursorFlags.Highlighting : CodeEditorViewModel.MoveCursorFlags.None;
                    ViewModel.MoveCursorTo(line.Line, GetColumn(ViewModel, position), moveCursorFlags);
                }

                Focus();
            }

            base.OnMouseLeftButtonDown(e);
        }

        protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
        {
            if (ViewModel != null)
            {
                var position = e.GetPosition(this);
                var line = GetLine(position);
                if (line != null)
                {
                    ViewModel.HighlightWordAt(line.Line, GetColumn(ViewModel, position));
                    e.Handled = true;
                }

                doubleClickTime = DateTime.UtcNow;
            }

            base.OnMouseDoubleClick(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && ViewModel != null)
            {
                if (!(e.OriginalSource is System.Windows.Controls.Primitives.Thumb)) // ignore when the user is dragging the scrollbar
                {
                    if (DateTime.UtcNow - doubleClickTime > TimeSpan.FromSeconds(1)) // prevent trigger when mouse moves during double click
                    {
                        var position = e.GetPosition(this);

                        var line = GetLine(position);
                        if (line != null)
                            ViewModel.MoveCursorTo(line.Line, GetColumn(ViewModel, position), CodeEditorViewModel.MoveCursorFlags.Highlighting);
                        else
                            ViewModel.MoveCursorTo(ViewModel.LineCount, Int32.MaxValue, CodeEditorViewModel.MoveCursorFlags.Highlighting);
                    }
                }
            }

            base.OnMouseMove(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (ViewModel != null && ViewModel.HandleKey(e.Key, Keyboard.Modifiers))
                e.Handled = true;
            else
                base.OnKeyDown(e);
        }
    }
}
