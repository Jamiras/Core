using Jamiras.Components;
using Jamiras.DataModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;

namespace Jamiras.ViewModels.CodeEditor
{
    public class CodeEditorViewModel : ViewModelBase
    {
        public CodeEditorViewModel()
        {
            _lines = new ObservableCollection<LineViewModel>();

            Style = new EditorProperties();
            Resources = new EditorResources(Style);

            var formattedText = new FormattedText("0", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(Resources.FontName), Resources.FontSize, Brushes.Black); 
            SetValue(CharacterWidthProperty, formattedText.Width);
        }

        private static readonly ModelProperty CharacterWidthProperty = ModelProperty.Register(typeof(CodeEditorViewModel), null, typeof(double), 8.0);
        internal double CharacterWidth
        {
            get { return (double)GetValue(CharacterWidthProperty); }
        }

        public static readonly ModelProperty AreLineNumbersVisibleProperty = ModelProperty.Register(typeof(CodeEditorViewModel), "AreLineNumbersVisible", typeof(bool), true);
        public bool AreLineNumbersVisible
        {
            get { return (bool)GetValue(AreLineNumbersVisibleProperty); }
            set { SetValue(AreLineNumbersVisibleProperty, value); }
        }

        public static readonly ModelProperty CursorColumnProperty = ModelProperty.Register(typeof(CodeEditorViewModel), "CursorColumn", typeof(int), 1, OnCursorColumnChanged);
        public int CursorColumn
        {
            get { return (int)GetValue(CursorColumnProperty); }
            set
            {
                if (value < 1)
                {
                    value = 0;
                }
                else
                {
                    var lineViewModel = _lines[CursorLine - 1];
                    var lineLength = lineViewModel.Text.Length;
                    if (lineLength == 0)
                        value = 1;
                    else if (value > lineLength + 1)
                        value = lineLength + 1;
                }

                SetValue(CursorColumnProperty, value);
            }
        }

        private static void OnCursorColumnChanged(object sender, ModelPropertyChangedEventArgs e)
        {
            var viewModel = (CodeEditorViewModel)sender;

            var line = viewModel.CursorLine;
            if (line > 0 && line <= viewModel._lines.Count)
            {
                var lineViewModel = viewModel._lines[line - 1];

                var newColumn = (int)e.NewValue;
                if (newColumn < 1)
                {
                    newColumn = 0;
                }
                else 
                {
                    var lineLength = lineViewModel.Text.Length;
                    if (lineLength == 0)
                        newColumn = 1;
                    else if (newColumn > lineLength + 1)
                        newColumn = lineLength + 1;
                }

                lineViewModel.CursorColumn = newColumn;
            }
        }

        public static readonly ModelProperty CursorLineProperty = ModelProperty.Register(typeof(CodeEditorViewModel), "CursorLine", typeof(int), 1, OnCursorLineChanged);
        public int CursorLine
        {
            get { return (int)GetValue(CursorLineProperty); }
            set
            {
                if (value < 1)
                    value = 1;
                else if (value > LineCount)
                    value = LineCount;

                SetValue(CursorLineProperty, value);
            }
        }

        private static void OnCursorLineChanged(object sender, ModelPropertyChangedEventArgs e)
        {
            var viewModel = (CodeEditorViewModel)sender;

            int oldLine = (int)e.OldValue;
            if (oldLine > 0 && oldLine <= viewModel._lines.Count)
                viewModel._lines[oldLine - 1].CursorColumn = 0;

            int newLine = (int)e.NewValue;
            if (newLine > 0 && newLine <= viewModel._lines.Count)
            {
                var lineViewModel = viewModel._lines[(int)e.NewValue - 1];
                lineViewModel.CursorColumn = Math.Min(viewModel.CursorColumn, lineViewModel.Text.Length + 1);
            }
        }

        public string Content
        {
            get { return BuildContent(); }
            set { SetContent(value); }
        }

        private string BuildContent()
        {
            var builder = new StringBuilder();
            return builder.ToString();
        }

        private void SetContent(string value)
        {
            _lines.Clear();

            int lineIndex = 1;
            var tokenizer = Tokenizer.CreateTokenizer(value);
            do
            {
                var line = tokenizer.ReadTo('\n');
                if (line.Length > 0 && line[line.Length - 1] == '\r')
                    line = line.SubToken(0, line.Length - 1);
                tokenizer.Advance();

                var lineViewModel = new LineViewModel(this, lineIndex) { Text = line.ToString() };
                _lines.Add(lineViewModel);
                lineIndex++;
            } while (tokenizer.NextChar != '\0');

            LineCount = _lines.Count;
            CursorLine = 1;
            CursorColumn = 1;
        }

        public EventHandler<LineChangedEventArgs> LineChanged;
        internal void OnLineChanged(LineChangedEventArgs e)
        {
            if (LineChanged != null)
                LineChanged(this, e);
        }

        public static readonly ModelProperty LineCountProperty = ModelProperty.Register(typeof(CodeEditorViewModel), "LineCount", typeof(int), 1);
        public int LineCount
        {
            get { return (int)GetValue(LineCountProperty); }
            private set { SetValue(LineCountProperty, value); }
        }

        public IEnumerable<LineViewModel> Lines
        {
            get { return _lines; }
        }
        private ObservableCollection<LineViewModel> _lines;

        public EditorProperties Style { get; private set; }

        public EditorResources Resources { get; private set; }

        public static readonly ModelProperty LineNumberColumnWidthProperty =
            ModelProperty.RegisterDependant(typeof(CodeEditorViewModel), "LineNumberColumnWidth", typeof(double),
                new[] { LineCountProperty, CharacterWidthProperty }, GetLineNumberColumnWidth);
        public double LineNumberColumnWidth
        {
            get { return (double)GetValue(LineNumberColumnWidthProperty); }
        }
        private static object GetLineNumberColumnWidth(ModelBase model)
        {
            var viewModel = (CodeEditorViewModel)model;
            double characterWidth = viewModel.CharacterWidth;

            var lineCount = viewModel.LineCount;
            if (lineCount < 100)
                return characterWidth * 4;
            if (lineCount < 1000)
                return characterWidth * 5;
            if (lineCount < 10000)
                return characterWidth * 6;
            return characterWidth * 7;
        }

        internal static readonly ModelProperty VisibleLinesProperty = ModelProperty.Register(typeof(CodeEditorViewModel), "VisibleLines", typeof(int), 20);
        internal int VisibleLines
        {
            get { return (int)GetValue(VisibleLinesProperty); }
            set { SetValue(VisibleLinesProperty, value); }
        }

        internal bool HandleKey(Key key, ModifierKeys modifiers)
        {
            switch (key)
            {
                case Key.Down:
                    CursorLine++;
                    return true;

                case Key.Up:
                    CursorLine--;
                    return true;

                case Key.Left:
                    CursorColumn--;
                    return true;

                case Key.Right:
                    CursorColumn++;
                    return true;

                case Key.PageDown:
                    CursorLine += VisibleLines - 1;
                    return true;

                case Key.PageUp:
                    CursorLine -= VisibleLines - 1;
                    return true;

                case Key.Home:
                    if (modifiers == ModifierKeys.Control)
                        CursorLine = 1;
                    CursorColumn = 1;
                    return true;

                case Key.End:
                    if (modifiers == ModifierKeys.Control)
                        CursorLine = _lines.Count;
                    CursorColumn = _lines[CursorLine - 1].Text.Length + 1;
                    return true;

                default:
                    return false;
            }
        }
    }
}
