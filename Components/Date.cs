using System;
using System.Text;

namespace Jamiras.Components
{
    /// <summary>
    /// Defines a structure that can be used to represent dates where one or more parts may be unknown (such as a "Month Year" or "Month Day" value).
    /// </summary>
    public struct Date
    {
        public Date(int month, int day, int year)
        {
            if (month < 0 || month > 12)
                throw new ArgumentException("month must be between 0 and 12", "month");
            if (day < 0 || day > 31)
                throw new ArgumentException("day must be between 0 and 31", "day");

            _value = (year << 16) | (month << 8) | day;
        }

        private readonly int _value;

        /// <summary>
        /// Gets the month value of the date (0 is unset).
        /// </summary>
        public int Month 
        {
            get { return (_value >> 8) & 0xFF; }
        }

        /// <summary>
        /// Gets the day value of the date (0 is unset).
        /// </summary>
        public int Day
        {
            get { return (_value & 0xFF); }
        }

        /// <summary>
        /// Gets the year value of the date (0 is unset).
        /// </summary>
        public int Year
        {
            get { return (_value >> 16); }
        }

        /// <summary>
        /// Gets a <see cref="Date"/> representing an unset value.
        /// </summary>
        public static Date Empty
        {
            get { return new Date(); }
        }

        /// <summary>
        /// Determines if this <see cref="Date"/> is unset.
        /// </summary>
        public bool IsEmpty 
        {
            get { return (_value == 0); }
        }

        private static readonly string[] _months = new string[]
        {
            "",
            "Jan",
            "Feb",
            "Mar",
            "Apr",
            "May",
            "Jun",
            "Jul",
            "Aug",
            "Sep",
            "Oct",
            "Nov",
            "Dec"
        };

        /// <summary>
        /// Gets the value of the <see cref="Date"/> in the standard format "[d] [MMM] [YYYY]" (i.e. "Jan 1946", "6 Dec", "12 Nov 2005")
        /// </summary>
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            if (Day > 0)
            {
                builder.Append(Day);
            }

            if (Month > 0)
            {
                if (builder.Length > 0)
                    builder.Append(' ');

                builder.Append(_months[Month]);
            }

            if (Year > 0)
            {
                if (builder.Length > 0)
                    builder.Append(' ');

                builder.Append(Year);
            }

            if (builder.Length == 0)
                return "Unknown";

            return builder.ToString();
        }

        /// <summary>
        /// Gets the value of the <see cref="Date"/> as a standardized string for storage in an external data source.
        /// </summary>
        public string ToDataString()
        {
            return String.Format("{0:D4}/{1:D2}/{2:D2}", Year, Month, Day);
        }

        /// <summary>
        /// Attempts to convert a string into a <see cref="Date"/>. Supported formats are "MM/DD/YYYY" (system), "YYYY/MM/DD" (<see cref="ToDataString"/>), and  "[d] [MMM] [YYYY]" (<see cref="ToString"/>).
        /// </summary>
        /// <param name="input">The string to parse.</param>
        /// <param name="date">The date to populate.</param>
        /// <returns><c>true</c> if the date was populated, <c>false</c> if not.</returns>
        public static bool TryParse(string input, out Date date)
        {
            if (String.IsNullOrEmpty(input))
            {
                date = Empty;
                return false;
            }

            int month = 0, day = 0, year = 0, val;
            bool isValid = true;

            string[] parts = input.Split('/');
            if (parts.Length == 3)
            {
                if (parts[0].Length == 4 && parts[1].Length == 2 && parts[2].Length == 2)
                {
                    // data format YYYY/MM/DD - may contain zeros
                    if (!Int32.TryParse(parts[0], out year) || !Int32.TryParse(parts[1], out month) || !Int32.TryParse(parts[2], out day))
                        isValid = false;
                }
                else
                {
                    // three parts - assume MM/DD/YYYY or MM/DD/YY
                    if (parts[0].Length > 2 || parts[1].Length > 2 || parts[2].Length > 4)
                    {
                        isValid = false;
                    }
                    else
                    {
                        // system format MM/DD/YYYY - may not contains zeros
                        if (!Int32.TryParse(parts[0], out month) || month == 0 || month > 12 ||
                            !Int32.TryParse(parts[1], out day) || day == 0 || day > 31 ||
                            !Int32.TryParse(parts[2], out year) || year == 0)
                        {
                            isValid = false;
                        }
                        else if (parts[2].Length <= 2)
                        {
                            year += 2000;
                            if (year > DateTime.Today.Year)
                                year -= 100;
                        }
                    }
                }
            }
            else if (parts.Length > 1)
            {
                // two parts - assume MM/DD or MM/YYYY
                if (parts[0].Length > 2 || parts[1].Length > 4)
                    isValid = false;
                else if (!Int32.TryParse(parts[0], out month) || !Int32.TryParse(parts[1], out val))
                    isValid = false;
                else if (val < 32)
                    day = val;
                else
                    year = val;
            }
            else // ToString format
            {
                parts = input.Split(' ', ',');
                foreach (string part in parts)
                {
                    if (part.Length == 4)
                    {
                        if (Int32.TryParse(part, out val))
                            year = val;
                        else
                            isValid = false;
                    }
                    else if (part.Length == 3)
                    {
                        for (int i = 1; i <= 12; i++)
                        {
                            if (String.Compare(_months[i], part, StringComparison.OrdinalIgnoreCase) == 0)
                            {
                                month = i;
                                break;
                            }
                        }

                        if (month == 0)
                            isValid = false;
                    }
                    else if (part.Length > 4)
                    {
                        isValid = false;
                    }
                    else if (part.Length > 0)
                    {
                        if (Int32.TryParse(part, out val) && val > 0 && val < 32)
                            day = val;
                        else
                            isValid = false;
                    }
                }

                if (year == 0 && month == 0 && day == 0)
                    isValid = false;
            }

            if (isValid)
            {
                date = new Date(month, day, year);
                return true;
            }

            date = Empty;
            return false;
        }

        /// <summary>
        /// Gets the number of whole years that have elapsed between two <see cref="Date"/>s.
        /// </summary>
        /// <param name="compare">The value to compare against.</param>
        /// <returns>The number of whole years that have elapsed between the two <see cref="Date"/>s.</returns>
        public int GetElapsedYears(Date compare)
        {
            if (Year < compare.Year)
                return compare.GetElapsedYears(this);

            int age = Year - compare.Year;
            if (age < 0)
                return 0;

            if (Month < compare.Month)
                return age - 1;

            if (Month > compare.Month)
                return age;

            if (Day < compare.Day)
                return age - 1;

            return age;
        }

        /// <summary>
        /// Trys to converts the <see cref="Date"/> to a <see cref="DateTime"/>.
        /// </summary>
        /// <param name="dateTime">The <see cref="DateTime"/> to initialize.</param>
        /// <returns><c>true</c> if the conversion was successful, <c>false</c> if not.</returns>
        /// <remarks>The <see cref="Month"/>, <see cref="Day"/>, and <see cref="Year"/> properties must be non-zero for this to succeed.</remarks>
        public bool TryConvert(out DateTime dateTime)
        {
            if (Day == 0 || Month == 0 || Year == 0)
            {
                dateTime = default(DateTime);
                return false;
            }

            dateTime = new DateTime(Year, Month, Day);
            return true;
        }

        /// <summary>
        /// Helper method to pass to Sort functions.
        /// </summary>
        public static int SortByDate(Date a, Date b)
        {
            return (a._value - b._value);
        }

        public static bool operator >(Date left, Date right)
        {
            return left._value > right._value;
        }

        public static bool operator <(Date left, Date right)
        {
            return left._value < right._value;
        }

        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        /// <returns>
        /// true if <paramref name="obj"/> and this instance are the same type and represent the same value; otherwise, false.
        /// </returns>
        /// <param name="obj">Another object to compare to. </param>
        /// <filterpriority>2</filterpriority>
        public override bool Equals(object obj)
        {
            if (obj is Date)
                return ((Date)obj)._value == _value;

            if (obj is DateTime)
            {
                var dttm = (DateTime)obj;
                return (dttm.Year == Year && dttm.Month == Month && dttm.Day == Day);
            }

            return false;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer that is the hash code for this instance.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override int GetHashCode()
        {
            return _value;
        }
    }
}
