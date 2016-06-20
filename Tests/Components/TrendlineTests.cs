using System;
using Jamiras.Components;
using NUnit.Framework;

namespace Jamiras.Core.Tests.Components
{
    [TestFixture]
    public class TrendlineTests
    {
        [Test]
        public void TestLinear()
        {
            // y = 2x + 1
            double[] x = new[] { 1.0, 2.0, 3.0 };
            double[] y = new[] { 3.0, 5.0, 7.0 };

            var trendline = new Trendline(x, y);

            Assert.That(trendline.GetX(0.0), Is.EqualTo(-0.5));
            Assert.That(trendline.GetY(0.0), Is.EqualTo(1.0));
            Assert.That(trendline.GetY(137.23), Is.EqualTo(275.46));
        }

        [Test]
        public void TestExample()
        {
            double[] x = new[] { 1.0, 2.0, 3.0 };
            double[] y = new[] { 3.0, 5.0, 6.5 };

            var trendline = new Trendline(x, y);

            Assert.That(Math.Round(trendline.GetY(0.0), 3), Is.EqualTo(1.333));
            Assert.That(Math.Round(trendline.GetY(3.14), 3), Is.EqualTo(6.828));
        }
    }
}
