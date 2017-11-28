using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Jamiras.Controls
{
    /// <summary>
    /// Attached properties for animating the appearance of a <see cref="FrameworkElement"/>
    /// </summary>
    public class PopupAnimation
    {
        /// <summary>
        /// Specifies whether the animation is vertical or horizontal.
        /// </summary>
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.RegisterAttached("Orientation", typeof(Orientation), typeof(PopupAnimation),
                new FrameworkPropertyMetadata(Orientation.Vertical));

        /// <summary>
        /// Gets the animation orientation for the <see cref="FrameworkElement"/>.
        /// </summary>
        public static Orientation GetOrientation(FrameworkElement target)
        {
            return (Orientation)target.GetValue(OrientationProperty);
        }

        /// <summary>
        /// Sets the animation orientation for the <see cref="FrameworkElement"/>.
        /// </summary>
        public static void SetOrientation(FrameworkElement target, Orientation value)
        {
            target.SetValue(OrientationProperty, value);
        }

        /// <summary>
        /// Specifies how long it should take to fully show or hide the <see cref="FrameworkElement"/>
        /// </summary>
        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.RegisterAttached("Duration", typeof(Duration), typeof(PopupAnimation),
                new FrameworkPropertyMetadata(new Duration(TimeSpan.FromSeconds(1))));

        /// <summary>
        /// Gets how long it will take to show or hide the <see cref="FrameworkElement"/>.
        /// </summary>
        public static Duration GetDuration(FrameworkElement target)
        {
            return (Duration)target.GetValue(DurationProperty);
        }

        /// <summary>
        /// Sets how long it will take to show or hide the <see cref="FrameworkElement"/>.
        /// </summary>
        public static void SetDuration(FrameworkElement target, Duration value)
        {
            target.SetValue(DurationProperty, value);
        }

        /// <summary>
        /// Specifies the container to show/hide when the animation starts/stops
        /// </summary>
        public static readonly DependencyProperty ContainerProperty =
            DependencyProperty.RegisterAttached("Container", typeof(FrameworkElement), typeof(PopupAnimation),
                new FrameworkPropertyMetadata(null, OnContainerChanged));

        /// <summary>
        /// Gets the container for the animated <see cref="FrameworkElement"/>.
        /// </summary>
        public static FrameworkElement GetContainer(FrameworkElement target)
        {
            return (FrameworkElement)target.GetValue(ContainerProperty);
        }

        /// <summary>
        /// Sets the container for the animated <see cref="FrameworkElement"/>.
        /// </summary>
        public static void SetContainer(FrameworkElement target, FrameworkElement value)
        {
            target.SetValue(ContainerProperty, value);
        }

        private static void OnContainerChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var frameworkElement = e.NewValue as FrameworkElement;
            if (frameworkElement != null)
                frameworkElement.Visibility = GetIsVisible(frameworkElement) ? Visibility.Visible : Visibility.Collapsed;
        }

        private static readonly DependencyProperty VisibleSizeProperty =
            DependencyProperty.RegisterAttached("VisibleSize", typeof(double), typeof(PopupAnimation),
                new FrameworkPropertyMetadata(Double.NaN));

        /// <summary>
        /// Specifies whether or not the <see cref="FrameworkElement"/> should be visible.
        /// </summary>
        public static readonly DependencyProperty IsVisibleProperty =
            DependencyProperty.RegisterAttached("IsVisible", typeof(bool), typeof(PopupAnimation),
                new FrameworkPropertyMetadata(false, OnIsVisibleChanged));

        /// <summary>
        /// Gets whether the <see cref="FrameworkElement"/> should be visible.
        /// </summary>
        public static bool GetIsVisible(FrameworkElement target)
        {
            return (bool)target.GetValue(IsVisibleProperty);
        }

        /// <summary>
        /// Sets whether the <see cref="FrameworkElement"/> should be visible.
        /// </summary>
        public static void SetIsVisible(FrameworkElement target, bool value)
        {
            target.SetValue(IsVisibleProperty, value);
        }

        private static void OnIsVisibleChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var frameworkElement = sender as FrameworkElement;
            if (frameworkElement != null)
            {
                DoubleAnimation animation;
                var container = GetContainer(frameworkElement);

                if ((bool)e.NewValue)
                {
                    if (container != null)
                        container.Visibility = Visibility.Visible;

                    double visibleSize = (double)frameworkElement.GetValue(VisibleSizeProperty);
                    if (Double.IsNaN(visibleSize))
                    {
                        visibleSize = GetOrientation(frameworkElement) == Orientation.Vertical ? frameworkElement.ActualHeight : frameworkElement.ActualWidth;
                        if (visibleSize == 0.0)
                        {
                            frameworkElement.UpdateLayout();
                            visibleSize = GetOrientation(frameworkElement) == Orientation.Vertical ? frameworkElement.ActualHeight : frameworkElement.ActualWidth;
                        }
                    }

                    animation = new DoubleAnimation(0.0, visibleSize, GetDuration(frameworkElement));
                }
                else
                {
                    double visibleSize = GetOrientation(frameworkElement) == Orientation.Vertical ? frameworkElement.ActualHeight : frameworkElement.ActualWidth; ;
                    frameworkElement.SetValue(VisibleSizeProperty, visibleSize);
                    animation = new DoubleAnimation(visibleSize, 0.0, GetDuration(frameworkElement));

                    if (container != null)
                        animation.Completed += (o, e2) => container.Visibility = Visibility.Collapsed;
                }

                if (GetOrientation(frameworkElement) == Orientation.Vertical)
                    frameworkElement.BeginAnimation(FrameworkElement.HeightProperty, animation);
                else
                    frameworkElement.BeginAnimation(FrameworkElement.WidthProperty, animation);
            }
        }
    }
}
