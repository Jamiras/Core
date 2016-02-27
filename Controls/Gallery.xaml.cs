﻿using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System;

namespace Jamiras.Controls
{
    /// <summary>
    /// Interaction logic for Gallery.xaml
    /// </summary>
    public partial class Gallery : UserControl
    {
        public Gallery()
        {
            InitializeComponent();
        }

        private class GalleryRow
        {
            public object[] Items { get; set; }
        }

        public static DependencyProperty ItemTemplateProperty = DependencyProperty.Register("ItemTemplate", typeof(DataTemplate), typeof(Gallery));

        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        public static DependencyProperty ItemWidthProperty = DependencyProperty.Register("ItemWidth", typeof(double), typeof(Gallery), new FrameworkPropertyMetadata(OnItemWidthChanged));

        public double ItemWidth
        {
            get { return (double)GetValue(ItemWidthProperty); }
            set { SetValue(ItemWidthProperty, value); }
        }

        private static void OnItemWidthChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ((Gallery)sender).UpdatePerRow();
        }

        public static DependencyProperty ItemsSourceProperty = DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(Gallery), new FrameworkPropertyMetadata(OnItemsSourceChanged));

        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        private static void OnItemsSourceChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var gallery = (Gallery)sender;
            var observable = e.OldValue as INotifyCollectionChanged;
            if (observable != null)
                observable.CollectionChanged -= gallery.CollectionChanged;

            observable = e.NewValue as INotifyCollectionChanged;
            if (observable != null)
                observable.CollectionChanged += gallery.CollectionChanged;

            gallery.UpdateRows();
        }

        private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateRows();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdatePerRow();
        }

        private void UpdatePerRow()
        {
            if (ActualWidth > 0 && ItemWidth > 0)
            {
                int perRow = (int)((ActualWidth - SystemParameters.VerticalScrollBarWidth) / ItemWidth);
                if (perRow < 1)
                    perRow = 1;

                PerRow = perRow;
            }
        }

        public static DependencyProperty PerRowProperty = DependencyProperty.Register("PerRow", typeof(int), typeof(Gallery), new FrameworkPropertyMetadata(OnPerRowChanged));

        public int PerRow
        {
            get { return (int)GetValue(PerRowProperty); }
            set { SetValue(PerRowProperty, value); }
        }

        private static void OnPerRowChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ((Gallery)sender).UpdateRows();
        }

        private void UpdateRows()
        {
            if (ItemsSource == null || PerRow == 0)
                return;

            var perRow = PerRow;
            var rows = new List<GalleryRow>();
            var row = new GalleryRow { Items = new object[perRow] };
            var columnIndex = 0;
            foreach (var item in ItemsSource)
            {
                row.Items[columnIndex++] = item;
                if (columnIndex == perRow)
                {
                    rows.Add(row);
                    row = new GalleryRow { Items = new object[perRow] };
                    columnIndex = 0;
                }
            }

            if (columnIndex > 0)
            {
                var newItems = new object[columnIndex];
                Array.Copy(row.Items, newItems, columnIndex);
                row.Items = newItems;
                rows.Add(row);
            }

            _listBox.ItemsSource = rows;
        }
    }
}
