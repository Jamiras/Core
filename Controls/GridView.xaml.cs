using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Jamiras.DataModels;
using Jamiras.ViewModels.Grid;

namespace Jamiras.Controls
{
    /// <summary>
    /// Interaction logic for GridView.xaml
    /// </summary>
    public partial class GridView : UserControl
    {
        public GridView()
        {
            InitializeComponent();
        }

        private bool _hasFooter;

        public static readonly DependencyProperty RowsProperty =
            DependencyProperty.Register("Rows", typeof(IEnumerable), typeof(GridView),
                new FrameworkPropertyMetadata(OnRowsChanged));

        public IEnumerable Rows
        {
            get { return (IEnumerable)GetValue(RowsProperty); }
            set { SetValue(RowsProperty, value); }
        }

        private static void OnRowsChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ((GridView)sender).RegenerateRowViewModels();
        }

        private void RegenerateRowViewModels()
        {
            var rows = Rows;
            var columns = Columns;
            if (rows != null && columns != null)
            {
                var rowViewModels = new List<GridRowViewModel>();

                foreach (ModelBase model in rows)
                {
                    var row = new GridRowViewModel(model, columns, ViewModels.ModelBindingMode.Committed);
                    rowViewModels.Add(row);
                }

                RowViewModels = rowViewModels;
            }
        }

        public static readonly DependencyProperty RowViewModelsProperty =
            DependencyProperty.Register("RowViewModels", typeof(IEnumerable<GridRowViewModel>), typeof(GridView),
                new FrameworkPropertyMetadata(OnRowViewModelsChanged));

        public IEnumerable<GridRowViewModel> RowViewModels
        {
            get { return (IEnumerable<GridRowViewModel>)GetValue(RowViewModelsProperty); }
            set { SetValue(RowViewModelsProperty, value); }
        }

        private static void OnRowViewModelsChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var view = (GridView)sender;

            var observableRows = e.OldValue as INotifyCollectionChanged;
            if (observableRows != null)
                observableRows.CollectionChanged -= view.OnRowsCollectionChanged;

            view.BindRows();
        }

        private void BindRows()
        {
            if (_hasFooter && Columns != null)
            {
                var rowViewModels = RowViewModels;
                foreach (var row in rowViewModels)
                    BindRow(row);

                UpdateSummaries();

                var observableRows = rowViewModels as INotifyCollectionChanged;
                if (observableRows != null)
                    observableRows.CollectionChanged += OnRowsCollectionChanged;                
            }
        }

        private void OnRowsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (GridRowViewModel row in e.NewItems)
                        BindRow(row);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (GridRowViewModel row in e.NewItems)
                        UnbindRow(row);
                    break;
            }

            UpdateSummaries();
        }

        private void BindRow(GridRowViewModel row)
        {
            foreach (var column in Columns)
            {
                if (column.HasSummarizeFunction)
                    row.AddPropertyChangedHandler(column.SourceProperty, OnSummaryPropertyChanged);
            }
        }

        private void UnbindRow(GridRowViewModel row)
        {
            foreach (var column in Columns)
            {
                if (column.HasSummarizeFunction)
                    row.RemovePropertyChangedHandler(column.SourceProperty, OnSummaryPropertyChanged);
            }
        }

        private void OnSummaryPropertyChanged(object sender, ModelPropertyChangedEventArgs e)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                var rowViewModels = RowViewModels;
                foreach (var column in Columns)
                {
                    if (column.SourceProperty == e.Property)
                        column.Summarize(rowViewModels);
                }
            }));
        }

        private void UpdateSummaries()
        {
            var rowViewModels = RowViewModels;
            foreach (var column in Columns)
                column.Summarize(rowViewModels);
        }

        public static readonly DependencyProperty ColumnsProperty =
            DependencyProperty.Register("Columns", typeof(IEnumerable<GridColumnDefinition>), typeof(GridView),
                new FrameworkPropertyMetadata(OnColumnsChanged));

        public IEnumerable<GridColumnDefinition> Columns
        {
            get { return (IEnumerable<GridColumnDefinition>)GetValue(ColumnsProperty); } 
            set { SetValue(ColumnsProperty, value); }
        }

        private static void OnColumnsChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var columns = e.NewValue == null ? new GridColumnDefinition[0] : ((IEnumerable<GridColumnDefinition>)e.NewValue).ToArray();

            var view = (GridView)sender;
            var headerGrid = (Grid)view.FindName("headerGrid");
            headerGrid.ColumnDefinitions.Clear();
            headerGrid.Children.Clear();
            foreach (var column in columns)
                headerGrid.ColumnDefinitions.Add(GenerateColumnDefinition(column));

            view._hasFooter = false;
            for (int i = 0; i < columns.Length; i++)
            {
                var border = new Border();
                border.Background = Brushes.Silver;
                border.BorderBrush = Brushes.Gray;

                if (i == 0)
                    border.BorderThickness = new Thickness(0, 0, 1, 1);
                else if (i == columns.Length - 1)
                    border.BorderThickness = new Thickness(1, 0, 0, 1);
                else
                    border.BorderThickness = new Thickness(1, 0, 1, 1);

                var column = columns[i];
                if (column.Width > 0)
                    border.Width = column.Width;

                var text = new TextBlock();
                text.Text = column.Header;
                border.Child = text;

                Grid.SetColumn(border, i);
                headerGrid.Children.Add(border);

                if (column.FooterText != null || column.GetValue(GridColumnDefinition.SummarizeFunctionProperty) != null)
                    view._hasFooter = true;
            }

            var footerGrid = (Grid)view.FindName("footerGrid");
            footerGrid.ColumnDefinitions.Clear();
            footerGrid.Children.Clear();

            if (!view._hasFooter)
            {
                ((UIElement)footerGrid.Parent).Visibility = Visibility.Collapsed;
            }
            else
            {
                foreach (var column in columns)
                    footerGrid.ColumnDefinitions.Add(GenerateColumnDefinition(column));

                for (int i = 0; i < columns.Length; i++)
                {
                    var text = new TextBlock();
                    text.DataContext = columns[i];
                    text.SetBinding(TextBlock.TextProperty, new Binding("FooterText"));
                    if (IsRightAligned(columns[i]))
                        text.TextAlignment = TextAlignment.Right;
                    Grid.SetColumn(text, i);
                    footerGrid.Children.Add(text);
                }                
            }

            if (view.Rows != null)
                view.RegenerateRowViewModels();
            else
                view.BindRows();
        }

        private static bool IsRightAligned(GridColumnDefinition column)
        {
            var displayTextColumn = column as DisplayTextColumnDefinition;
            if (displayTextColumn != null)
                return displayTextColumn.IsRightAligned;

            if (column is IntegerColumnDefinition || column is CurrencyColumnDefinition)
                return true;

            return false;
        }

        internal static ColumnDefinition GenerateColumnDefinition(GridColumnDefinition column)
        {
            var definition = new ColumnDefinition();

            switch (column.WidthType)
            {
                case GridColumnWidthType.Fill:
                    definition.Width = new GridLength(1, GridUnitType.Star);
                    break;

                case GridColumnWidthType.Auto:
                    definition.Width = new GridLength();
                    definition.SharedSizeGroup = column.Header;
                    break;

                case GridColumnWidthType.Fixed:
                    definition.Width = new GridLength(column.Width);
                    break;
            }

            return definition;
        }
    }

    internal class GridRow : Grid
    {
        public static readonly DependencyProperty OwnerProperty =
            DependencyProperty.Register("Owner", typeof(GridView), typeof(GridRow),
                new FrameworkPropertyMetadata(OnOwnerChanged));

        public GridView Owner
        {
            get { return (GridView)GetValue(OwnerProperty); }
            set { SetValue(OwnerProperty, value); }
        }

        private static void OnOwnerChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var view = (GridView)e.NewValue;
            var row = (GridRow)sender;
            var rowViewModel = (GridRowViewModel)row.DataContext;

            foreach (var column in view.Columns)
                row.ColumnDefinitions.Add(GridView.GenerateColumnDefinition(column));

            int i = 0;
            foreach (var column in view.Columns)
            {
                var contentPresenter = new ContentPresenter();
                var cell = rowViewModel.Cells[i];
                if (cell == null)
                {
                    cell = column.CreateFieldViewModelInternal(rowViewModel);
                    rowViewModel.Cells[i] = cell;
                }

                contentPresenter.Content = cell;
                Grid.SetColumn(contentPresenter, i);
                row.Children.Add(contentPresenter);
                i++;
            }
        }
    }
}
