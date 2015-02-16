using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Jamiras.Components;

namespace Jamiras.Core.Tests.Components
{
    [TestFixture]
    public class DateTests
    {
        [Test]
        public void TestMonthConstructor()
        {
            for (int i = 0; i <= 12; i++)
            {
                var date = new Date(i, 0, 0);
                Assert.That(date.Month, Is.EqualTo(i));
            }
        }

        [Test]
        public void TestDayConstructor()
        {
            for (int i = 0; i <= 31; i++)
            {
                var date = new Date(0, i, 0);
                Assert.That(date.Day, Is.EqualTo(i));
            }
        }

        [Test]
        public void TestYearConstructor()
        {
            for (int i = 2050; i >= 1900; i--)
            {
                var date = new Date(0, 0, i);
                Assert.That(date.Year, Is.EqualTo(i));
            }

            for (int i = 1895; i >= 1700; i -= 5)
            {
                var date = new Date(0, 0, i);
                Assert.That(date.Year, Is.EqualTo(i));
            }

            for (int i = 1790; i >= 1500; i -= 10)
            {
                var date = new Date(0, 0, i);
                Assert.That(date.Year, Is.EqualTo(i));
            }

            for (int i = 1475; i >= 1000; i -= 25)
            {
                var date = new Date(0, 0, i);
                Assert.That(date.Year, Is.EqualTo(i));
            }

            for (int i = 950; i >= 0; i -= 50)
            {
                var date = new Date(0, 0, i);
                Assert.That(date.Year, Is.EqualTo(i));
            }
        }

        [Test]
        public void TestIsEmpty()
        {
            var date = new Date(0, 0, 0);
            Assert.That(date.IsEmpty, Is.True);

            date = new Date(1, 0, 0);
            Assert.That(date.IsEmpty, Is.False);

            date = new Date(0, 1, 0);
            Assert.That(date.IsEmpty, Is.False);

            date = new Date(0, 0, 2000);
            Assert.That(date.IsEmpty, Is.False);
        }

        [Test]
        public void TestEmpty()
        {
            var date = Date.Empty;
            Assert.That(date.Month, Is.EqualTo(0));
            Assert.That(date.Day, Is.EqualTo(0));
            Assert.That(date.Year, Is.EqualTo(0));
            Assert.That(date.IsEmpty, Is.True);
        }

        [Test]
        [TestCase(0, 0, 0, "Unknown")]
        [TestCase(1, 0, 0, "1")]
        [TestCase(0, 1, 0, "Jan")]
        [TestCase(0, 0, 2000, "2000")]
        [TestCase(0, 2, 1962, "Feb 1962")]
        [TestCase(3, 0, 2008, "3 2008")]
        [TestCase(0, 4, 1975, "Apr 1975")]
        [TestCase(6, 5, 0, "6 May")]
        [TestCase(7, 8, 1955, "7 Aug 1955")]
        public void TestToString(int day, int month, int year, string expected)
        {
            var date = new Date(month, day, year);
            Assert.That(date.ToString(), Is.EqualTo(expected));
        }

        [Test]
        [TestCase(0, 0, 0, "0000/00/00")]
        [TestCase(1, 0, 0, "0000/00/01")]
        [TestCase(0, 1, 0, "0000/01/00")]
        [TestCase(0, 0, 2000, "2000/00/00")]
        [TestCase(0, 2, 1962, "1962/02/00")]
        [TestCase(3, 0, 2008, "2008/00/03")]
        [TestCase(0, 4, 1975, "1975/04/00")]
        [TestCase(6, 5, 0, "0000/05/06")]
        [TestCase(7, 8, 1955, "1955/08/07")]
        public void TestToDataString(int day, int month, int year, string expected)
        {
            var date = new Date(month, day, year);
            Assert.That(date.ToDataString(), Is.EqualTo(expected));
        }

        [Test]
        [TestCase("0000/00/00", 0, 0, 0)]
        [TestCase("0000/00/01", 1, 0, 0)]
        [TestCase("0000/01/00", 0, 1, 0)]
        [TestCase("2000/00/00", 0, 0, 2000)]
        [TestCase("1962/02/00", 0, 2, 1962)]
        [TestCase("2008/00/03", 3, 0, 2008)]
        [TestCase("1975/04/00", 0, 4, 1975)]
        [TestCase("0000/05/06", 6, 5, 0)]
        [TestCase("1955/08/07", 7, 8, 1955)]
        [TestCase("6/4/1980", 4, 6, 1980)] // system format
        [TestCase("6/6/6", 6, 6, 2006)] // system format
        [TestCase("9/17/45", 17, 9, 1945)] // system format, short year
        [TestCase("11/13/12", 13, 11, 2012)] // system format, short year
        [TestCase("8/17", 17, 8, 0)] // system format, no year
        [TestCase("8/1980", 0, 8, 1980)] // system format, no day
        [TestCase("3", 3, 0, 0)] // standard format - day only
        [TestCase("Nov", 0, 11, 0)] // standard format - month only
        [TestCase("1985", 0, 0, 1985)] // standard format - year only
        [TestCase("Jun 1977", 0, 6, 1977)] // standard format - month/year
        [TestCase("6 Mar", 6, 3, 0)] // standard format - day/month
        [TestCase("Oct 11", 11, 10, 0)] // standard format - month/day
        [TestCase("09 Jul", 9, 7, 0)] // standard format - padded day/month
        [TestCase("May 04", 4, 5, 0)] // standard format - month/padded day
        [TestCase("9 Mar 2004", 9, 3, 2004)] // standard format - day/month/year
        [TestCase("Sep 2 1820", 2, 9, 1820)] // standard format - month/day/year
        [TestCase("06 Dec 1900", 6,12, 1900)] // standard format - padded day/month/year
        [TestCase("Apr 08 2015", 8, 4, 2015)] // standard format - month/padded day/year
        [TestCase("2012 Dec 21", 21, 12, 2012)] // standard format - year/month/day
        [TestCase("Mar 4, 1989", 4, 3, 1989)] // standard format - month/day, year
        public void TestTryParseValid(string input, int day, int month, int year)
        {
            Date date;
            Assert.That(Date.TryParse(input, out date), Is.True, "parse failed");
            Assert.That(date.Month, Is.EqualTo(month), "month");
            Assert.That(date.Day, Is.EqualTo(day), "day");
            Assert.That(date.Year, Is.EqualTo(year), "year");
        }

        [Test]
        [TestCase("")]
        [TestCase(null)]
        [TestCase("banana")]
        [TestCase("March 2013")] // only abbreviated months supported
        [TestCase("13/6/2000")] // invalid month
        [TestCase("2/36/1980")] // invalid day
        public void TestTryParseInvalid(string input)
        {
            Date date;
            Assert.That(Date.TryParse(input, out date), Is.False);
            Assert.That(date.IsEmpty, Is.True);
        }

        [Test]
        [TestCase(1, 2, 1987, true)]
        [TestCase(3, 0, 2010, false)]
        [TestCase(0, 8, 1999, false)]
        [TestCase(4, 6, 0, false)]
        [TestCase(0, 0, 1974, false)]
        [TestCase(8, 0, 0, false)]
        [TestCase(0, 11, 0, false)]
        [TestCase(0, 0, 0, false)]
        public void TestTryConvert(int month, int day, int year, bool expectedResult)
        {
            DateTime dateTime;
            Date date = new Date(month, day, year);
            Assert.That(date.TryConvert(out dateTime), Is.EqualTo(expectedResult));

            if (expectedResult)
            {
                Assert.That(dateTime.Month, Is.EqualTo(month));
                Assert.That(dateTime.Day, Is.EqualTo(day));
                Assert.That(dateTime.Year, Is.EqualTo(year));
            }
        }

        [Test]
        [TestCase(6, 8, 1950, 6, 9, 1950, 0)]
        [TestCase(6, 8, 1950, 6, 8, 1951, 1)]
        [TestCase(6, 8, 1950, 6, 8, 1949, 1)]
        [TestCase(6, 8, 1950, 6, 7, 1951, 0)]
        [TestCase(6, 8, 1950, 6, 9, 1951, 1)]
        [TestCase(6, 8, 1950, 6, 9, 1949, 0)]
        [TestCase(6, 8, 1950, 6, 7, 1949, 1)]
        public void TestGetElapsedYears(int month1, int day1, int year1, int month2, int day2, int year2, int expected)
        {
            var date1 = new Date(month1, day1, year1);
            var date2 = new Date(month2, day2, year2);
            Assert.That(date2.GetElapsedYears(date1), Is.EqualTo(expected));
        }

        [Test]
        public void TestSortByDate()
        {
            var list = new Date[]
            {
                new Date(1, 7, 2000), // 0
                new Date(8, 4, 2010), // 1
                new Date(9, 3, 1996), // 2
                new Date(7, 1, 1948), // 3
                new Date(7, 0, 1948), // 4
                new Date(4, 19, 0), // 5
                new Date(4, 19, 1985) // 6
            };

            var list2 = new List<Date>(list);
            list2.Sort(Date.SortByDate);

            Assert.That(list2, Is.EqualTo(new Date[]
            {
                list[5], list[4], list[3], list[6], list[2], list[0], list[1]
            }));
        }
    }
}
