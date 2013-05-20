using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Jamiras.Commands;
using Jamiras.ViewModels;

namespace Jamiras.Controls
{
    /// <summary>
    /// Interaction logic for CalendarControl.xaml
    /// </summary>
    public partial class CalendarControl : UserControl
    {
        public CalendarControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty SelectedYearProperty = DependencyProperty.Register("SelectedYear",
            typeof(int), typeof(CalendarControl), new FrameworkPropertyMetadata(SelectedYearChanged));

        public int SelectedYear
        {
            get { return (int)GetValue(SelectedYearProperty); }
            set { SetValue(SelectedYearProperty, value); }
        }

        private static void SelectedYearChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((CalendarControl)sender).UpdateMonth();
        }

        public static readonly DependencyProperty SelectedMonthProperty = DependencyProperty.Register("SelectedMonth",
            typeof(int), typeof(CalendarControl), new FrameworkPropertyMetadata(SelectedMonthChanged, CoerceMonth));

        public int SelectedMonth
        {
            get { return (int)GetValue(SelectedMonthProperty); }
            set { SetValue(SelectedMonthProperty, value); }
        }

        private static object CoerceMonth(DependencyObject d, object value)
        {
            if (!(value is int))
                value = Convert.ToInt32(value);

            if ((int)value < 1)
                return 1;
            if ((int)value > 12)
                return 12;            
            return value;
        }

        private static void SelectedMonthChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((CalendarControl)sender).UpdateMonth();
        }

        private void UpdateMonth()
        {
            if (SelectedYear > 0 && SelectedMonth > 0)
            {
                var first = new DateTime(SelectedYear, SelectedMonth, 1);
                var days = new List<CalendarDay>();

                Debug.Assert((int)DayOfWeek.Sunday == 0);
                for (int i = 0; i < (int)first.DayOfWeek; i++)
                    days.Add(new CalendarDay(0));

                for (int i = 1; i <= GetDaysInMonth(); i++)
                    days.Add(new CalendarDay(i));

                while ((days.Count % 7) != 0)
                    days.Add(new CalendarDay(0));

                if (SelectedDay != 0)
                {
                    var day = days.FirstOrDefault(d => d.Day == SelectedDay);
                    if (day != null)
                        day.IsSelected = true;
                    else
                        SelectedDay = 0;
                }

                CalendarDays = days;

                switch (SelectedMonth)
                {
                    case 1: MonthLabel = "January"; break;
                    case 2: MonthLabel = "February"; break;
                    case 3: MonthLabel = "March"; break;
                    case 4: MonthLabel = "April"; break;
                    case 5: MonthLabel = "May"; break;
                    case 6: MonthLabel = "June"; break;
                    case 7: MonthLabel = "July"; break;
                    case 8: MonthLabel = "August"; break;
                    case 9: MonthLabel = "September"; break;
                    case 10: MonthLabel = "October"; break;
                    case 11: MonthLabel = "November"; break;
                    case 12: MonthLabel = "December"; break;
                }
            }
        }

        private int GetDaysInMonth()
        {
            switch (SelectedMonth)
            {
                case 1: // January
                case 3: // March
                case 5: // May
                case 7: // July
                case 8: // August
                case 10: // October
                case 12: // December
                    return 31;

                case 4: // April
                case 6: // June
                case 9: // September
                case 11: // November
                    return 30;

                case 2: // February
                    int year = SelectedYear;
                    if ((year % 4) == 0 && (year % 100) != 0)
                        return 29;
                    return 28;

                default:
                    throw new InvalidOperationException(SelectedMonth + " is not a valid month");
            }
        }

        public static readonly DependencyProperty SelectedDayProperty = DependencyProperty.Register("SelectedDay",
            typeof(int), typeof(CalendarControl), new FrameworkPropertyMetadata(SelectedDayChanged, CoerceDay));

        public int SelectedDay
        {
            get { return (int)GetValue(SelectedDayProperty); }
            set { SetValue(SelectedDayProperty, value); }
        }

        private static object CoerceDay(DependencyObject d, object value)
        {
            if (!(value is int))
                value = Convert.ToInt32(value);

            if ((int)value < 1)
                return 1;
            if ((int)value > 31)
                return 31;
            return value;
        }

        private static void SelectedDayChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var days = ((CalendarControl)sender).CalendarDays;
            if (days != null)
            {
                var day = days.FirstOrDefault(d => d.IsSelected);
                if (day != null)
                    day.IsSelected = false;

                int newValue = (int)e.NewValue;
                if (newValue > 0)
                {
                    day = days.FirstOrDefault(d => d.Day == newValue);
                    if (day != null)
                        day.IsSelected = true;
                }
            }
        }

        private static readonly DependencyPropertyKey MonthLabelPropertyKey = DependencyProperty.RegisterReadOnly("MonthLabel",
            typeof(string), typeof(CalendarControl), new PropertyMetadata());
        public static readonly DependencyProperty MonthLabelProperty = MonthLabelPropertyKey.DependencyProperty;

        public string MonthLabel
        {
            get { return (string)GetValue(MonthLabelProperty); }
            private set { SetValue(MonthLabelPropertyKey, value); }
        }

        private static readonly DependencyPropertyKey CalendarDaysPropertyKey = DependencyProperty.RegisterReadOnly("CalendarDays",
            typeof(IEnumerable<CalendarDay>), typeof(CalendarControl), new PropertyMetadata());
        public static readonly DependencyProperty CalendarDaysProperty = CalendarDaysPropertyKey.DependencyProperty;

        public IEnumerable<CalendarDay> CalendarDays
        {
            get { return (IEnumerable<CalendarDay>)GetValue(CalendarDaysProperty); }
            private set { SetValue(CalendarDaysPropertyKey, value); }
        }

        [DebuggerDisplay("{Day}")]
        public class CalendarDay : ViewModelBase
        {
            public CalendarDay(int day)
            {
                Day = day;
            }

            public int Day { get; private set; }

            public bool IsSelected
            {
                get { return _isSelected; }
                set
                {
                    if (_isSelected != value)
                    {
                        _isSelected = value;
                        OnPropertyChanged(() => IsSelected);
                    }
                }
            }
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private bool _isSelected;
        }

        private void DayMouseClick(object sender, MouseButtonEventArgs e)
        {
            var day = (CalendarDay)((ContentControl)sender).DataContext;
            if (day.Day != 0)
            {
                SelectedDay = day.Day;
                OnDateClicked(EventArgs.Empty);
            }
        }

        public event EventHandler DateClicked;
        protected virtual void OnDateClicked(EventArgs e)
        {
            var handler = DateClicked;
            if (handler != null)
                handler(this, e);
        }

        public static readonly DependencyProperty ShowPreviousNextButtonsProperty = DependencyProperty.Register("ShowPreviousNextButtons",
            typeof(bool), typeof(CalendarControl), new PropertyMetadata(true));

        public bool ShowPreviousNextButtons
        {
            get { return (bool)GetValue(ShowPreviousNextButtonsProperty); }
            set { SetValue(ShowPreviousNextButtonsProperty, value); }
        }

        public ICommand PreviousMonthCommand 
        {
            get { return new DelegateCommand(PreviousMonth); }
        }

        public void PreviousMonth()
        {
            if (SelectedMonth == 1)
            {
                SelectedMonth = 12;
                SelectedYear--;
            }
            else
            {
                SelectedMonth--;
            }
        }

        public ICommand NextMonthCommand
        {
            get { return new DelegateCommand(NextMonth); }
        }

        public void NextMonth()
        {
            if (SelectedMonth == 12)
            {
                SelectedMonth = 1;
                SelectedYear++;
            }
            else
            {
                SelectedMonth++;
            }
        }
    }
}
