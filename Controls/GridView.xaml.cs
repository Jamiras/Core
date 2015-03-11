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
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Input;

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

            var scrollViewer = (ScrollViewer)FindName("scrollViewer");
            var desc = DependencyPropertyDescriptor.FromProperty(ScrollViewer.ComputedVerticalScrollBarVisibilityProperty, scrollViewer.GetType());
            desc.AddValueChanged(scrollViewer, OnVerticalScrollBarVisibilityChanged);
        }

        private void OnVerticalScrollBarVisibilityChanged(object sender, EventArgs e)
        {
            var scrollViewer = (ScrollViewer)sender;
            double width = scrollViewer.ComputedVerticalScrollBarVisibility == System.Windows.Visibility.Visible ? SystemParameters.VerticalScrollBarWidth : 0.0;

            var headerGrid = (Grid)FindName("headerGrid");
            headerGrid.Margin = new Thickness(0, 0, width, 0);
            var footerGrid = (Grid)FindName("footerGrid");
            ((Border)footerGrid.Parent).Margin = headerGrid.Margin;
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
                var rowViewModels = new ObservableCollection<GridRowViewModel>();

                foreach (ModelBase model in rows)
                {
                    var row = new GridRowViewModel(model, columns, ViewModels.ModelBindingMode.Committed);
                    rowViewModels.Add(row);
                }

                RowViewModels = rowViewModels;
            }
        }

        public static readonly DependencyProperty RowViewModelsProperty =
            DependencyProperty.Register("RowViewModels", typeof(ObservableCollection<GridRowViewModel>), typeof(GridView),
                new FrameworkPropertyMetadata(OnRowViewModelsChanged));

        public ObservableCollection<GridRowViewModel> RowViewModels
        {
            get { return (ObservableCollection<GridRowViewModel>)GetValue(RowViewModelsProperty); }
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
            ((GridView)sender).UpdateHeaders();
        }

        private void UpdateHeaders()
        {
            var columns = Columns == null ? new GridColumnDefinition[0] : ((IEnumerable<GridColumnDefinition>)Columns).ToArray();

            var headerGrid = (Grid)FindName("headerGrid");
            headerGrid.ColumnDefinitions.Clear();
            headerGrid.Children.Clear();
            foreach (var column in columns)
                headerGrid.ColumnDefinitions.Add(GenerateColumnDefinition(column));

            _hasFooter = false;
            for (int i = 0; i < columns.Length; i++)
            {
                var border = new Border();
                border.Background = Brushes.Silver;
                border.BorderBrush = Brushes.Gray;

                if (i == 0)
                    border.BorderThickness = new Thickness(0, 0, 1, 1);
                else if (i == columns.Length - 1 && !HasCommands)
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
                    _hasFooter = true;
            }

            if (HasCommands)
            {
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { SharedSizeGroup = "commands", MinWidth=32 });

                var border = new Border();
                border.Background = Brushes.Silver;
                border.BorderBrush = Brushes.Gray;
                border.BorderThickness = new Thickness(1, 0, 0, 1);

                Grid.SetColumn(border, columns.Length);
                headerGrid.Children.Add(border);
            }

            var footerGrid = (Grid)FindName("footerGrid");
            footerGrid.ColumnDefinitions.Clear();
            footerGrid.Children.Clear();

            if (!_hasFooter)
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

                if (HasCommands)
                    footerGrid.ColumnDefinitions.Add(new ColumnDefinition { SharedSizeGroup = "commands" });
            }

            if (Rows != null)
                RegenerateRowViewModels();
            else if (RowViewModels != null)
                BindRows();
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

        public static readonly DependencyProperty DoubleClickCommandProperty =
            DependencyProperty.Register("DoubleClickCommand", typeof(ICommand), typeof(GridView));

        public ICommand DoubleClickCommand
        {
            get { return (ICommand)GetValue(DoubleClickCommandProperty); }
            set { SetValue(DoubleClickCommandProperty, value); }
        }

        public static readonly DependencyProperty CanReorderProperty =
            DependencyProperty.Register("CanReorder", typeof(bool), typeof(GridView), new FrameworkPropertyMetadata(OnCanReorderChanged));

        public bool CanReorder
        {
            get { return (bool)GetValue(CanReorderProperty); }
            set { SetValue(CanReorderProperty, value); }
        }

        private static void OnCanReorderChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ((GridView)sender).UpdateHasCommands();
        }

        public static readonly DependencyProperty CanRemoveProperty =
            DependencyProperty.Register("CanRemove", typeof(bool), typeof(GridView), new FrameworkPropertyMetadata(OnCanRemoveChanged));

        public bool CanRemove
        {
            get { return (bool)GetValue(CanRemoveProperty); }
            set { SetValue(CanRemoveProperty, value); }
        }

        private static void OnCanRemoveChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ((GridView)sender).UpdateHasCommands();
        }

        private void UpdateHasCommands()
        {
            HasCommands = CanRemove || CanReorder;
        }

        private static readonly DependencyProperty HasCommandsProperty =
            DependencyProperty.Register("HasCommands", typeof(bool), typeof(GridView), new FrameworkPropertyMetadata(OnHasCommandsChanged));

        private bool HasCommands
        {
            get { return (bool)GetValue(HasCommandsProperty); }
            set { SetValue(HasCommandsProperty, value); }
        }

        private static void OnHasCommandsChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ((GridView)sender).UpdateHeaders();
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

            if (view.CanReorder || view.CanRemove)
                row.ColumnDefinitions.Add(new ColumnDefinition { SharedSizeGroup = "commands" });

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

            if (view.CanReorder || view.CanRemove)
            {
                var commands = rowViewModel.Commands;
                if (commands == null)
                    commands = rowViewModel.Commands = new GridRowCommandsViewModel(view, rowViewModel);

                var contentPresenter = new ContentPresenter();
                contentPresenter.Content = commands;
                Grid.SetColumn(contentPresenter, i);
                row.Children.Add(contentPresenter);
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                var command = Owner.DoubleClickCommand;
                if (command != null)
                {
                    var parameter = Owner.Rows != null ? ((GridRowViewModel)this.DataContext).Model : this.DataContext;
                    if (command.CanExecute(parameter))
                        command.Execute(parameter);
                }
            }

            base.OnMouseLeftButtonDown(e);
        }
    }
}
