using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Jamiras.Controls
{
    /// <summary>
    /// Attached properties for binding <see cref="ICommand"/>s to UI events.
    /// </summary>
    public class CommandBinding
    {
        /// <summary>
        /// Property for binding an <see cref="ICommand"/> to the <see cref="E:UIElement.KeyDown"/> event.
        /// </summary>
        public static readonly DependencyProperty KeyDownCommandProperty =
            DependencyProperty.RegisterAttached("KeyDownCommand", typeof(ICommand), typeof(CommandBinding),
                new FrameworkPropertyMetadata(OnKeyDownCommandChanged));

        /// <summary>
        /// Gets the <see cref="ICommand"/> bound to the <see cref="E:UIElement.KeyDown"/> event for the provided <see cref="UIElement"/>.
        /// </summary>
        public static ICommand GetKeyDownCommand(UIElement target)
        {
            return (ICommand)target.GetValue(KeyDownCommandProperty);
        }

        /// <summary>
        /// Binds a <see cref="ICommand"/> to the <see cref="E:UIElement.KeyDown"/> event for the provided <see cref="UIElement"/>.
        /// </summary>
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

        /// <summary>
        /// Property for binding an <see cref="ICommand"/> to the <see cref="E:UIElement.MouseDown"/> event.
        /// </summary>
        public static readonly DependencyProperty ClickCommandProperty =
            DependencyProperty.RegisterAttached("ClickCommand", typeof(ICommand), typeof(CommandBinding),
                new FrameworkPropertyMetadata(OnClickCommandChanged));

        /// <summary>
        /// Gets the <see cref="ICommand"/> bound to the <see cref="E:UIElement.MouseDown"/> event for the provided <see cref="UIElement"/>.
        /// </summary>
        public static ICommand GetClickCommand(UIElement target)
        {
            return (ICommand)target.GetValue(ClickCommandProperty);
        }

        /// <summary>
        /// Binds a <see cref="ICommand"/> to the <see cref="E:UIElement.MouseDown"/> event for the provided <see cref="UIElement"/>.
        /// </summary>
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

        /// <summary>
        /// Property for binding an <see cref="ICommand"/> to the <see cref="MouseAction.LeftDoubleClick"/> gesture.
        /// </summary>
        public static readonly DependencyProperty DoubleClickCommandProperty =
            DependencyProperty.RegisterAttached("DoubleClickCommand", typeof(ICommand), typeof(CommandBinding),
        new FrameworkPropertyMetadata(OnDoubleClickCommandChanged));

        /// <summary>
        /// Gets the <see cref="ICommand"/> bound to the <see cref="MouseAction.LeftDoubleClick"/> gesture for the provided <see cref="UIElement"/>.
        /// </summary>
        public static ICommand GetDoubleClickCommand(UIElement target)
        {
            return (ICommand)target.GetValue(DoubleClickCommandProperty);
        }

        /// <summary>
        /// Binds a <see cref="ICommand"/> to the <see cref="MouseAction.LeftDoubleClick"/> gesture for the provided <see cref="UIElement"/>.
        /// </summary>
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

        /// <summary>
        /// Property for binding a <see cref="bool"/> to a <see cref="UIElement"/> that causes the UIElement to become focused when the bool becomes true.
        /// </summary>
        public static readonly DependencyProperty FocusIfTrueProperty =
            DependencyProperty.RegisterAttached("FocusIfTrue", typeof(bool), typeof(CommandBinding),
                new FrameworkPropertyMetadata(OnFocusIfTrueChanged));

        /// <summary>
        /// Gets whether the FocusIfTrue attached property is <c>true</c> for the <see cref="UIElement"/>.
        /// </summary>
        public static bool GetFocusIfTrue(UIElement target)
        {
            return (bool)target.GetValue(FocusIfTrueProperty);
        }

        /// <summary>
        /// Sets the FocusIfTrue attached property for the <see cref="UIElement"/>.
        /// </summary>
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

        /// <summary>
        /// Property for binding an <see cref="ICommand"/> to the <see cref="E:UIElement.LostFocus"/> event.
        /// </summary>
        public static readonly DependencyProperty LostFocusCommandProperty =
            DependencyProperty.RegisterAttached("LostFocusCommand", typeof(ICommand), typeof(CommandBinding),
                new FrameworkPropertyMetadata(OnLostFocusCommandChanged));

        /// <summary>
        /// Gets the <see cref="ICommand"/> bound to the <see cref="E:UIElement.LostFocus"/> event for the provided <see cref="UIElement"/>.
        /// </summary>
        public static ICommand GetLostFocusCommand(UIElement target)
        {
            return (ICommand)target.GetValue(LostFocusCommandProperty);
        }

        /// <summary>
        /// Binds a <see cref="ICommand"/> to the <see cref="E:UIElement.LostFocus"/> event for the provided <see cref="UIElement"/>.
        /// </summary>
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

        /// <summary>
        /// Ensures that the focused control pushes its updated data to the DataContext object. Many controls wait to update the backing data until the user finishes entering data.
        /// </summary>
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
