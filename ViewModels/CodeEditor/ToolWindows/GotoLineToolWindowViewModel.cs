using Jamiras.DataModels;
using Jamiras.ViewModels.Fields;
using System;
using System.Windows.Input;

namespace Jamiras.ViewModels.CodeEditor.ToolWindows
{
    public class GotoLineToolWindowViewModel : ToolWindowViewModel
    {
        public GotoLineToolWindowViewModel(CodeEditorViewModel owner)
            : base(owner)
        {
            Caption = "Go to Line";

            LineNumber = new IntegerFieldViewModel("Line", 1, Int32.MaxValue);
        }

        public IntegerFieldViewModel LineNumber { get; private set; }

        public static readonly ModelProperty ShouldFocusLineNumberProperty = ModelProperty.Register(typeof(GotoLineToolWindowViewModel), "ShouldFocusLineNumber", typeof(bool), false);
        public bool ShouldFocusLineNumber
        {
            get { return (bool)GetValue(ShouldFocusLineNumberProperty); }
            set { SetValue(ShouldFocusLineNumberProperty, value); }
        }

        protected override void OnKeyPressed(KeyPressedEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    Jamiras.Controls.CommandBinding.ForceLostFocusBinding();
                    if (LineNumber.Value.GetValueOrDefault() >= 1)
                    {
                        Owner.MoveCursorTo(LineNumber.Value.GetValueOrDefault(), 1, CodeEditorViewModel.MoveCursorFlags.None);
                        Close();
                    }
                    e.Handled = true;
                    return;
            }
            base.OnKeyPressed(e);
        }
    }
}
