using System;
using System.Collections.Generic;
using NUnit.Framework;
using Jamiras.Components;

namespace Jamiras.Core.Tests.Components
{
    [TestFixture]
    public class SoftwareVersionTests
    {
        [Test]
        public void TestMajorMinorConstructor()
        {
            var ver = new SoftwareVersion(1, 3);
            Assert.That(ver.Major, Is.EqualTo(1));
            Assert.That(ver.Minor, Is.EqualTo(3));
            Assert.That(ver.Patch, Is.EqualTo(0));
            Assert.That(ver.Revision, Is.EqualTo(0));
            Assert.That(ver.ToString(), Is.EqualTo("1.3"));
        }

        [Test]
        public void TestMajorMinorPatchConstructor()
        {
            var ver = new SoftwareVersion(3, 10, 4);
            Assert.That(ver.Major, Is.EqualTo(3));
            Assert.That(ver.Minor, Is.EqualTo(10));
            Assert.That(ver.Patch, Is.EqualTo(4));
            Assert.That(ver.Revision, Is.EqualTo(0));
            Assert.That(ver.ToString(), Is.EqualTo("3.10.4"));
        }

        [Test]
        public void TestMajorMinorPatchRevisionConstructor()
        {
            var ver = new SoftwareVersion(12, 6, 0, 12345);
            Assert.That(ver.Major, Is.EqualTo(12));
            Assert.That(ver.Minor, Is.EqualTo(6));
            Assert.That(ver.Patch, Is.EqualTo(0));
            Assert.That(ver.Revision, Is.EqualTo(12345));
            Assert.That(ver.ToString(), Is.EqualTo("12.6.0.12345"));
        }

        [Test]
        [TestCase("0.1", "1.0", '<')]
        [TestCase("1.0", "1.0", '=')]
        [TestCase("1.0", "1.0.0", '=')]
        [TestCase("1.0", "1.0.0.0", '=')]
        [TestCase("1.0", "1.0.1", '<')]
        [TestCase("1.0.0.1", "1.0.1", '<')]
        [TestCase("1.1", "1.0.1", '>')]
        [TestCase("2.0", "1.0.1", '>')]
        [TestCase("1.10.6", "11.0.6", '<')]
        [TestCase("5.11.7", "5.1.17", '>')]
        public void TestComparison(string ver1, string ver2, char expected)
        {
            SoftwareVersion version1, version2;
            Assert.That(SoftwareVersion.TryParse(ver1, out version1), Is.True);
            Assert.That(SoftwareVersion.TryParse(ver2, out version2), Is.True);

            switch (expected)
            {
                case '=':
                    Assert.That(version1, Is.EqualTo(version2));
                    Assert.That(version1 == version2, Is.True);
                    Assert.That(version1 != version2, Is.False);
                    Assert.That(version1 < version2, Is.False);
                    Assert.That(version1 > version2, Is.False);
                    Assert.That(version1 <= version2, Is.True);
                    Assert.That(version1 >= version2, Is.True);
                    break;

                case '<':
                    Assert.That(version1, Is.Not.EqualTo(version2));
                    Assert.That(version1 == version2, Is.False);
                    Assert.That(version1 != version2, Is.True);
                    Assert.That(version1 < version2, Is.True);
                    Assert.That(version1 > version2, Is.False);
                    Assert.That(version1 <= version2, Is.True);
                    Assert.That(version1 >= version2, Is.False);
                    break;

                case '>':
                    Assert.That(version1, Is.Not.EqualTo(version2));
                    Assert.That(version1 == version2, Is.False);
                    Assert.That(version1 != version2, Is.True);
                    Assert.That(version1 < version2, Is.False);
                    Assert.That(version1 > version2, Is.True);
                    Assert.That(version1 <= version2, Is.False);
                    Assert.That(version1 >= version2, Is.True);
                    break;
            }
        }

        [Test]
        [TestCase("1")]
        [TestCase("1.2a")]
        [TestCase("1.a")]
        [TestCase("a.2")]
        [TestCase("1.2.3a")]
        [TestCase("1.2.a")]
        [TestCase("1.2.3.4a")]
        [TestCase("1.2.3.a")]
        [TestCase("1.2.3.4.a")]
        [TestCase("1.2.3.4.5")]
        [TestCase("-1.2")]
        [TestCase("1.-2")]
        public void TestParseFailure(string input)
        {
            SoftwareVersion version;
            Assert.That(SoftwareVersion.TryParse(input, out version), Is.False);
        }
    }
}
