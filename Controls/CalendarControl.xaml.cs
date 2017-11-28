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
        /// <summary>
        /// Initializes a new instance of the <see cref="CalendarControl"/> class.
        /// </summary>
        public CalendarControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// <see cref="DependencyProperty"/> for <see cref="SelectedYear"/>
        /// </summary>
        public static readonly DependencyProperty SelectedYearProperty = DependencyProperty.Register("SelectedYear",
            typeof(int), typeof(CalendarControl), new FrameworkPropertyMetadata(SelectedYearChanged));

        /// <summary>
        /// Gets or sets the selected year.
        /// </summary>
        public int SelectedYear
        {
            get { return (int)GetValue(SelectedYearProperty); }
            set { SetValue(SelectedYearProperty, value); }
        }

        private static void SelectedYearChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((CalendarControl)sender).UpdateMonth();
        }

        /// <summary>
        /// <see cref="DependencyProperty"/> for <see cref="SelectedMonth"/>
        /// </summary>
        public static readonly DependencyProperty SelectedMonthProperty = DependencyProperty.Register("SelectedMonth",
            typeof(int), typeof(CalendarControl), new FrameworkPropertyMetadata(SelectedMonthChanged, CoerceMonth));

        /// <summary>
        /// Gets or sets the selected month.
        /// </summary>
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
            if (SelectedYear > 1 && SelectedMonth > 0)
            {
                var date = new DateTime(SelectedYear, SelectedMonth, 1);
                var days = new List<CalendarDay>();

                while (date.DayOfWeek != DayOfWeek.Sunday)
                    date = date.AddDays(-1);

                for (int i = 0; i < 42; i++)
                {
                    CalendarDay day = new CalendarDay(date, (date.Month == SelectedMonth));
                    days.Add(day);

                    date = date.AddDays(1);
                }

                if (SelectedDay != 0)
                {
                    int daysInMonth = GetDaysInMonth(SelectedMonth, SelectedYear);
                    if (SelectedDay > daysInMonth)
                        SelectedDay = daysInMonth;

                    date = new DateTime(SelectedYear, SelectedMonth, SelectedDay);
                    var day = days.FirstOrDefault(d => d.Date == date);
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
            return GetDaysInMonth(SelectedMonth, SelectedYear);
        }

        private static int GetDaysInMonth(int month, int year)
        {
            switch (month)
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
                    if ((year % 4) == 0 && (year % 100) != 0)
                        return 29;
                    return 28;

                default:
                    throw new InvalidOperationException(month + " is not a valid month");
            }
        }

        /// <summary>
        /// <see cref="DependencyProperty"/> for <see cref="SelectedDay"/>
        /// </summary>
        public static readonly DependencyProperty SelectedDayProperty = DependencyProperty.Register("SelectedDay",
            typeof(int), typeof(CalendarControl), new FrameworkPropertyMetadata(SelectedDayChanged, CoerceDay));

        /// <summary>
        /// Gets or sets the selected day.
        /// </summary>
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
                    day = days.FirstOrDefault(d => d.Day == newValue && d.IsInSelectedMonth);
                    if (day != null)
                        day.IsSelected = true;
                }
            }
        }

        private static readonly DependencyPropertyKey MonthLabelPropertyKey = DependencyProperty.RegisterReadOnly("MonthLabel",
            typeof(string), typeof(CalendarControl), new PropertyMetadata());

        /// <summary>
        /// <see cref="DependencyProperty"/> for <see cref="MonthLabel"/>
        /// </summary>
        public static readonly DependencyProperty MonthLabelProperty = MonthLabelPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the label for the <see cref="SelectedMonth"/>.
        /// </summary>
        public string MonthLabel
        {
            get { return (string)GetValue(MonthLabelProperty); }
            private set { SetValue(MonthLabelPropertyKey, value); }
        }

        private static readonly DependencyPropertyKey CalendarDaysPropertyKey = DependencyProperty.RegisterReadOnly("CalendarDays",
            typeof(IEnumerable<CalendarDay>), typeof(CalendarControl), new PropertyMetadata());

        /// <summary>
        /// <see cref="DependencyProperty"/> for <see cref="CalendarDays"/>
        /// </summary>
        public static readonly DependencyProperty CalendarDaysProperty = CalendarDaysPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the collection of <see cref="CalendarDay"/>s for the selected month. Includes last few days of previous month and/or the 
        /// next few days of the next month to ensure the calendar is fully populated.
        /// </summary>
        public IEnumerable<CalendarDay> CalendarDays
        {
            get { return (IEnumerable<CalendarDay>)GetValue(CalendarDaysProperty); }
            private set { SetValue(CalendarDaysPropertyKey, value); }
        }

        /// <summary>
        /// Represents a single cell in the calendar.
        /// </summary>
        [DebuggerDisplay("{Day}")]
        public class CalendarDay : ViewModelBase
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="CalendarDay"/> class.
            /// </summary>
            /// <param name="date">The date represented by the cell.</param>
            /// <param name="isInSelectedMonth"><c>true</c> if the date is part of the selected month, <c>false</c> if it's from the previous or next month.</param>
            public CalendarDay(DateTime date, bool isInSelectedMonth)
            {
                Date = date;
                Day = date.Day;
                IsInSelectedMonth = isInSelectedMonth;
            }

            /// <summary>
            /// Gets the date represented by the cell.
            /// </summary>
            public DateTime Date { get; private set; }

            /// <summary>
            /// Gets the number to display in the cell.
            /// </summary>
            public int Day { get; private set; }

            /// <summary>
            /// Gets whether the cell represents a day in the current month.
            /// </summary>
            /// <value>
            /// <c>true</c> if the date is part of the selected month, <c>false</c> if it's from the previous or next month.
            /// </value>
            public bool IsInSelectedMonth { get; private set; }

            /// <summary>
            /// Gets or sets a value whether the cell is selected.
            /// </summary>
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
                if (!day.IsInSelectedMonth)
                {
                    SelectedYear = day.Date.Year;
                    SelectedMonth = day.Date.Month;
                }
                SelectedDay = day.Day;
                OnDateClicked(EventArgs.Empty);
            }
        }

        /// <summary>
        /// Raised when a cell is clicked.
        /// </summary>
        public event EventHandler DateClicked;

        /// <summary>
        /// Raises the <see cref="E:DateClicked" /> event.
        /// </summary>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected virtual void OnDateClicked(EventArgs e)
        {
            var handler = DateClicked;
            if (handler != null)
                handler(this, e);
        }

        /// <summary>
        /// <see cref="DependencyProperty"/> for <see cref="ShowPreviousNextButtons"/>
        /// </summary>
        public static readonly DependencyProperty ShowPreviousNextButtonsProperty = DependencyProperty.Register("ShowPreviousNextButtons",
            typeof(bool), typeof(CalendarControl), new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets whether the previous and next month buttons should be visible.
        /// </summary>
        public bool ShowPreviousNextButtons
        {
            get { return (bool)GetValue(ShowPreviousNextButtonsProperty); }
            set { SetValue(ShowPreviousNextButtonsProperty, value); }
        }

        /// <summary>
        /// A bindable command for changing the calendar to the previous month.
        /// </summary>
        public ICommand PreviousMonthCommand 
        {
            get { return new DelegateCommand(PreviousMonth); }
        }

        /// <summary>
        /// Changes the calendar to the previous month.
        /// </summary>
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

        /// <summary>
        /// A bindable command for changing the calendar to the next month.
        /// </summary>
        public ICommand NextMonthCommand
        {
            get { return new DelegateCommand(NextMonth); }
        }

        /// <summary>
        /// Changes the calendar to the next month.
        /// </summary>
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
