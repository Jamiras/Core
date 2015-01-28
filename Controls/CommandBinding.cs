using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Jamiras.Controls
{
    public class CommandBinding
    {
        public static readonly DependencyProperty KeyDownCommandProperty =
            DependencyProperty.RegisterAttached("KeyDownCommand", typeof(ICommand), typeof(CommandBinding),
                new FrameworkPropertyMetadata(OnKeyDownCommandChanged));

        public static ICommand GetKeyDownCommand(UIElement target)
        {
            return (ICommand)target.GetValue(KeyDownCommandProperty);
        }

        public static void SetKeyDownCommand(UIElement target, ICommand value)
        {
            target.SetValue(KeyDownCommandProperty, value);
        }

        private static void OnKeyDownCommandChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var textBox = (TextBoxBase)sender;
            if (e.NewValue != null)
            {
                if (e.OldValue == null)
                    textBox.KeyDown += OnKeyDown;
            }
            else
            {
                if (e.OldValue != null)
                    textBox.KeyDown -= OnKeyDown;
            }
        }

        private static void OnKeyDown(object sender, KeyEventArgs e)
        {
            var command = GetKeyDownCommand((UIElement)sender);
            if (command != null && command.CanExecute(e))
                command.Execute(e);
        }
    }
}
