using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Controls;

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
            var element = (UIElement)sender;
            if (e.NewValue != null)
            {
                if (e.OldValue == null)
                    element.KeyDown += OnKeyDown;
            }
            else
            {
                if (e.OldValue != null)
                    element.KeyDown -= OnKeyDown;
            }
        }

        private static void OnKeyDown(object sender, KeyEventArgs e)
        {
            var command = GetKeyDownCommand((UIElement)sender);
            if (command != null && command.CanExecute(e))
                command.Execute(e);
        }

        public static readonly DependencyProperty ClickCommandProperty =
            DependencyProperty.RegisterAttached("ClickCommand", typeof(ICommand), typeof(CommandBinding),
                new FrameworkPropertyMetadata(OnClickCommandChanged));

        public static ICommand GetClickCommand(UIElement target)
        {
            return (ICommand)target.GetValue(ClickCommandProperty);
        }

        public static void SetClickCommand(UIElement target, ICommand value)
        {
            target.SetValue(ClickCommandProperty, value);
        }

        private static void OnClickCommandChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var element = (UIElement)sender;
            if (e.NewValue != null)
            {
                if (e.OldValue == null)
                    element.MouseDown += OnMouseDown;
            }
            else
            {
                if (e.OldValue != null)
                    element.MouseDown -= OnMouseDown;
            }
        }

        private static void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            var command = GetClickCommand((UIElement)sender);
            if (command != null && command.CanExecute(e))
                command.Execute(e);
        }

        public static readonly DependencyProperty DoubleClickCommandProperty =
            DependencyProperty.RegisterAttached("DoubleClickCommand", typeof(ICommand), typeof(CommandBinding),
        new FrameworkPropertyMetadata(OnDoubleClickCommandChanged));

        public static ICommand GetDoubleClickCommand(UIElement target)
        {
            return (ICommand)target.GetValue(DoubleClickCommandProperty);
        }

        public static void SetDoubleClickCommand(UIElement target, ICommand value)
        {
            target.SetValue(DoubleClickCommandProperty, value);
        }

        private static void OnDoubleClickCommandChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var element = (UIElement)sender;
            if (e.NewValue != null)
            {
                element.InputBindings.Add(new InputBinding((ICommand)e.NewValue, new MouseGesture(MouseAction.LeftDoubleClick)));
            }
            else
            {
                var binding = element.InputBindings.OfType<InputBinding>().FirstOrDefault(b =>
                {
                    var gesture = b.Gesture as MouseGesture;
                    return (gesture != null && gesture.MouseAction == MouseAction.LeftDoubleClick);
                });

                if (binding != null)
                    element.InputBindings.Remove(binding);
            }
        }

        private static void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var command = GetClickCommand((UIElement)sender);
            if (command != null && command.CanExecute(e))
                command.Execute(e);
        }

        public static readonly DependencyProperty FocusIfTrueProperty =
            DependencyProperty.RegisterAttached("FocusIfTrue", typeof(bool), typeof(CommandBinding),
                new FrameworkPropertyMetadata(OnFocusIfTrueChanged));

        public static bool GetFocusIfTrue(UIElement target)
        {
            return (bool)target.GetValue(FocusIfTrueProperty);
        }

        public static void SetFocusIfTrue(UIElement target, bool value)
        {
            target.SetValue(FocusIfTrueProperty, value);
        }

        private static void OnFocusIfTrueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                ((IInputElement)sender).Focus();
                SetFocusIfTrue((UIElement)sender, false);
            }
        }

        public static readonly DependencyProperty LostFocusCommandProperty =
            DependencyProperty.RegisterAttached("LostFocusCommand", typeof(ICommand), typeof(CommandBinding),
                new FrameworkPropertyMetadata(OnLostFocusCommandChanged));

        public static ICommand GetLostFocusCommand(UIElement target)
        {
            return (ICommand)target.GetValue(LostFocusCommandProperty);
        }

        public static void SetLostFocusCommand(UIElement target, ICommand value)
        {
            target.SetValue(LostFocusCommandProperty, value);
        }

        private static void OnLostFocusCommandChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var element = (UIElement)sender;
            if (e.NewValue != null)
            {
                if (e.OldValue == null)
                    element.LostFocus += OnLostFocus;
            }
            else
            {
                if (e.OldValue != null)
                    element.LostFocus -= OnLostFocus;
            }
        }

        private static void OnLostFocus(object sender, RoutedEventArgs e)
        {
            var command = GetLostFocusCommand((UIElement)sender);
            if (command != null && command.CanExecute(e))
                command.Execute(e);
        }

        public static void ForceLostFocusBinding()
        {
            var textBox = Keyboard.FocusedElement as TextBox;
            if (textBox != null)
            {
                var binding = textBox.GetBindingExpression(TextBox.TextProperty);
                if (binding != null)
                    binding.UpdateSource();
            }
        }
    }
}
