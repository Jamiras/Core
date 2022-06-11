using System.Collections.Generic;
using Jamiras.Services;
using Jamiras.ViewModels;
using Moq;
using NUnit.Framework;

namespace Jamiras.UI.Tests.ViewModels
{
    [TestFixture]
    class DialogViewModelBaseTests
    {
        [SetUp]
        public void Setup()
        {
            _dialogService = new Mock<IDialogService>();
            _viewModel = new TestViewModel(_dialogService.Object);
        }

        private Mock<IDialogService> _dialogService;
        private TestViewModel _viewModel;

        private class TestViewModel : DialogViewModelBase
        {
            public TestViewModel(IDialogService dialogService)
                : base(dialogService)
            {
            }
        }

        [Test]
        public void TestInitialization()
        {
            Assert.That(_viewModel.DialogTitle, Is.Null);
            Assert.That(_viewModel.DialogResult, Is.EqualTo(DialogResult.None));
            Assert.That(_viewModel.OkButtonText, Is.EqualTo("OK"));
            Assert.That(_viewModel.CancelButtonText, Is.EqualTo("Cancel"));
        }

        [Test]
        public void TestDialogTitle()
        {
            List<string> propertiesChanged = new List<string>();
            _viewModel.PropertyChanged += (o, e) => propertiesChanged.Add(e.PropertyName);

            _viewModel.DialogTitle = "Title";
            Assert.That(_viewModel.DialogTitle, Is.EqualTo("Title"));
            Assert.That(propertiesChanged, Contains.Item("DialogTitle"));
        }

        [Test]
        public void TestOkButtonText()
        {
            List<string> propertiesChanged = new List<string>();
            _viewModel.PropertyChanged += (o, e) => propertiesChanged.Add(e.PropertyName);

            _viewModel.OkButtonText = "Save";
            Assert.That(_viewModel.OkButtonText, Is.EqualTo("Save"));
            Assert.That(propertiesChanged, Contains.Item("OkButtonText"));
        }

        [Test]
        public void TestOkCommand()
        {
            List<string> propertiesChanged = new List<string>();
            _viewModel.PropertyChanged += (o, e) => propertiesChanged.Add(e.PropertyName);

            _viewModel.OkCommand.Execute();
            Assert.That(_viewModel.DialogResult, Is.EqualTo(DialogResult.Ok));
            Assert.That(propertiesChanged, Contains.Item("DialogResult"));
        }

        private class TestViewModelWithErrors : DialogViewModelBase
        {
            public TestViewModelWithErrors(IDialogService dialogService)
                : base(dialogService)
            {
            }

            public override string Validate()
            {
                return "Foo is invalid.";
            }
        }

        [Test]
        public void TestOkCommandWithErrors()
        {
            var viewModel = new TestViewModelWithErrors(_dialogService.Object);
            var error = viewModel.Validate();

            bool errorShown = false;
            _dialogService.Setup(d => d.ShowDialog(It.IsAny<MessageBoxViewModel>())).
                Returns((MessageBoxViewModel mbvm) =>
                    {
                        errorShown = true;
                        Assert.That(mbvm.Message, Is.EqualTo(error));
                        return DialogResult.Ok;
                    });

            viewModel.OkCommand.Execute();

            Assert.That(errorShown, Is.True, "Error not shown");
            Assert.That(_viewModel.DialogResult, Is.EqualTo(DialogResult.None));
        }

        [Test]
        public void TestCancelCommand()
        {
            List<string> propertiesChanged = new List<string>();
            _viewModel.PropertyChanged += (o, e) => propertiesChanged.Add(e.PropertyName);

            _viewModel.CancelCommand.Execute();
            Assert.That(_viewModel.DialogResult, Is.EqualTo(DialogResult.Cancel));
            Assert.That(propertiesChanged, Contains.Item("DialogResult"));
        }

        [Test]
        public void TestCancelButtonText()
        {
            List<string> propertiesChanged = new List<string>();
            _viewModel.PropertyChanged += (o, e) => propertiesChanged.Add(e.PropertyName);

            _viewModel.CancelButtonText = "Abort";
            Assert.That(_viewModel.CancelButtonText, Is.EqualTo("Abort"));
            Assert.That(propertiesChanged, Has.Member("CancelButtonText"));

            propertiesChanged.Clear();
            _viewModel.CancelButtonText = null;
            Assert.That(_viewModel.CancelButtonText, Is.Null);
            Assert.That(propertiesChanged, Has.Member("CancelButtonText"));
        }

        [Test]
        public void TestShowDialogOk()
        {
            _dialogService.Setup(d => d.ShowDialog(_viewModel)).Returns(DialogResult.Ok);
            Assert.That(_viewModel.ShowDialog(), Is.EqualTo(DialogResult.Ok));
            _dialogService.Verify(d => d.ShowDialog(_viewModel));
        }

        [Test]
        public void TestShowDialogCancel()
        {
            _dialogService.Setup(d => d.ShowDialog(_viewModel)).Returns(DialogResult.Cancel);
            Assert.That(_viewModel.ShowDialog(), Is.EqualTo(DialogResult.Cancel));
            _dialogService.Verify(d => d.ShowDialog(_viewModel));
        }
    }
}
