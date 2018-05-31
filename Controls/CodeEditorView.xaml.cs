using Jamiras.Components;
using Jamiras.DataModels;
using Jamiras.Services;
using Jamiras.ViewModels.CodeEditor;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
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
                        _viewModel.RemovePropertyChangedHandler(CodeEditorViewModel.IsToolWindowVisibleProperty, OnIsToolWindowVisibleChanged);
                    }

                    _viewModel = value;

                    if (_viewModel != null)
                    {
                        _viewModel.AddPropertyChangedHandler(CodeEditorViewModel.CursorLineProperty, OnCursorLineChanged);
                        _viewModel.AddPropertyChangedHandler(CodeEditorViewModel.CursorColumnProperty, OnCursorColumnChanged);
                        _viewModel.AddPropertyChangedHandler(CodeEditorViewModel.IsToolWindowVisibleProperty, OnIsToolWindowVisibleChanged);

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

        private void OnIsToolWindowVisibleChanged(object sender, ModelPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == false)
                Focus();
        }

        private void EnsureCursorVisible()
        {
            // do this asynchronously in case the cursor position is being updated rapidly
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!IsLoaded)
                    return;

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

        private bool IsCursorInToolWindow()
        {
            if (ViewModel == null || !ViewModel.IsToolWindowVisible)
                return false;

            var input = Keyboard.FocusedElement as FrameworkElement;
            if (input.DataContext != ViewModel)
                return true;

            return false;
        }

        private bool IsCursorInToolWindow(Point point)
        {
            if (ViewModel == null || !ViewModel.IsToolWindowVisible)
                return false;

            var item = VisualTreeHelper.HitTest(this, point).VisualHit;

            do
            {
                var frameworkElement = item as FrameworkElement;
                if (frameworkElement != null)
                {
                    if (frameworkElement.DataContext is ToolWindowViewModel)
                        return true;

                    if (frameworkElement.DataContext is CodeEditorViewModel)
                        return false;
                }

                item = VisualTreeHelper.GetParent(item);
            } while (item != null);

            return false;
        }

        private LineViewModel GetLineInternal(Point point)
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

        private LineViewModel GetLine(Point point)
        {
            var line = GetLineInternal(point);
            if (line == null)
            {
                // some of the area around the border between the line number and the content doesn't locate a LineViewModel
                // check a few pixels to the right in case we're in that zone.
                line = GetLineInternal(new Point(point.X + 10, point.Y));
            }

            return line;
        }

        private static int GetColumn(CodeEditorViewModel viewModel, Point mousePosition)
        {
            var characterWidth = viewModel.Resources.CharacterWidth;
            int column = (int)((mousePosition.X - 2 - viewModel.LineNumberColumnWidth + 1) / characterWidth) + 1;
            if (column < 1)
                column = 1;
            return column;
        }

        private DateTime doubleClickTime;

        /// <summary>
        /// Raises the <see cref="E:MouseLeftButtonDown" /> event.
        /// </summary>
        /// <param name="e">The <see cref="MouseButtonEventArgs"/> instance containing the event data.</param>
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1 && ViewModel != null)
            {
                var position = e.GetPosition(this);
                if (!IsCursorInToolWindow(position))
                {
                    var line = GetLine(position);
                    if (line != null)
                    {
                        var moveCursorFlags = ((Keyboard.Modifiers & ModifierKeys.Shift) != 0) ? CodeEditorViewModel.MoveCursorFlags.Highlighting : CodeEditorViewModel.MoveCursorFlags.None;
                        ViewModel.MoveCursorTo(line.Line, GetColumn(ViewModel, position), moveCursorFlags);
                    }

                    Focus();
                }
            }

            base.OnMouseLeftButtonDown(e);
        }

        /// <summary>
        /// Raises the <see cref="E:MouseDoubleClick" /> event.
        /// </summary>
        /// <param name="e">The <see cref="MouseButtonEventArgs"/> instance containing the event data.</param>
        protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
        {
            if (ViewModel != null)
            {
                var position = e.GetPosition(this);
                if (!IsCursorInToolWindow(position))
                {
                    var line = GetLine(position);
                    if (line != null)
                    {
                        ViewModel.HighlightWordAt(line.Line, GetColumn(ViewModel, position));
                        e.Handled = true;
                    }

                    doubleClickTime = DateTime.UtcNow;
                }
            }

            base.OnMouseDoubleClick(e);
        }

        /// <summary>
        /// Raises the <see cref="E:MouseMove" /> event.
        /// </summary>
        /// <param name="e">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && ViewModel != null && IsFocused)
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

        /// <summary>
        /// Raises the <see cref="E:KeyDown" /> event.
        /// </summary>
        /// <param name="e">The <see cref="KeyEventArgs"/> instance containing the event data.</param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (ViewModel != null)
            {
                if (IsCursorInToolWindow())
                {
                    if (ViewModel.ToolWindow.HandleKey(e.Key, Keyboard.Modifiers))
                    {
                        e.Handled = true;
                        return;
                    }
                }
                else
                {
                    if (ViewModel.HandleKey(e.Key, Keyboard.Modifiers))
                    {
                        e.Handled = true;
                        return;
                    }
                }
            }

            base.OnKeyDown(e);
        }
    }
}
