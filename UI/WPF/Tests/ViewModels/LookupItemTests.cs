using System.Collections.Generic;
using NUnit.Framework;
using Jamiras.ViewModels;
using System.ComponentModel;

namespace Jamiras.UI.WPF.Tests.ViewModels
{
    [TestFixture]
    class LookupItemTests
    {
        [Test]
        public void TestInterfaces()
        {
            var item = new LookupItem(1, "Test");
            Assert.That(item, Is.InstanceOf<INotifyPropertyChanged>());
        }

        [Test]
        public void TestInitialization()
        {
            var item = new LookupItem(1, "Test");
            Assert.That(item.Id, Is.EqualTo(1));
            Assert.That(item.Label, Is.EqualTo("Test"));
            Assert.That(item.IsSelected, Is.False);
        }

        [Test]
        public void TestIsSelected()
        {
            var item = new LookupItem(1, "Test");
            Assert.That(item.IsSelected, Is.False);

            List<string> propertiesChanged = new List<string>();
            item.PropertyChanged += (o, e) => propertiesChanged.Add(e.PropertyName);

            item.IsSelected = false;
            Assert.That(propertiesChanged, Has.No.Member("IsSelected"));

            item.IsSelected = true;
            Assert.That(propertiesChanged, Contains.Item("IsSelected"));
        }
    }
}
