using System.Windows;
using System.Windows.Controls;

namespace Jamiras.Controls
{
    public class TreeUtils
    {
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.RegisterAttached("SelectedItem", typeof(object), typeof(TreeUtils),
                new FrameworkPropertyMetadata(typeof(TreeUtils), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemChanged));

        private static readonly DependencyProperty IsSelectedItemAttachedProperty =
            DependencyProperty.RegisterAttached("IsSelectedItemAttached", typeof(bool), typeof(TreeUtils),
                new FrameworkPropertyMetadata(false));
    
        public static object GetSelectedItem(TreeView target)
        {
            return (object)target.GetValue(SelectedItemProperty);
        }

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
                tv.SelectedItemChanged += treeView_SelectedItemChanged;
                tv.SetValue(IsSelectedItemAttachedProperty, false);
            }

            if (tv.SelectedItem != e.NewValue)
                SelectItem(tv, e.NewValue);
        }

        private static void SelectItem(TreeView treeView, object item)
        {
            var tvi = (TreeViewItem)treeView.ItemContainerGenerator.ContainerFromItem(item);
            if (tvi != null)
                tvi.IsSelected = true;
        }

        private static void treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            SetSelectedItem((TreeView)sender, e.NewValue);
        }
    }
}
