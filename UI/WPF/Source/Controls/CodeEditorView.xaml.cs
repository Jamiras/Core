using Jamiras.DataModels;
using Jamiras.ViewModels.CodeEditor;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

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

            descriptor = DependencyPropertyDescriptor.FromProperty(ScrollViewer.HorizontalOffsetProperty, typeof(ScrollViewer));
            if (descriptor != null)
                descriptor.AddValueChanged(CodeLinesScrollViewer, OnHorizontalOffsetChanged);

            OnViewportHeightChanged(CodeLinesScrollViewer, EventArgs.Empty);
            RestoreScrollOffset();
            EnsureCursorVisible();

            if (ViewModel != null && ViewModel.IsCenterOnLineRequested)
                CenterOnLine();
        }

        private ScrollViewer CodeLinesScrollViewer
        {
            get { return (ScrollViewer)VisualTreeHelper.GetChild(codeEditorLines, 0); }
        }

        private ScrollViewer CodeLineNumbersScrollViewer
        {
            get { return (ScrollViewer)VisualTreeHelper.GetChild(codeEditorLineNumbers, 0); }
        }

        private void RestoreScrollOffset()
        {
            if (IsLoaded && ViewModel != null)
            {
                double scrollOffset = (double)ViewModel.GetValue(EditorScrollOffsetProperty);
                double scrollOffsetHorizontal = (double)ViewModel.GetValue(HorizontalScrollOffsetProperty);
                CodeLinesScrollViewer.ScrollToVerticalOffset(scrollOffset);
                CodeLinesScrollViewer.ScrollToHorizontalOffset(scrollOffsetHorizontal);
                CodeLineNumbersScrollViewer.ScrollToVerticalOffset(scrollOffset);

                UpdateVisibleLines(scrollOffset);
            }
        }

        private void UpdateVisibleLines(double scrollOffset)
        {
            int firstLine = Math.Max(0, (int)Math.Floor(scrollOffset));
            int lastLine = Math.Min(ViewModel.LineCount, firstLine + ViewModel.VisibleLines);
            for (int i = 0; i < firstLine; i++)
                ViewModel.Lines[i].IsVisible = false;
            for (int i = firstLine; i < lastLine; i++)
                ViewModel.Lines[i].IsVisible = true;
            for (int i = lastLine; i < ViewModel.LineCount; i++)
                ViewModel.Lines[i].IsVisible = false;
        }

        private void OnViewportHeightChanged(object sender, EventArgs e)
        {
            if (ViewModel != null)
            {
                int oldValue = ViewModel.VisibleLines;
                int newValue = (int)CodeLinesScrollViewer.ViewportHeight;
                ViewModel.VisibleLines = newValue;

                int firstLine = (int)Math.Floor((double)ViewModel.GetValue(EditorScrollOffsetProperty));
                oldValue = Math.Min(firstLine + oldValue, ViewModel.LineCount - 1);
                newValue = Math.Min(firstLine + newValue, ViewModel.LineCount);

                if (oldValue > newValue)
                {
                    for (int i = newValue + 1; i <= oldValue; i++)
                        ViewModel.Lines[i].IsVisible = false;
                }
                else
                {
                    for (int i = oldValue; i < newValue; i++)
                        ViewModel.Lines[i].IsVisible = true;
                }
            }
        }

        private static readonly ModelProperty EditorScrollOffsetProperty = ModelProperty.Register(typeof(CodeEditorView), null, typeof(double), 0.0);
        private static readonly ModelProperty HorizontalScrollOffsetProperty = ModelProperty.Register(typeof(CodeEditorView), null, typeof(double), 0.0);

        private void OnVerticalOffsetChanged(object sender, EventArgs e)
        {
            var offset = ((ScrollViewer)sender).VerticalOffset;

            if (ViewModel != null)
            {
                ViewModel.SetValueCore(EditorScrollOffsetProperty, offset);
                UpdateVisibleLines(offset);
            }

            CodeLineNumbersScrollViewer.ScrollToVerticalOffset(offset);
        }

        private void OnHorizontalOffsetChanged(object sender, EventArgs e)
        {
            if (ViewModel != null)
                ViewModel.SetValueCore(HorizontalScrollOffsetProperty, ((ScrollViewer)sender).HorizontalOffset);
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
                        _viewModel.RemovePropertyChangedHandler(CodeEditorViewModel.IsCenterOnLineRequestedProperty, OnIsCenterOnLineRequestedChanged);
                    }

                    _viewModel = value;

                    if (_viewModel != null)
                    {
                        _viewModel.AddPropertyChangedHandler(CodeEditorViewModel.CursorLineProperty, OnCursorLineChanged);
                        _viewModel.AddPropertyChangedHandler(CodeEditorViewModel.CursorColumnProperty, OnCursorColumnChanged);
                        _viewModel.AddPropertyChangedHandler(CodeEditorViewModel.IsToolWindowVisibleProperty, OnIsToolWindowVisibleChanged);
                        _viewModel.AddPropertyChangedHandler(CodeEditorViewModel.IsCenterOnLineRequestedProperty, OnIsCenterOnLineRequestedChanged);

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

        private void OnIsCenterOnLineRequestedChanged(object sender, ModelPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == true)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (IsLoaded)
                        CenterOnLine();
                }));
            }
        }

        private void CenterOnLine()
        {
            var scrollViewer = CodeLinesScrollViewer;
            var newOffset = (double)ViewModel.CursorLine - (int)(scrollViewer.ViewportHeight / 2);
            scrollViewer.ScrollToVerticalOffset(newOffset);

            ViewModel.IsCenterOnLineRequested = false;
        }

        private void EnsureCursorVisible()
        {
            // if center on line is requested, it takes precedence.
            if (ViewModel.IsCenterOnLineRequested)
                return;

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
            var hitTestResult = VisualTreeHelper.HitTest(this, point);
            if (hitTestResult == null)
                return null;

            var item = hitTestResult.VisualHit;

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

        private static int GetColumn(CodeEditorViewModel viewModel, LineViewModel line, Point mousePosition)
        {
            // mousePosition is relative to CodeEditorView.
            // adjust for the line number column (and its margin)
            var editorX = mousePosition.X - viewModel.LineNumberColumnWidth - 2;

            // adjust for the horizontal scroll offset
            editorX += (double)viewModel.GetValue(HorizontalScrollOffsetProperty);

            return viewModel.Resources.GetNearestCharacterIndex(line.Text, editorX);
        }

        private DateTime _doubleClickTime;
        private bool _lineSelectionMode = false;

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
                        if (moveCursorFlags == CodeEditorViewModel.MoveCursorFlags.None && position.X < ViewModel.LineNumberColumnWidth)
                        {
                            ViewModel.MoveCursorTo(line.Line, 1, moveCursorFlags);
                            ViewModel.MoveCursorTo(line.Line + 1, 1, CodeEditorViewModel.MoveCursorFlags.Highlighting);
                            _lineSelectionMode = true;
                        }
                        else
                        {
                            ViewModel.MoveCursorTo(line.Line, GetColumn(ViewModel, line, position), moveCursorFlags);
                            _lineSelectionMode = false;
                        }
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
                        ViewModel.HighlightWordAt(line.Line, GetColumn(ViewModel, line, position));
                        e.Handled = true;
                    }

                    _doubleClickTime = DateTime.UtcNow;
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
                    if (DateTime.UtcNow - _doubleClickTime > TimeSpan.FromSeconds(1)) // prevent trigger when mouse moves during double click
                    {
                        var position = e.GetPosition(this);
                        var line = GetLine(position);
                        if (line == null)
                        {
                            ViewModel.MoveCursorTo(ViewModel.LineCount, Int32.MaxValue, CodeEditorViewModel.MoveCursorFlags.Highlighting);
                        }
                        else if (_lineSelectionMode)
                        {
                            var selection = ViewModel.GetSelection();
                            if (line.Line < selection.Start.Line)
                            {
                                if (selection.Start.Column == 1)
                                {
                                    var startLine = ViewModel.Lines[selection.Start.Line - 1];
                                    ViewModel.MoveCursorTo(selection.Start.Line, startLine.LineLength + 1, CodeEditorViewModel.MoveCursorFlags.None);
                                }
                                ViewModel.MoveCursorTo(line.Line, 1, CodeEditorViewModel.MoveCursorFlags.Highlighting);
                            }
                            else
                            {
                                if (selection.Start.Column != 1)
                                    ViewModel.MoveCursorTo(selection.Start.Line, 1, CodeEditorViewModel.MoveCursorFlags.None);
                                ViewModel.MoveCursorTo(line.Line + 1, 1, CodeEditorViewModel.MoveCursorFlags.Highlighting);
                            }
                        }
                        else
                        {
                            ViewModel.MoveCursorTo(line.Line, GetColumn(ViewModel, line, position), CodeEditorViewModel.MoveCursorFlags.Highlighting);
                        }
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
