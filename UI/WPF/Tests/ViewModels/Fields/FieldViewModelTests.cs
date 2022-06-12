using System.Collections.Generic;
using Jamiras.ViewModels.Fields;
using NUnit.Framework;

namespace Jamiras.UI.WPF.Tests.ViewModels.Fields
{
    [TestFixture]
    class FieldViewModelTests
    {
        private class TestFieldViewModel : FieldViewModelBase
        {
            public string GetLabelWithoutAccelerators()
            {
                return LabelWithoutAccelerators;
            }
        }

        [SetUp]
        public void Setup()
        {
            _viewModel = new TestFieldViewModel();
        }

        private TestFieldViewModel _viewModel;

        [Test]
        public void TestIsRequired()
        {
            Assert.That(_viewModel.IsRequired, Is.False);

            var propertiesChanged = new List<string>();
            _viewModel.PropertyChanged += (o, e) => propertiesChanged.Add(e.PropertyName);

            _viewModel.IsRequired = false;
            Assert.That(_viewModel.IsRequired, Is.False);
            Assert.That(propertiesChanged, Is.Empty);

            _viewModel.IsRequired = true;
            Assert.That(_viewModel.IsRequired, Is.True);
            Assert.That(propertiesChanged, Has.Member("IsRequired"));
        }

        [Test]
        public void TestLabel()
        {
            Assert.That(_viewModel.Label, Is.EqualTo("Value"));

            var propertiesChanged = new List<string>();
            _viewModel.PropertyChanged += (o, e) => propertiesChanged.Add(e.PropertyName);

            _viewModel.Label = "Value";
            Assert.That(_viewModel.Label, Is.EqualTo("Value"));
            Assert.That(propertiesChanged, Is.Empty);

            _viewModel.Label = "Label";
            Assert.That(_viewModel.Label, Is.EqualTo("Label"));
            Assert.That(propertiesChanged, Has.Member("Label"));
        }

        [Test]
        [TestCase("label", "labelTestField")]
        [TestCase("Th_is is 4 complex_words!", "thisIs4ComplexWordsTestField")]
        [TestCase("ANGRY_BIRDS", "angryBirdsTestField")]
        public void TestNameDerived(string label, string expected)
        {
            Assert.That(_viewModel.Name, Is.EqualTo("valueTestField"));

            var propertiesChanged = new List<string>();
            _viewModel.PropertyChanged += (o, e) => propertiesChanged.Add(e.PropertyName);

            _viewModel.Label = label;
            Assert.That(_viewModel.Name, Is.EqualTo(expected));
            Assert.That(propertiesChanged, Has.Member("Name"));
        }

        [Test]
        public void TestNameCustom()
        {
            Assert.That(_viewModel.Name, Is.EqualTo("valueTestField"));

            var propertiesChanged = new List<string>();
            _viewModel.PropertyChanged += (o, e) => propertiesChanged.Add(e.PropertyName);

            _viewModel.Name = "myField";
            Assert.That(_viewModel.Name, Is.EqualTo("myField"));
            Assert.That(propertiesChanged, Has.Member("Name"));

            propertiesChanged.Clear();
            _viewModel.Label = "label";
            Assert.That(_viewModel.Name, Is.EqualTo("myField"));
            Assert.That(propertiesChanged, Has.No.Member("Name"));
        }

        [Test]
        [TestCase("Simple", "Simple")]
        [TestCase("_Field", "Field")]
        [TestCase("Complex _field", "Complex field")]
        [TestCase("1_2_3_4", "12_3_4")]
        [TestCase("What_", "What")]
        public void TestLabelWithoutAccelerators(string label, string expected)
        {
            _viewModel.Label = label;
            Assert.That(_viewModel.GetLabelWithoutAccelerators(), Is.EqualTo(expected));
        }
    }
}
