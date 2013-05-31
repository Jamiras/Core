using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

            SetCalendarDate(DateTime.Today);
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
            typeof(DateTime?), typeof(DatePicker), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, SelectedDateChanged, CoerceDate));

        public DateTime? SelectedDate
        {
            get { return (DateTime?)GetValue(SelectedDateProperty); }
            set { SetValue(SelectedDateProperty, value); }
        }

        private static object CoerceDate(DependencyObject d, object value)
        {
            if (value != null && !(value is DateTime))
                value = Convert.ToDateTime(value);
            return value;
        }

        private static void SelectedDateChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var picker = (DatePicker)sender;
            if (e.NewValue == null)
                picker.SetCalendarDate(DateTime.Today);
            else
                picker.SetCalendarDate((DateTime)e.NewValue);
        }

        private void SetCalendarDate(DateTime date)
        {
            popupCalendar.SelectedMonth = date.Month;
            popupCalendar.SelectedYear = date.Year;
            popupCalendar.SelectedDay = date.Day;
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
