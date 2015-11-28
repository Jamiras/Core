using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Data;

namespace Jamiras.Controls
{
    public class GridUtils
    {
        public static readonly DependencyProperty ConstrainToRowProperty =
            DependencyProperty.RegisterAttached("ConstrainToRow", typeof(int), typeof(GridUtils),
           new FrameworkPropertyMetadata(-1, OnConstrainToRowChanged));

        public static int GetConstrainToRow(FrameworkElement target)
        {
            return (int)target.GetValue(ConstrainToRowProperty);
        }

        public static void SetConstrainToRow(FrameworkElement target, int value)
        {
            target.SetValue(ConstrainToRowProperty, value);
        }

        private static void OnConstrainToRowChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var frameworkElement = sender as FrameworkElement;
            if (frameworkElement != null)
            {
                var binding = new Binding("RowDefinitions[" + e.NewValue + "].ActualHeight");
                binding.RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Grid), 1);
                binding.Converter = new MarginConverter();
                binding.ConverterParameter = frameworkElement;
                BindingOperations.SetBinding(frameworkElement, FrameworkElement.MaxHeightProperty, binding);
            }
        }

        private class MarginConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                var height = (double)value;
                var frameworkElement = (FrameworkElement)parameter;
                return height - frameworkElement.Margin.Top - frameworkElement.Margin.Bottom;
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }
}
