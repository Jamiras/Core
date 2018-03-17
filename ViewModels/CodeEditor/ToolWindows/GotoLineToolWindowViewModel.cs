using Jamiras.DataModels;
using Jamiras.ViewModels.Fields;
using System;
using System.Windows.Input;

namespace Jamiras.ViewModels.CodeEditor.ToolWindows
{
    /// <summary>
    /// Defines a tool window for jumping to a specified line in the editor.
    /// </summary>
    /// <seealso cref="Jamiras.ViewModels.CodeEditor.ToolWindowViewModel" />
    public class GotoLineToolWindowViewModel : ToolWindowViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GotoLineToolWindowViewModel"/> class.
        /// </summary>
        /// <param name="owner">The editor that owns the tool window.</param>
        public GotoLineToolWindowViewModel(CodeEditorViewModel owner)
            : base(owner)
        {
            Caption = "Go to Line";

            LineNumber = new IntegerFieldViewModel("Line", 1, Int32.MaxValue);
        }

        /// <summary>
        /// Gets the view model for the line number field.
        /// </summary>
        public IntegerFieldViewModel LineNumber { get; private set; }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="ShouldFocusLineNumber"/>
        /// </summary>
        public static readonly ModelProperty ShouldFocusLineNumberProperty = ModelProperty.Register(typeof(GotoLineToolWindowViewModel), "ShouldFocusLineNumber", typeof(bool), false);

        /// <summary>
        /// Bindable property for causing the line number field to be focused.
        /// </summary>
        /// <remarks>
        /// Set to <c>true</c> to cause the line number field to be focused.
        /// </remarks>
        public bool ShouldFocusLineNumber
        {
            get { return (bool)GetValue(ShouldFocusLineNumberProperty); }
            set { SetValue(ShouldFocusLineNumberProperty, value); }
        }

        /// <summary>
        /// Allows the tool window to process a key press before the editor if the tool window has focus.
        /// </summary>
        /// <param name="e">Information about which key was pressed.</param>
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
