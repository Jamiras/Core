using System.Linq;
using Jamiras.Controls;
using NUnit.Framework;

namespace Jamiras.Core.Tests.Controls
{
    [TestFixture]
    [RequiresSTA]
    class CalendarControlTests
    {
        private CalendarControl _control;

        [SetUp]
        public void Setup()
        {
            _control = new CalendarControl();
        }

        [Test]
        [TestCase(-1, 1)]
        [TestCase(0, 1)]
        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(3, 3)]
        [TestCase(4, 4)]
        [TestCase(5, 5)]
        [TestCase(6, 6)]
        [TestCase(7, 7)]
        [TestCase(8, 8)]
        [TestCase(9, 9)]
        [TestCase(10, 10)]
        [TestCase(11, 11)]
        [TestCase(12, 12)]
        [TestCase(13, 12)]
        [TestCase(100, 12)]
        [TestCase(9999, 12)]
        public void TestSetMonth(int value, int expected)
        {
            _control.SelectedMonth = value;
            Assert.That(_control.SelectedMonth, Is.EqualTo(expected));
        }

        [Test]
        [TestCase(1, "January")]
        [TestCase(2, "February")]
        [TestCase(3, "March")]
        [TestCase(4, "April")]
        [TestCase(5, "May")]
        [TestCase(6, "June")]
        [TestCase(7, "July")]
        [TestCase(8, "August")]
        [TestCase(9, "September")]
        [TestCase(10, "October")]
        [TestCase(11, "November")]
        [TestCase(12, "December")]
        public void TestMonthLabel(int month, string label)
        {
            _control.SelectedMonth = month;
            _control.SelectedYear = 2013;
            Assert.That(_control.MonthLabel, Is.EqualTo(label));
        }

        [Test]
        public void TestSetMonthYear()
        {
            _control.SelectedMonth = 5;
            _control.SelectedYear = 2013;

            Assert.That(_control.CalendarDays.Count(), Is.EqualTo(42));
            Assert.That(_control.CalendarDays.ElementAt(0).Day, Is.EqualTo(28));
            Assert.That(_control.CalendarDays.ElementAt(2).Day, Is.EqualTo(30));
            Assert.That(_control.CalendarDays.ElementAt(3).Day, Is.EqualTo(1));
            Assert.That(_control.CalendarDays.ElementAt(33).Day, Is.EqualTo(31));
            Assert.That(_control.CalendarDays.ElementAt(34).Day, Is.EqualTo(1));
            Assert.That(_control.CalendarDays.ElementAt(41).Day, Is.EqualTo(8));

            Assert.That(_control.CalendarDays.ElementAt(2).IsInSelectedMonth, Is.False);
            Assert.That(_control.CalendarDays.ElementAt(15).IsInSelectedMonth, Is.True);
            Assert.That(_control.CalendarDays.ElementAt(34).IsInSelectedMonth, Is.False);
        }

        [Test]
        [Description("When changing month, if day 15 was selected, day 15 will be selected in the new month")]
        public void SelectedDay()
        {
            _control.SelectedMonth = 5;
            _control.SelectedYear = 2013;
            _control.SelectedDay = 15;
            Assert.That(_control.SelectedDay, Is.EqualTo(15));
            var day = _control.CalendarDays.FirstOrDefault(d => d.Day == 15);
            Assert.That(day, Is.Not.Null);
            Assert.That(day.IsSelected, Is.True);

            _control.SelectedMonth = 6;
            Assert.That(_control.SelectedDay, Is.EqualTo(15));
            day = _control.CalendarDays.FirstOrDefault(d => d.Day == 15);
            Assert.That(day, Is.Not.Null);
            Assert.That(day.IsSelected, Is.True);

            _control.SelectedMonth = 5;
            Assert.That(_control.SelectedDay, Is.EqualTo(15));
            day = _control.CalendarDays.FirstOrDefault(d => d.Day == 15);
            Assert.That(day, Is.Not.Null);
            Assert.That(day.IsSelected, Is.True);
        }

        [Test]
        public void TestPreviousMonthCommand()
        {
            _control.SelectedMonth = 5;
            _control.SelectedYear = 2013;

            _control.PreviousMonthCommand.Execute(null);
            Assert.That(_control.SelectedMonth, Is.EqualTo(4));
            Assert.That(_control.SelectedYear, Is.EqualTo(2013));
        }

        [Test]
        public void TestPreviousMonthJanuaryCommand()
        {
            _control.SelectedMonth = 1;
            _control.SelectedYear = 2013;

            _control.PreviousMonthCommand.Execute(null);
            Assert.That(_control.SelectedMonth, Is.EqualTo(12));
            Assert.That(_control.SelectedYear, Is.EqualTo(2012));
        }

        [Test]
        public void TestNextMonthCommand()
        {
            _control.SelectedMonth = 5;
            _control.SelectedYear = 2013;

            _control.NextMonthCommand.Execute(null);
            Assert.That(_control.SelectedMonth, Is.EqualTo(6));
            Assert.That(_control.SelectedYear, Is.EqualTo(2013));
        }

        [Test]
        public void TestNextMonthDecemberCommand()
        {
            _control.SelectedMonth = 12;
            _control.SelectedYear = 2013;

            _control.NextMonthCommand.Execute(null);
            Assert.That(_control.SelectedMonth, Is.EqualTo(1));
            Assert.That(_control.SelectedYear, Is.EqualTo(2014));
        }
    }
}
