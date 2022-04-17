using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Jamiras.DataModels;

namespace Jamiras.Controls
{
    /// <summary>
    /// Attached property for remembering the scroll offset of a <see cref="ScrollViewer"/> when it's not visible.
    /// </summary>
    public class ScrollPreserver : DependencyObject
    {
        /// <summary>
        /// Enables preservation of the vertical scroll offset.
        /// </summary>
        public static readonly DependencyProperty PreserveVerticalOffsetProperty =
            DependencyProperty.RegisterAttached("PreserveVerticalOffset", typeof(bool), typeof(ScrollPreserver),
                new FrameworkPropertyMetadata(OnPreserveVerticalOffsetChanged));

        /// <summary>
        /// Gets whether the vertical scroll offset is preserved for the specified <see cref="ScrollViewer"/>.
        /// </summary>
        public static bool GetPreserveVerticalOffset(ScrollViewer target)
        {
            return (bool)target.GetValue(PreserveVerticalOffsetProperty);
        }

        /// <summary>
        /// Sets whether the vertical scroll offset should be preserved for the specified <see cref="ScrollViewer"/>.
        /// </summary>
        public static void SetPreserveVerticalOffset(ScrollViewer target, bool value)
        {
            target.SetValue(PreserveVerticalOffsetProperty, value);
        }

        private static void OnPreserveVerticalOffsetChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var scrollViewer = FindScrollViewer(sender);
            if (scrollViewer != null && !GetPreserveHorizontalOffset(scrollViewer))
            {
                if ((bool)e.NewValue)
                    AttachObserver(scrollViewer);
                else
                    DetachObserver(scrollViewer);
            }
        }

        /// <summary>
        /// Finds the scroll viewer for an object.
        /// </summary>
        public static ScrollViewer FindScrollViewer(DependencyObject obj)
        {
            var scrollViewer = obj as ScrollViewer;
            if (scrollViewer != null)
                return scrollViewer;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                scrollViewer = FindScrollViewer(child);
                if (scrollViewer != null)
                    return scrollViewer;                
            }

            return null;
        }

        /// <summary>
        /// Enables preservation of the horizontal scroll offset.
        /// </summary>
        public static readonly DependencyProperty PreserveHorizontalOffsetProperty =
            DependencyProperty.RegisterAttached("PreserveHorizontalOffset", typeof(bool), typeof(ScrollPreserver),
                new FrameworkPropertyMetadata(OnPreserveHorizontalOffsetChanged));

        /// <summary>
        /// Gets whether the horizontal scroll offset is preserved for the specified <see cref="ScrollViewer"/>.
        /// </summary>
        public static bool GetPreserveHorizontalOffset(ScrollViewer target)
        {
            return (bool)target.GetValue(PreserveHorizontalOffsetProperty);
        }

        /// <summary>
        /// Sets whether the horizontal scroll offset should be preserved for the specified <see cref="ScrollViewer"/>.
        /// </summary>
        public static void SetPreserveHorizontalOffset(ScrollViewer target, bool value)
        {
            target.SetValue(PreserveHorizontalOffsetProperty, value);
        }

        private static void OnPreserveHorizontalOffsetChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var scrollViewer = (ScrollViewer)sender;
            if (!GetPreserveVerticalOffset(scrollViewer))
            {
                if ((bool)e.NewValue)
                    AttachObserver(scrollViewer);
                else
                    DetachObserver(scrollViewer);
            }
        }

        private static void AttachObserver(ScrollViewer scrollViewer)
        {
            if (scrollViewer.IsLoaded)
                RememberOffset(scrollViewer, scrollViewer.DataContext);
            else
                scrollViewer.Loaded += ScrollViewerOnLoaded;

            scrollViewer.Unloaded += ScrollViewerOnUnloaded;
            scrollViewer.LostFocus += ScrollViewerLostFocus;
            scrollViewer.DataContextChanged += ScrollViewerOnDataContextChanged;
        }

        private static void DetachObserver(ScrollViewer scrollViewer)
        {
            scrollViewer.Loaded -= ScrollViewerOnLoaded;
            scrollViewer.Unloaded -= ScrollViewerOnUnloaded;
            scrollViewer.LostFocus -= ScrollViewerLostFocus;
            scrollViewer.DataContextChanged -= ScrollViewerOnDataContextChanged;
        }

        private static void ScrollViewerOnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var scrollViewer = (ScrollViewer)sender;
            if (scrollViewer.IsLoaded)
            {
                StoreOffset(scrollViewer, e.OldValue);
                RememberOffset(scrollViewer, e.NewValue);

                // refresh the offset again after data binding has completed in case the layout causes the scroll value to change
                scrollViewer.Dispatcher.BeginInvoke(new Action(() => RememberOffset(scrollViewer, e.NewValue)), DispatcherPriority.Render);
            }
        }

        private static void ScrollViewerOnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            var scrollViewer = (ScrollViewer)sender;
            RememberOffset(scrollViewer, scrollViewer.DataContext);
        }

        private static void ScrollViewerLostFocus(object sender, RoutedEventArgs e)
        {
            var scrollViewer = (ScrollViewer)sender;
            StoreOffset(scrollViewer, scrollViewer.DataContext);
        }

        private static void ScrollViewerOnUnloaded(object sender, RoutedEventArgs routedEventArgs)
        {
            var scrollViewer = (ScrollViewer)sender;
            StoreOffset(scrollViewer, scrollViewer.DataContext);
        }

        private static readonly ModelProperty VerticalScrollBarOffsetProperty =
            ModelProperty.Register(typeof(ScrollPreserver), null, typeof(double), 0.0);

        private static readonly ModelProperty HorizontalScrollBarOffsetProperty =
            ModelProperty.Register(typeof(ScrollPreserver), null, typeof(double), 0.0);

        private static void StoreOffset(ScrollViewer scrollViewer, object dataContext)
        {
            var model = dataContext as ModelBase;
            if (model != null)
            {
                if (GetPreserveHorizontalOffset(scrollViewer))
                    model.SetValueCore(HorizontalScrollBarOffsetProperty, scrollViewer.HorizontalOffset);
                if (GetPreserveVerticalOffset(scrollViewer))
                    model.SetValueCore(VerticalScrollBarOffsetProperty, scrollViewer.VerticalOffset);
            }
            else
            {
                // TODO
            }
        }

        private static void RememberOffset(ScrollViewer scrollViewer, object dataContext)
        {
            var model = dataContext as ModelBase;
            if (model != null)
            {
                if (GetPreserveHorizontalOffset(scrollViewer))
                {
                    var horizontalOffset = (double)model.GetValue(HorizontalScrollBarOffsetProperty);
                    scrollViewer.ScrollToHorizontalOffset(horizontalOffset);
                }
                if (GetPreserveVerticalOffset(scrollViewer))
                {
                    var verticalOffset = (double)model.GetValue(VerticalScrollBarOffsetProperty);
                    scrollViewer.ScrollToVerticalOffset(verticalOffset);
                }
            }
            else
            {
                // TODO
            }           
        }
    }
}
