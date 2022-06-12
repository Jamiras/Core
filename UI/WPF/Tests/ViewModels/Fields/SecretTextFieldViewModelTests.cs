using Jamiras.ViewModels.Fields;
using NUnit.Framework;

namespace Jamiras.UI.WPF.Tests.ViewModels.Fields
{
    [TestFixture]
    class SecretTextFieldViewModelTests
    {
        [SetUp]
        public void Setup()
        {
            _viewModel = new SecretTextFieldViewModel("Secret", 16);
        }

        private SecretTextFieldViewModel _viewModel;

        [Test]
        public void TestInitiallyHidden()
        {
            _viewModel.SecretText = "Secret";
            Assert.That(_viewModel.SecretText, Is.EqualTo("Secret"));
            Assert.That(_viewModel.IsUnmasked, Is.False);
            Assert.That(_viewModel.Text, Is.EqualTo(new string(SecretTextFieldViewModel.MaskChar, 6)));
        }

        [Test]
        public void TestChangeWhileHidden()
        {
            _viewModel.SecretText = "Secret";
            Assert.That(_viewModel.IsUnmasked, Is.False);
            Assert.That(_viewModel.SecretText, Is.EqualTo("Secret"));
            Assert.That(_viewModel.Text, Is.EqualTo(new string(SecretTextFieldViewModel.MaskChar, 6)));

            _viewModel.Text = "Secret2";
            Assert.That(_viewModel.SecretText, Is.EqualTo("Secret2"));
            Assert.That(_viewModel.Text, Is.EqualTo(new string(SecretTextFieldViewModel.MaskChar, 7)));

            _viewModel.Text = _viewModel.Text.Substring(0, 5); // delete two dots from the text
            Assert.That(_viewModel.SecretText, Is.EqualTo("Secret2"));
            Assert.That(_viewModel.Text, Is.EqualTo(new string(SecretTextFieldViewModel.MaskChar, 5)));

            _viewModel.Text = ""; // delete entire string
            Assert.That(_viewModel.SecretText, Is.EqualTo(""));
            Assert.That(_viewModel.Text, Is.EqualTo(""));

            _viewModel.Text = "Secret3";
            Assert.That(_viewModel.SecretText, Is.EqualTo("Secret3"));
            Assert.That(_viewModel.Text, Is.EqualTo(new string(SecretTextFieldViewModel.MaskChar, 7)));
        }

        [Test]
        public void TestChangeWhileNotHidden()
        {
            _viewModel.SecretText = "Secret";
            _viewModel.IsUnmasked = true;
            Assert.That(_viewModel.SecretText, Is.EqualTo("Secret"));
            Assert.That(_viewModel.Text, Is.EqualTo("Secret"));
            Assert.That(_viewModel.IsUnmasked, Is.True);

            _viewModel.Text = "Secret2";
            Assert.That(_viewModel.SecretText, Is.EqualTo("Secret2"));
            Assert.That(_viewModel.Text, Is.EqualTo("Secret2"));

            _viewModel.Text = _viewModel.Text.Substring(0, 5); // delete last two characters from the text
            Assert.That(_viewModel.SecretText, Is.EqualTo("Secre"));
            Assert.That(_viewModel.Text, Is.EqualTo("Secre"));

            _viewModel.Text = ""; // delete entire string
            Assert.That(_viewModel.SecretText, Is.EqualTo(""));
            Assert.That(_viewModel.Text, Is.EqualTo(""));

            _viewModel.Text = "Secret3";
            Assert.That(_viewModel.SecretText, Is.EqualTo("Secret3"));
            Assert.That(_viewModel.Text, Is.EqualTo("Secret3"));
        }

        [Test]
        public void TestToggleHidden()
        {
            _viewModel.SecretText = "Secret";
            Assert.That(_viewModel.IsUnmasked, Is.False);
            Assert.That(_viewModel.SecretText, Is.EqualTo("Secret"));
            Assert.That(_viewModel.Text, Is.EqualTo(new string(SecretTextFieldViewModel.MaskChar, 6)));

            _viewModel.IsUnmasked = true;
            Assert.That(_viewModel.SecretText, Is.EqualTo("Secret"));
            Assert.That(_viewModel.Text, Is.EqualTo("Secret"));

            _viewModel.Text = "Secret2";
            Assert.That(_viewModel.SecretText, Is.EqualTo("Secret2"));
            Assert.That(_viewModel.Text, Is.EqualTo("Secret2"));

            _viewModel.IsUnmasked = false;
            Assert.That(_viewModel.SecretText, Is.EqualTo("Secret2"));
            Assert.That(_viewModel.Text, Is.EqualTo(new string(SecretTextFieldViewModel.MaskChar, 7)));

            _viewModel.IsUnmasked = true;
            Assert.That(_viewModel.SecretText, Is.EqualTo("Secret2"));
            Assert.That(_viewModel.Text, Is.EqualTo("Secret2"));
        }
    }
}
