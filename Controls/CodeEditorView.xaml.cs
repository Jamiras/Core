using Jamiras.DataModels;
using Jamiras.ViewModels.CodeEditor;
using System;
using System.ComponentModel;
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
            OnViewportHeightChanged(CodeLinesScrollViewer, EventArgs.Empty);
        }

        private void OnViewportHeightChanged(object sender, EventArgs e)
        {
            var editorViewModel = DataContext as CodeEditorViewModel;
            if (editorViewModel != null)
                editorViewModel.VisibleLines = (int)CodeLinesScrollViewer.ViewportHeight;
        }

        private void OnDataContextChanged(object sender, EventArgs e)
        {
            var editorViewModel = DataContext as CodeEditorViewModel;
            if (editorViewModel != null)
            {
                editorViewModel.AddPropertyChangedHandler(CodeEditorViewModel.CursorLineProperty, OnCursorLineChanged);
                editorViewModel.AddPropertyChangedHandler(CodeEditorViewModel.CursorColumnProperty, OnCursorColumnChanged);
            }
        }

        private ScrollViewer CodeLinesScrollViewer
        {
            get { return (ScrollViewer)VisualTreeHelper.GetChild(codeEditorLines, 0);  }
        }

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
            var scrollViewer = CodeLinesScrollViewer;
            var newOffset = ((CodeEditorViewModel)DataContext).CursorLine - 1;
            var firstVisibleOffset = scrollViewer.VerticalOffset;
            if (newOffset < firstVisibleOffset)
            {
                scrollViewer.ScrollToVerticalOffset(newOffset);
            }
            else 
            {
                var lastVisibleOffset = firstVisibleOffset + scrollViewer.ViewportHeight - 1;
                if (newOffset > lastVisibleOffset)
                    scrollViewer.ScrollToVerticalOffset(newOffset - scrollViewer.ViewportHeight + 1);
            }
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

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            var position = e.GetPosition(this);
            var line = GetLine(position);
            if (line != null)
            {
                var viewModel = (CodeEditorViewModel)DataContext;
                var moveCursorFlags = ((Keyboard.Modifiers & ModifierKeys.Shift) != 0) ? CodeEditorViewModel.MoveCursorFlags.Highlighting : CodeEditorViewModel.MoveCursorFlags.None;
                viewModel.MoveCursorTo(line.Line, GetColumn(viewModel, position), moveCursorFlags);
            }

            Focus();

            base.OnMouseLeftButtonDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var viewModel = (CodeEditorViewModel)DataContext;
                var position = e.GetPosition(this);

                var line = GetLine(position);
                if (line != null)
                    viewModel.MoveCursorTo(line.Line, GetColumn(viewModel, position), CodeEditorViewModel.MoveCursorFlags.Highlighting);
                else
                    viewModel.MoveCursorTo(viewModel.LineCount, Int32.MaxValue, CodeEditorViewModel.MoveCursorFlags.Highlighting);
            }

            base.OnMouseMove(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            var viewModel = (CodeEditorViewModel)DataContext;
            if (viewModel.HandleKey(e.Key, Keyboard.Modifiers))
                e.Handled = true;
            else
                base.OnKeyDown(e);
        }
    }
}
