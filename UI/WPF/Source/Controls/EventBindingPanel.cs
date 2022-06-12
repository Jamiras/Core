using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Jamiras.Controls
{
    /// <summary>
    /// <see cref="Panel"/> extension that allows binding commands to mouse clicks.
    /// </summary>
    public class EventBindingPanel : Panel
    {
        /// <summary>
        /// When overridden in a derived class, measures the size in layout required for child elements and determines a size for the <see cref="T:System.Windows.FrameworkElement"/>-derived class. 
        /// </summary>
        /// <returns>
        /// The size that this element determines it needs during layout, based on its calculations of child element sizes.
        /// </returns>
        /// <param name="availableSize">The available size that this element can give to child elements. Infinity can be specified as a value to indicate that the element will size to whatever content is available.</param>
        protected override Size MeasureOverride(Size availableSize)
        {
            Size requestedSize = Size.Empty;
            foreach (UIElement element in InternalChildren)
            {
                element.Measure(availableSize);
                var desiredSize = element.DesiredSize;
                requestedSize = new Size(Math.Max(requestedSize.Width, desiredSize.Width), Math.Max(requestedSize.Height, desiredSize.Height));
            }

            return requestedSize;
        }

        /// <summary>
        /// When overridden in a derived class, positions child elements and determines a size for a <see cref="T:System.Windows.FrameworkElement"/> derived class. 
        /// </summary>
        /// <returns>
        /// The actual size used.
        /// </returns>
        /// <param name="finalSize">The final area within the parent that this element should use to arrange itself and its children.</param>
        protected override Size ArrangeOverride(Size finalSize)
        {
            foreach (UIElement element in InternalChildren)
            {
                var desiredSize = element.DesiredSize;
                double width = Math.Min(finalSize.Width, desiredSize.Width);
                double height = Math.Min(finalSize.Height, desiredSize.Height);
                double x = 0, y = 0;

                var frameworkElement = element as FrameworkElement;
                if (frameworkElement != null)
                {
                    switch (frameworkElement.HorizontalAlignment)
                    {
                        case HorizontalAlignment.Right:
                            x = finalSize.Width - width;
                            break;

                        case HorizontalAlignment.Stretch:
                            width = finalSize.Width;
                            break;

                        case HorizontalAlignment.Center:
                            x = (finalSize.Width - width) / 2;
                            break;
                    }

                    switch (frameworkElement.VerticalAlignment)
                    {
                        case VerticalAlignment.Bottom:
                            y = finalSize.Height - height;
                            break;

                        case VerticalAlignment.Stretch:
                            height = finalSize.Height;
                            break;

                        case VerticalAlignment.Center:
                            y = (finalSize.Height - height) / 2;
                            break;
                    }
                }

                var elementRect = new Rect(x, y, width, height);
                element.Arrange(elementRect);
            }

            return finalSize;
        }

        /// <summary>
        /// Invoked when an unhandled <see cref="E:System.Windows.UIElement.MouseLeftButtonDown"/> routed event is raised on this element. Implement this method to add class handling for this event. 
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseButtonEventArgs"/> that contains the event data. The event data reports that the left mouse button was pressed.</param>
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                var command = DoubleClickCommand;
                if (command != null)
                {
                    var parameter = DoubleClickCommandParameter;
                    if (command.CanExecute(parameter))
                        command.Execute(parameter);
                }
            }

            base.OnMouseLeftButtonDown(e);
        }

        /// <summary>
        /// Invoked when an unhandled <see cref="E:System.Windows.UIElement.MouseLeftButtonUp"/> routed event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event. 
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseButtonEventArgs"/> that contains the event data. The event data reports that the left mouse button was released.</param>
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                var command = ClickCommand;
                if (command != null)
                {
                    var parameter = ClickCommandParameter;
                    if (command.CanExecute(parameter))
                        command.Execute(parameter);
                }
            }

            base.OnMouseLeftButtonUp(e);
        }

        /// <summary>
        /// <see cref="DependencyProperty"/> for <see cref="ClickCommand"/>
        /// </summary>
        public static readonly DependencyProperty ClickCommandProperty =
            DependencyProperty.Register("ClickCommand", typeof(ICommand), typeof(EventBindingPanel));

        /// <summary>
        /// Gets or sets the command to execute when the user clicks on the panel.
        /// </summary>
        public ICommand ClickCommand
        {
            get { return (ICommand)GetValue(ClickCommandProperty); }
            set { SetValue(ClickCommandProperty, value); }
        }

        /// <summary>
        /// <see cref="DependencyProperty"/> for <see cref="ClickCommandParameter"/>
        /// </summary>
        public static readonly DependencyProperty ClickCommandParameterProperty =
            DependencyProperty.Register("ClickCommandParameter", typeof(object), typeof(EventBindingPanel));

        /// <summary>
        /// Gets or sets the object to pass to the command that is executed when the user clicks on the panel.
        /// </summary>
        public object ClickCommandParameter
        {
            get { return GetValue(ClickCommandParameterProperty); }
            set { SetValue(ClickCommandParameterProperty, value); }
        }

        /// <summary>
        /// <see cref="DependencyProperty"/> for <see cref="DoubleClickCommand"/>
        /// </summary>
        public static readonly DependencyProperty DoubleClickCommandProperty =
            DependencyProperty.Register("DoubleClickCommand", typeof(ICommand), typeof(EventBindingPanel));

        /// <summary>
        /// Gets or sets the command to execute when the user double clicks on the panel.
        /// </summary>
        public ICommand DoubleClickCommand
        {
            get { return (ICommand)GetValue(DoubleClickCommandProperty); }
            set { SetValue(DoubleClickCommandProperty, value); }
        }

        /// <summary>
        /// <see cref="DependencyProperty"/> for <see cref="DoubleClickCommandParameter"/>
        /// </summary>
        public static readonly DependencyProperty DoubleClickCommandParameterProperty =
            DependencyProperty.Register("DoubleClickCommandParameter", typeof(object), typeof(EventBindingPanel));

        /// <summary>
        /// Gets or sets the object to pass to the command that is executed when the user double clicks on the panel.
        /// </summary>
        public object DoubleClickCommandParameter
        {
            get { return GetValue(DoubleClickCommandParameterProperty); }
            set { SetValue(DoubleClickCommandParameterProperty, value); }
        }
    }
}
