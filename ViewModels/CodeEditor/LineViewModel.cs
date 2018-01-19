using Jamiras.DataModels;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;

namespace Jamiras.ViewModels.CodeEditor
{
    [DebuggerDisplay("{Text}")]
    public class LineViewModel : ViewModelBase
    {
        public LineViewModel(CodeEditorViewModel owner, int line)
        {
            _owner = owner;
            SetValueCore(LineProperty, line);
        }

        private readonly CodeEditorViewModel _owner;

        public static readonly ModelProperty LineProperty = ModelProperty.Register(typeof(LineViewModel), "Line", typeof(int), 1);
        public int Line
        {
            get { return (int)GetValue(LineProperty); }
            internal set { SetValue(LineProperty, value); }
        }

        private static readonly ModelProperty HighlightBeginProperty = ModelProperty.Register(typeof(LineViewModel), "HighlightBegin", typeof(int), 0);
        public int HighlightBegin
        {
            get { return (int)GetValue(HighlightBeginProperty); }
            private set { SetValue(HighlightBeginProperty, value); }
        }

        private static readonly ModelProperty HighlightEndProperty = ModelProperty.Register(typeof(LineViewModel), "HighlightEnd", typeof(int), 0);
        public int HighlightEnd
        {
            get { return (int)GetValue(HighlightEndProperty); }
            internal set { SetValue(HighlightEndProperty, value); }
        }

        private static readonly ModelProperty CursorColumnProperty = ModelProperty.Register(typeof(LineViewModel), "CursorColumn", typeof(int), 0);
        public int CursorColumn
        {
            get { return (int)GetValue(CursorColumnProperty); }
            internal set { SetValue(CursorColumnProperty, value); }
        }

        private static readonly ModelProperty CursorLocationProperty = 
            ModelProperty.RegisterDependant(typeof(LineViewModel), "CursorLocation", typeof(Thickness), new[] { CursorColumnProperty }, GetCursorLocation);
        public Thickness CursorLocation
        {
            get { return (Thickness)GetValue(CursorLocationProperty); }
        }
        private static object GetCursorLocation(ModelBase model)
        {
            var viewModel = (LineViewModel)model;
            var offset = (viewModel.CursorColumn - 1) * viewModel._owner.CharacterWidth;
            return new Thickness((int)offset, 0, 0, 0);
        }

        public EditorResources Resources
        {
            get { return _owner.Resources; }
        }

        public static readonly ModelProperty TextProperty = ModelProperty.Register(typeof(LineViewModel), "Text", typeof(string), string.Empty);
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly ModelProperty TextPiecesProperty = 
            ModelProperty.RegisterDependant(typeof(LineViewModel), "TextPieces", typeof(IEnumerable<TextPiece>), new[] { TextProperty }, GetTextPieces);
        public IEnumerable<TextPiece> TextPieces
        {
            get { return (IEnumerable<TextPiece>)GetValue(TextPiecesProperty); }
        }

        private static object GetTextPieces(ModelBase model)
        {
            var viewModel = (LineViewModel)model;

            var e = new LineChangedEventArgs(viewModel);
            viewModel._owner.OnLineChanged(e);

            var pieces = viewModel.TextPieces ?? new TextPiece[0];
            return e.ApplyColors(pieces);
        }
    }
}
