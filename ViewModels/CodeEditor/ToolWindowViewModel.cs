using Jamiras.Commands;
using Jamiras.DataModels;
using System.Windows.Input;

namespace Jamiras.ViewModels.CodeEditor
{
    public abstract class ToolWindowViewModel : ViewModelBase
    {
        protected ToolWindowViewModel(CodeEditorViewModel owner)
        {
            Owner = owner;

            CloseCommand = new DelegateCommand(Close);
        }

        protected CodeEditorViewModel Owner { get; private set; }

        public static readonly ModelProperty CaptionProperty = ModelProperty.Register(typeof(ToolWindowViewModel), "Caption", typeof(string), "");
        public string Caption
        {
            get { return (string)GetValue(CaptionProperty); }
            protected set { SetValue(CaptionProperty, value); }
        }

        public CommandBase CloseCommand { get; private set; }
        public void Close()
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
        /// Raises the <see cref="E:KeyPressed" /> event.
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
