using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Jamiras.Components;

namespace Jamiras.Core.Tests.Components
{
    [TestFixture]
    class SortFunctionsTests
    {
        [Test]
        [TestCase(null, null, 0)]
        [TestCase(null, "", -1)]
        [TestCase("", null, 1)]
        [TestCase("", "", 0)]
        [TestCase("a", "a", 0)]
        [TestCase("a", "A", 0)]
        [TestCase("A", "a", 0)]
        [TestCase("a", "ab", -1)]
        [TestCase("ab", "a", 1)]
        [TestCase("Ab", "aB", 0)]
        [TestCase("a", "b", -1)]
        [TestCase("A", "b", -1)]
        [TestCase("a", "B", -1)]
        [TestCase("1", "2", -1)]
        [TestCase("1", "10", -1)]
        [TestCase("2", "10", -1)]
        public void TestNumericStringCaseInsensitiveCompare(string left, string right, int expected)
        {
            int result = SortFunctions.NumericStringCaseInsensitiveCompare(left, right);

            if (expected == 0)
                Assert.That(result, Is.EqualTo(0), "expected equal strings");
            else if (expected < 0)
                Assert.That(result, Is.LessThan(0), "expected left first");
            else
                Assert.That(result, Is.GreaterThan(0), "expected right first");
        }
    }
}
