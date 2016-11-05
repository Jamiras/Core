using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Controls;

namespace Jamiras.Controls
{
    public class PopupAnimation
    {
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.RegisterAttached("Orientation", typeof(Orientation), typeof(PopupAnimation),
                new FrameworkPropertyMetadata(Orientation.Vertical));

        public static Orientation GetOrientation(FrameworkElement target)
        {
            return (Orientation)target.GetValue(OrientationProperty);
        }

        public static void SetOrientation(FrameworkElement target, Orientation value)
        {
            target.SetValue(OrientationProperty, value);
        }

        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.RegisterAttached("Duration", typeof(Duration), typeof(PopupAnimation),
                new FrameworkPropertyMetadata(new Duration(TimeSpan.FromSeconds(1))));

        public static Duration GetDuration(FrameworkElement target)
        {
            return (Duration)target.GetValue(DurationProperty);
        }

        public static void SetDuration(FrameworkElement target, Duration value)
        {
            target.SetValue(DurationProperty, value);
        }

        public static readonly DependencyProperty ContainerProperty =
            DependencyProperty.RegisterAttached("Container", typeof(FrameworkElement), typeof(PopupAnimation),
                new FrameworkPropertyMetadata(null, OnContainerChanged));

        public static FrameworkElement GetContainer(FrameworkElement target)
        {
            return (FrameworkElement)target.GetValue(ContainerProperty);
        }

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

        public static readonly DependencyProperty IsVisibleProperty =
            DependencyProperty.RegisterAttached("IsVisible", typeof(bool), typeof(PopupAnimation),
                new FrameworkPropertyMetadata(false, OnIsVisibleChanged));

        public static bool GetIsVisible(FrameworkElement target)
        {
            return (bool)target.GetValue(IsVisibleProperty);
        }

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
