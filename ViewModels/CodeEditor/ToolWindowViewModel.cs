using Jamiras.Commands;
using Jamiras.DataModels;
using System.Windows.Input;

namespace Jamiras.ViewModels.CodeEditor
{
    /// <summary>
    /// Base class for tool windows for a <see cref="CodeEditor"/>.
    /// </summary>
    /// <seealso cref="Jamiras.ViewModels.ViewModelBase" />
    public abstract class ToolWindowViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ToolWindowViewModel"/> class.
        /// </summary>
        /// <param name="owner">The editor that owns the tool window.</param>
        protected ToolWindowViewModel(CodeEditorViewModel owner)
        {
            Owner = owner;

            CloseCommand = new DelegateCommand(Close);
        }

        /// <summary>
        /// Gets a reference to the editor that owns the tool window.
        /// </summary>
        protected CodeEditorViewModel Owner { get; private set; }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="Caption"/>.
        /// </summary>
        public static readonly ModelProperty CaptionProperty = ModelProperty.Register(typeof(ToolWindowViewModel), "Caption", typeof(string), "");

        /// <summary>
        /// Gets or sets the caption of the tool window.
        /// </summary>
        public string Caption
        {
            get { return (string)GetValue(CaptionProperty); }
            protected set { SetValue(CaptionProperty, value); }
        }

        /// <summary>
        /// Gets the command to close the tool window.
        /// </summary>
        public CommandBase CloseCommand { get; private set; }

        /// <summary>
        /// Closes the tool window.
        /// </summary>
        public virtual void Close()
        {
            Owner.CloseToolWindow();
        }

        internal bool HandleKey(Key key, ModifierKeys modifiers)
        {
            var e = new KeyPressedEventArgs(key, modifiers);
            OnKeyPressed(e);
            return e.Handled;
        }

        /// <summary>
        /// Allows the tool window to process a key press before the editor if the tool window has focus.
        /// </summary>
        /// <param name="e">Information about which key was pressed.</param>
        protected virtual void OnKeyPressed(KeyPressedEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    Close();
                    e.Handled = true;
                    break;
            }
        }
    }
}
