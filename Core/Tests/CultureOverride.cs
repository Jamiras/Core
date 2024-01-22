using NUnit.Framework;
using System;
using System.Globalization;

namespace Jamiras.Core.Tests
{
    public class CultureOverride : IDisposable
    {
        public CultureOverride(string name)
        {
            _oldCulture = CultureInfo.CurrentCulture;

            CultureInfo.CurrentCulture = CultureInfo.CreateSpecificCulture(name);

            Assert.AreEqual(name, CultureInfo.CurrentCulture.Name);
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _oldCulture;
            GC.SuppressFinalize(this);
        }

        private readonly CultureInfo _oldCulture;
    }

    [TestFixture]
    class CultureOverrideTests
    {
        [Test]
        public void TestOverride()
        {
            var oldCulture = CultureInfo.CurrentCulture;

            using (var cultureOverride = new CultureOverride("fr-FR"))
            {
                Assert.AreEqual("fr-FR", CultureInfo.CurrentCulture.Name);
                Assert.AreEqual("1,23", String.Format("{0:F2}", 1.23));
            }

            Assert.AreEqual(oldCulture, CultureInfo.CurrentCulture);

            using (var cultureOverride = new CultureOverride("en-US"))
            {
                Assert.AreEqual("en-US", CultureInfo.CurrentCulture.Name);
                Assert.AreEqual("1.23", String.Format("{0:F2}", 1.23));
            }

            Assert.AreEqual(oldCulture, CultureInfo.CurrentCulture);

            using (var cultureOverride = new CultureOverride("de-DE"))
            {
                Assert.AreEqual("de-DE", CultureInfo.CurrentCulture.Name);
                Assert.AreEqual("1,23", String.Format("{0:F2}", 1.23));
            }

            Assert.AreEqual(oldCulture, CultureInfo.CurrentCulture);
        }
    }
}
