using Jamiras.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Jamiras.Controls
{
    /// <summary>
    /// Attached properties for extending <see cref="ListView"/>s.
    /// </summary>
    public class ListViewUtils
    {
        /// <summary>
        /// Allows a <see cref="GridViewColumn"/> to automatically size to fill the remaining width of the <see cref="ListView"/>
        /// </summary>
        public static readonly DependencyProperty AutoSizeColumnProperty =
            DependencyProperty.RegisterAttached("AutoSizeColumn", typeof(bool), typeof(ListViewUtils),
                new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Gets whether or not the <see cref="GridViewColumn"/> should size to fill the remaining width of the <see cref="ListView"/>
        /// </summary>
        public static bool GetAutoSizeColumn(GridViewColumn target)
        {
            return (bool)target.GetValue(AutoSizeColumnProperty);
        }

        /// <summary>
        /// Sets whether or not the <see cref="GridViewColumn"/> should size to fill the remaining width of the <see cref="ListView"/>
        /// </summary>
        public static void SetAutoSizeColumn(GridViewColumn target, bool value)
        {
            target.SetValue(AutoSizeColumnProperty, value);
        }

        /// <summary>
        /// Specifies that a <see cref="ListView"/> has AutoSize columns.
        /// </summary>
        public static readonly DependencyProperty HasAutoSizeColumnsProperty =
            DependencyProperty.RegisterAttached("HasAutoSizeColumns", typeof(bool), typeof(ListViewUtils),
                new FrameworkPropertyMetadata(false, OnHasAutoSizeColumnsChanged));

        /// <summary>
        /// Gets whether or not a <see cref="ListView"/> has AutoSize columns.
        /// </summary>
        public static bool GetHasAutoSizeColumns(FrameworkElement target)
        {
            return (bool)target.GetValue(HasAutoSizeColumnsProperty);
        }

        /// <summary>
        /// Sets whether or not a <see cref="ListView"/> has AutoSize columns.
        /// </summary>
        public static void SetHasAutoSizeColumns(FrameworkElement target, bool value)
        {
            target.SetValue(HasAutoSizeColumnsProperty, value);
        }

        private static void OnHasAutoSizeColumnsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var listView = sender as ListView;

            if ((bool)e.OldValue)
            {
                listView.SizeChanged -= ListViewSizeChanged;
            }
            else
            {
                if ((bool)e.NewValue)
                    listView.SizeChanged += ListViewSizeChanged;

                UpdateAutoSizeColumns(listView);
            }
        }

        private static void ListViewSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.WidthChanged)
                UpdateAutoSizeColumns((ListView)sender);
        }


        private static readonly DependencyProperty ScrollViewerVisibilityMonitorProperty =
            DependencyProperty.RegisterAttached("ScrollViewerVisibilityMonitor", typeof(bool), typeof(ListViewUtils),
                new FrameworkPropertyMetadata(false, OnScrollViewerVisibilityMonitorChanged));

        private static void OnScrollViewerVisibilityMonitorChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var scrollViewer = (ScrollViewer)sender;
            var descriptor = DependencyPropertyDescriptor.FromProperty(ScrollViewer.ComputedVerticalScrollBarVisibilityProperty, typeof(ScrollViewer));

            if ((bool)e.NewValue)
                descriptor.AddValueChanged(scrollViewer, ScrollViewerVisibilityChanged);
            else
                descriptor.RemoveValueChanged(scrollViewer, ScrollViewerVisibilityChanged);
        }

        private static void ScrollViewerVisibilityChanged(object sender, EventArgs e)
        {
            var obj = sender as DependencyObject;
            while (obj != null)
            {
                var listView = obj as ListView;
                if (listView != null)
                {
                    UpdateAutoSizeColumns(listView);
                    return;
                }

                obj = VisualTreeHelper.GetParent(obj);
            }
        }

        private static void UpdateAutoSizeColumns(ListView listView)
        { 
            var gridView = listView.View as System.Windows.Controls.GridView;
            if (gridView == null)
                return;

            var autoSizeColumns = new List<GridViewColumn>();
            double remainingWidth = listView.ActualWidth - gridView.Columns.Count * 2;

            var scrollViewer = ScrollPreserver.FindScrollViewer(listView);
            if (scrollViewer != null)
            {
                scrollViewer.SetValue(ScrollViewerVisibilityMonitorProperty, true);
                if (scrollViewer.ComputedVerticalScrollBarVisibility == Visibility.Visible)
                    remainingWidth -= SystemParameters.VerticalScrollBarWidth;
            }

            foreach (var column in gridView.Columns)
            {
                if (GetAutoSizeColumn(column))
                    autoSizeColumns.Add(column);
                else
                    remainingWidth -= column.ActualWidth;
            }

            if (autoSizeColumns.Count > 0)
            {
                remainingWidth /= autoSizeColumns.Count;
                if (remainingWidth > 0)
                {
                    foreach (var column in autoSizeColumns)
                        column.Width = remainingWidth;
                }
            }
        }
    }
}
