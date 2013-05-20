using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Jamiras.Commands;

namespace Jamiras.Controls
{
    /// <summary>
    /// Interaction logic for DatePicker.xaml
    /// </summary>
    public partial class DatePicker : UserControl
    {
        public DatePicker()
        {
            InitializeComponent();
        }

        private void TextBoxGotFocus(object sender, RoutedEventArgs e)
        {
            ((TextBox)sender).SelectAll();
        }

        /// <summary>
        /// Gets whether or not the suggestion list is visible
        /// </summary>
        public bool IsCalendarVisible
        {
            get { return (bool)GetValue(IsCalendarVisibleProperty); }
            set { SetValue(IsCalendarVisibleProperty, value); }
        }

        public static readonly DependencyProperty IsCalendarVisibleProperty =
            DependencyProperty.Register("IsCalendarVisible", typeof(bool), typeof(DatePicker));

        public static readonly DependencyProperty SelectedDateProperty = DependencyProperty.Register("SelectedDate",
            typeof(DateTime), typeof(DatePicker), new FrameworkPropertyMetadata(SelectedDateChanged, CoerceDate));

        public DateTime SelectedDate
        {
            get { return (DateTime)GetValue(SelectedDateProperty); }
            set { SetValue(SelectedDateProperty, value); }
        }

        private static object CoerceDate(DependencyObject d, object value)
        {
            if (!(value is DateTime))
                value = Convert.ToDateTime(value);
            return value;
        }

        private static void SelectedDateChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var picker = (DatePicker)sender;
            var date = (DateTime)e.NewValue;
            picker.popupCalendar.SelectedMonth = date.Month;
            picker.popupCalendar.SelectedYear = date.Year;
            picker.popupCalendar.SelectedDay = date.Day;
        }

        private void CalendarDateClicked(object sender, EventArgs e)
        {
            var calendar = (CalendarControl)sender;
            SelectedDate = new DateTime(calendar.SelectedYear, calendar.SelectedMonth, calendar.SelectedDay);
            IsCalendarVisible = false;
        }

        public ICommand OpenCalendarCommand 
        {
            get { return new DelegateCommand(() => IsCalendarVisible = !IsCalendarVisible); }
        }
    }
}
