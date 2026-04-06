using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Jamiras.Controls
{
    /// <summary>
    /// Attached properties for extending <see cref="TreeView"/> functionality.
    /// </summary>
    public class TreeUtils
    {
        /// <summary>
        /// Bindable property for the selected item within a hierarchical items source.
        /// </summary>
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.RegisterAttached("SelectedItem", typeof(object), typeof(TreeUtils),
                new FrameworkPropertyMetadata(typeof(TreeUtils), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemChanged));

        private static readonly DependencyProperty IsSelectedItemAttachedProperty =
            DependencyProperty.RegisterAttached("IsSelectedItemAttached", typeof(bool), typeof(TreeUtils),
                new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Gets the selected item for the <see cref="TreeView"/>.
        /// </summary>
        public static object GetSelectedItem(TreeView target)
        {
            return target.GetValue(SelectedItemProperty);
        }

        /// <summary>
        /// Sets the selected item for the <see cref="TreeView"/>.
        /// </summary>
        public static void SetSelectedItem(TreeView target, object value)
        {
            target.SetValue(SelectedItemProperty, value);
        }

        private static void OnSelectedItemChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var tv = (TreeView)sender;

            bool isAttached = (bool)tv.GetValue(IsSelectedItemAttachedProperty);
            if (!isAttached)
            {
                if (e.NewValue != null && !tv.IsLoaded)
                    tv.Loaded += treeView_Loaded;

                tv.SelectedItemChanged += treeView_SelectedItemChanged;
                tv.SetValue(IsSelectedItemAttachedProperty, true);
            }

            if (tv.SelectedItem != e.NewValue)
                SelectItem(tv, e.NewValue);
        }

        private static void treeView_Loaded(object sender, System.EventArgs e)
        {
            var tv = (TreeView)sender;
            tv.Loaded -= treeView_Loaded;

            var item = GetSelectedItem(tv);
            if (item != null)
                SelectItem(tv, item);
        }

        private static void SelectItem(TreeView treeView, object item)
        {
            var tvi = FindTreeViewItem(treeView, item);
            if (tvi != null)
                tvi.IsSelected = true;
        }

        private static TreeViewItem FindTreeViewItem(ItemsControl parent, object item)
        {
            var tvi = parent.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
            if (tvi != null)
                return tvi;

            foreach (var child in parent.Items)
            {
                tvi = parent.ItemContainerGenerator.ContainerFromItem(child) as TreeViewItem;
                if (tvi != null)
                {
                    tvi = FindTreeViewItem(tvi, item);
                    if (tvi != null)
                        return tvi;
                }
            }

            return null;
        }

        private static void treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            SetSelectedItem((TreeView)sender, e.NewValue);
        }

        /// <summary>
        /// Bindable property for the selecting an item within a hierarchical items source on right click.
        /// </summary>
        public static readonly DependencyProperty SelectOnRightClickProperty =
            DependencyProperty.RegisterAttached("SelectedOnRightClick", typeof(bool), typeof(TreeUtils),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectOnRightClickChanged));

        /// <summary>
        /// Gets where the <see cref="TreeView"/> being right-clicked should be selected.
        /// </summary>
        public static bool GetSelectOnRightClick(TreeView target)
        {
            return (bool)target.GetValue(SelectOnRightClickProperty);
        }

        /// <summary>
        /// Sets where the <see cref="TreeView"/> being right-clicked should be selected.
        /// </summary>
        public static void SetSelectOnRightClick(TreeView target, bool value)
        {
            target.SetValue(SelectOnRightClickProperty, value);
        }

        private static void OnSelectOnRightClickChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var tv = (TreeView)sender;

            if ((bool)e.NewValue)
                tv.PreviewMouseRightButtonDown += treeview_PreviewMouseRightButtonDown;
            else
                tv.PreviewMouseRightButtonDown += treeview_PreviewMouseRightButtonDown;
        }

        private static void treeview_PreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Get the element that was actually clicked
            DependencyObject obj = (DependencyObject)e.OriginalSource;

            // Traverse up the visual tree to find the TreeViewItem container
            while (obj != null)
            {
                var item = obj as TreeViewItem;
                if (item != null)
                {
                    // Found it. Focus it.
                    item.Focus();
                    item.IsSelected = true;
                    break;
                }

                obj = VisualTreeHelper.GetParent(obj);
            }
        }
    }
}
