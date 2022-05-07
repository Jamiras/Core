using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Jamiras.Controls
{
    /// <summary>
    /// attached behavior for a ListView using a GridView to allow the columns to sort
    /// </summary>
    /// <example>
    /// 
    ///     &lt;ListView ItemsSource="{Binding Items}" jamiras:GridViewSort.IsEnabled="true"&gt;
    ///         &lt;ListView.ItemContainerStyle&gt;
    ///             &lt;Style TargetType="{x:Type ListViewItem}"&gt;
    ///                 &lt;Setter Property="HorizontalContentAlignment" Value="Stretch" /&gt;
    ///             &lt;/Style&gt;
    ///         &lt;/ListView.ItemContainerStyle&gt;
    ///         &lt;ListView.View&gt;
    ///             &lt;GridView&gt;
    ///                 &lt;GridViewColumn Width="100" Header="Name" DisplayMemberBinding="{Binding Name}" /&gt;
    ///                 ...
    ///
    /// </example>
    public static class GridViewSort
    {
        /// <summary>
        /// Enables sorting
        /// </summary>
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(GridViewSort),
                new FrameworkPropertyMetadata(false, OnIsEnabledChanged));

        /// <summary>
        /// Gets whether the <see cref="ListView"/> allows sorting by clicking on headers.
        /// </summary>
        public static bool GetIsEnabled(ListView listView)
        {
            return (bool)listView.GetValue(IsEnabledProperty);
        }

        /// <summary>
        /// Sets whether the <see cref="ListView"/> should allow sorting by clicking on headers.
        /// </summary>
        public static void SetIsEnabled(ListView target, bool value)
        {
            target.SetValue(IsEnabledProperty, value);
        }

        private static void OnIsEnabledChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var listView = sender as ListView;
            if (listView != null)
            {
                if ((bool)e.NewValue == true)
                    listView.AddHandler(GridViewColumnHeader.ClickEvent, new RoutedEventHandler(OnHeaderClicked));
                else
                    listView.RemoveHandler(GridViewColumnHeader.ClickEvent, new RoutedEventHandler(OnHeaderClicked));
            }
        }

        private static readonly DependencyProperty LastHeaderClickedProperty =
            DependencyProperty.RegisterAttached("LastHeaderClicked", typeof(GridViewColumnHeader), typeof(GridViewSort));

        private static GridViewColumnHeader GetLastHeaderClicked(ListView listView)
        {
            return (GridViewColumnHeader)listView.GetValue(LastHeaderClickedProperty);
        }

        private static void SetLastHeaderClicked(ListView listView, GridViewColumnHeader value)
        {
            listView.SetValue(LastHeaderClickedProperty, value);
        }

        private static readonly DependencyProperty LastSortDirectionProperty =
            DependencyProperty.RegisterAttached("LastSortDirection", typeof(ListSortDirection), typeof(GridViewSort), new FrameworkPropertyMetadata(ListSortDirection.Ascending));

        private static ListSortDirection GetLastSortDirection(ListView listView)
        {
            return (ListSortDirection)listView.GetValue(LastSortDirectionProperty);
        }

        private static void SetLastSortDirection(ListView listView, ListSortDirection value)
        {
            listView.SetValue(LastSortDirectionProperty, value);
        }

        private static void OnHeaderClicked(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader headerClicked = e.OriginalSource as GridViewColumnHeader;
            if (headerClicked == null)
                return;

            if (headerClicked.Role == GridViewColumnHeaderRole.Padding)
                return;

            var lv = (ListView)sender;
            ListSortDirection direction;

            if (headerClicked != GetLastHeaderClicked(lv))
                direction = ListSortDirection.Ascending;
            else if (GetLastSortDirection(lv) == ListSortDirection.Ascending)
                direction = ListSortDirection.Descending;
            else
                direction = ListSortDirection.Ascending;

            // sort
            var header = headerClicked.Column.Header as string;
            ICollectionView dataView = CollectionViewSource.GetDefaultView(lv.ItemsSource);

            dataView.SortDescriptions.Clear();
            SortDescription sd = new SortDescription(header, direction);
            dataView.SortDescriptions.Add(sd);
            dataView.Refresh();

            // TODO: update arrow
            //if (direction == ListSortDirection.Ascending)
            //{
            //    headerClicked.Column.HeaderTemplate =
            //      Resources["HeaderTemplateArrowUp"] as DataTemplate;
            //}
            //else
            //{
            //    headerClicked.Column.HeaderTemplate =
            //      Resources["HeaderTemplateArrowDown"] as DataTemplate;
            //}

            //// Remove arrow from previously sorted header
            //if (_lastHeaderClicked != null && _lastHeaderClicked != headerClicked)
            //{
            //    _lastHeaderClicked.Column.HeaderTemplate = null;
            //}

            SetLastHeaderClicked(lv, headerClicked);
            SetLastSortDirection(lv, direction);
        }
    }
}
