using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Jamiras.Controls
{
    /// <summary>
    /// Attached properties for extending <see cref="Grid"/>s.
    /// </summary>
    public class GridUtils
    {
        /// <summary>
        /// Constrains the height of the <see cref="FrameworkElement"/> to the actual height of the bound row.
        /// </summary>
        public static readonly DependencyProperty ConstrainToRowProperty =
            DependencyProperty.RegisterAttached("ConstrainToRow", typeof(int), typeof(GridUtils),
           new FrameworkPropertyMetadata(-1, OnConstrainToRowChanged));

        /// <summary>
        /// Gets the index of the row the <see cref="FrameworkElement"/> is constrained to.
        /// </summary>
        public static int GetConstrainToRow(FrameworkElement target)
        {
            return (int)target.GetValue(ConstrainToRowProperty);
        }

        /// <summary>
        /// Gets the index of the row to constain the <see cref="FrameworkElement"/> to.
        /// </summary>
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
