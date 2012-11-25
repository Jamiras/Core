using System.Collections.Generic;
using Jamiras.ViewModels;
using NUnit.Framework;
using Jamiras.Services;
using Moq;

namespace Jamiras.Core.Tests.ViewModels
{
    [TestFixture]
    class MessageBoxViewModelTests
    {
        [TestFixtureSetUp]
        public void FixtureSetup()
        {
            var dialogService = new Mock<IDialogService>();
            dialogService.Setup(d => d.ShowDialog(It.IsAny<MessageBoxViewModel>())).Returns(DialogResult.OK);
            _dialogService = dialogService.Object;
        }

        private IDialogService _dialogService;

        [Test]
        public void TestInitialization()
        {
            var viewModel = new MessageBoxViewModel("Hello, world!", _dialogService);
            Assert.That(viewModel, Is.InstanceOf<DialogViewModelBase>());
            Assert.That(viewModel.IsCancelButtonVisible, Is.False);
            Assert.That(viewModel.DialogTitle, Is.Null);
            Assert.That(viewModel.DialogResult, Is.EqualTo(DialogResult.None));
            Assert.That(viewModel.Message, Is.EqualTo("Hello, world!"));
            Assert.That(viewModel.OkButtonText, Is.EqualTo("OK"));
        }

        [Test]
        public void TestMessage()
        {
            var viewModel = new MessageBoxViewModel("Foo", _dialogService);
            Assert.That(viewModel.Message, Is.EqualTo("Foo"));

            List<string> propertiesChanged = new List<string>();
            viewModel.PropertyChanged += (o, e) => propertiesChanged.Add(e.PropertyName);

            viewModel.Message = "Foo";
            Assert.That(propertiesChanged, Has.No.Member("Message"));

            viewModel.Message = "Bar";
            Assert.That(propertiesChanged, Contains.Item("Message"));
        }

        [Test]
        public void TestOkCommand()
        {
            var viewModel = new MessageBoxViewModel("Foo", _dialogService);

            List<string> propertiesChanged = new List<string>();
            viewModel.PropertyChanged += (o, e) => propertiesChanged.Add(e.PropertyName);

            viewModel.OkCommand.Execute();
            Assert.That(viewModel.DialogResult, Is.EqualTo(DialogResult.OK));
            Assert.That(propertiesChanged, Contains.Item("DialogResult"));
        }
    }
}
