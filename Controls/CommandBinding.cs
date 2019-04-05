using Jamiras.Commands;
using System;
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
            var binding = element.InputBindings.OfType<InputBinding>().FirstOrDefault(b =>
            {
                var gesture = b.Gesture as MouseGesture;
                return (gesture != null && gesture.MouseAction == MouseAction.LeftDoubleClick);
            });

            if (binding == null)
            {
                if (e.NewValue != null)
                {
                    binding = new InputBinding((ICommand)e.NewValue, new MouseGesture(MouseAction.LeftDoubleClick));
                    element.InputBindings.Add(binding);
                }
            }
            else
            {
                if (e.NewValue != null)
                    binding.Command = (ICommand)e.NewValue;
                else
                    element.InputBindings.Remove(binding);
            }
        }

        /// <summary>
        /// Property for binding an <see cref="ICommand"/> to the <see cref="MouseAction.LeftDoubleClick"/> gesture.
        /// </summary>
        public static readonly DependencyProperty DoubleClickCommandParameterProperty =
            DependencyProperty.RegisterAttached("DoubleClickCommandParameter", typeof(object), typeof(CommandBinding),
        new FrameworkPropertyMetadata(OnDoubleClickCommandParameterChanged));

        /// <summary>
        /// Gets the <see cref="ICommand"/> bound to the <see cref="MouseAction.LeftDoubleClick"/> gesture for the provided <see cref="UIElement"/>.
        /// </summary>
        public static object GetDoubleClickCommandParameter(UIElement target)
        {
            return target.GetValue(DoubleClickCommandParameterProperty);
        }

        /// <summary>
        /// Binds a <see cref="ICommand"/> to the <see cref="MouseAction.LeftDoubleClick"/> gesture for the provided <see cref="UIElement"/>.
        /// </summary>
        public static void SetDoubleClickCommandParameter(UIElement target, object value)
        {
            target.SetValue(DoubleClickCommandParameterProperty, value);
        }

        private static void OnDoubleClickCommandParameterChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var element = (UIElement)sender;
            var binding = element.InputBindings.OfType<InputBinding>().FirstOrDefault(b =>
            {
                var gesture = b.Gesture as MouseGesture;
                return (gesture != null && gesture.MouseAction == MouseAction.LeftDoubleClick);
            });

            if (binding != null)
                binding.CommandParameter = e.NewValue;
        }

        /// <summary>
        /// Property for binding a <see cref="bool"/> to a <see cref="UIElement"/> that causes the UIElement to become focused when the bool becomes true.
        /// </summary>
        public static readonly DependencyProperty FocusIfTrueProperty =
            DependencyProperty.RegisterAttached("FocusIfTrue", typeof(bool), typeof(CommandBinding),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnFocusIfTrueChanged));

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
                var uiElement = (FrameworkElement)sender;
                if (!uiElement.IsLoaded)
                {
                    uiElement.Loaded += OnFocusRequestedElementLoaded;
                    return;
                }

                uiElement.Focus();

                // asynchronously set the value back to false. trying to update the source property from wihtin the property changed handler gets swallowed by the WPF framework
                uiElement.Dispatcher.BeginInvoke(new Action<UIElement>((UIElement element) =>
                {
                    SetFocusIfTrue(element, false);
                }), uiElement);
            }
        }

        private static void OnFocusRequestedElementLoaded(object sender, RoutedEventArgs e)
        {
            var uiElement = (FrameworkElement)sender;
            uiElement.Loaded -= OnFocusRequestedElementLoaded;

            if (GetFocusIfTrue(uiElement))
            {
                uiElement.Focus();
                SetFocusIfTrue(uiElement, false);
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

        /// <summary>
        /// Property for <see cref="TextBox"/> that causes the contents to be selected whenever the TextBox gets focused.
        /// </summary>
        public static readonly DependencyProperty SelectAllOnFocusProperty =
            DependencyProperty.RegisterAttached("SelectAllOnFocus", typeof(bool), typeof(CommandBinding),
                new FrameworkPropertyMetadata(OnSelectAllOnFocusChanged));

        /// <summary>
        /// Gets whether the SelectAllOnFocus attached property is <c>true</c> for the <see cref="TextBox"/>.
        /// </summary>
        public static bool GetSelectAllOnFocus(TextBox target)
        {
            return (bool)target.GetValue(SelectAllOnFocusProperty);
        }

        /// <summary>
        /// Sets the SelectAllOnFocus attached property for the <see cref="TextBox"/>.
        /// </summary>
        public static void SetSelectAllOnFocus(TextBox target, bool value)
        {
            target.SetValue(SelectAllOnFocusProperty, value);
        }

        private static void OnSelectAllOnFocusChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                if ((bool)e.NewValue)
                {
                    textBox.GotKeyboardFocus += TextBox_SelectAllOnFocus;

                    if (textBox.IsKeyboardFocusWithin)
                        SelectAll(textBox);
                }
                else
                    textBox.GotKeyboardFocus -= TextBox_SelectAllOnFocus;
            }
        }

        private static void TextBox_SelectAllOnFocus(object sender, RoutedEventArgs e)
        {
            SelectAll((TextBox)sender);
        }

        private static void SelectAll(TextBox textBox)
        {
            textBox.Dispatcher.BeginInvoke(new Action(() =>
            {
                textBox.SelectAll();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        /// <summary>
        /// Property for a <see cref="MenuItem"/> that sets its InputGesture property and creates an InputBinding for it.
        /// </summary>
        public static readonly DependencyProperty InputGestureProperty =
            DependencyProperty.RegisterAttached("InputGesture", typeof(string), typeof(CommandBinding),
                new FrameworkPropertyMetadata(OnInputGestureChanged));

        /// <summary>
        /// Gets whether the InputGesture attached property is <c>true</c> for the <see cref="MenuItem"/>.
        /// </summary>
        public static string GetInputGesture(MenuItem target)
        {
            return (string)target.GetValue(InputGestureProperty);
        }

        /// <summary>
        /// Sets the InputGesture attached property for the <see cref="MenuItem"/>.
        /// </summary>
        public static void SetInputGesture(MenuItem target, string value)
        {
            target.SetValue(InputGestureProperty, value);
        }

        private static void OnInputGestureChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var menuItem = (MenuItem)sender;
            var gestureText = (String)e.NewValue;
            KeyGesture gesture = null;

            if (String.IsNullOrEmpty(gestureText))
            {
                menuItem.InputGestureText = null;
            }
            else
            {
                menuItem.InputGestureText = gestureText;
                gesture = (KeyGesture)new KeyGestureConverter().ConvertFrom(null, System.Globalization.CultureInfo.CurrentUICulture, gestureText);
            }

            var uiElement = sender;
            while (uiElement != null)
            {
                var window = uiElement as Window;
                if (window != null)
                {
                    var bindings = window.InputBindings.OfType<KeyBinding>();
                    var binding = bindings.FirstOrDefault(b => b.Key == gesture.Key && b.Modifiers == gesture.Modifiers);
                    if (binding == null)
                    {
                        if (gesture == null)
                            return;

                        binding = new KeyBinding { Gesture = gesture };
                        window.InputBindings.Add(binding);
                    }

                    binding.Command = new DelegateCommand<MenuItem>(ActivateMenuItem);
                    binding.CommandParameter = menuItem;
                }

                uiElement = LogicalTreeHelper.GetParent(uiElement);
            }
        }

        private static void ActivateMenuItem(MenuItem item)
        {
            var command = item.Command;
            if (command != null && command.CanExecute(item.CommandParameter))
                command.Execute(item.CommandParameter);
        }
    }
}
