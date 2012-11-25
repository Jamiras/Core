using System;
using System.Collections.Generic;
using System.ComponentModel;
using Jamiras.ViewModels;
using NUnit.Framework;

namespace Jamiras.Core.Tests.ViewModels
{
    [TestFixture]
    class ViewModelBaseTests
    {
        private class TestViewModel : ViewModelBase
        {
            public TestViewModel()
            {
                AddValidation("Text", ValidateText);
            }

            public string Text
            {
                get { return _text; }
                set
                {
                    if (_text != value)
                    {
                        _text = value;
                        OnPropertyChanged(new PropertyChangedEventArgs("Text"));
                    }
                }
            }
            private string _text;

            public int Integer
            {
                get { return _integer; }
                set
                {
                    if (_integer != value)
                    {
                        _integer = value;
                        OnPropertyChanged(() => Integer);
                    }
                }
            }
            private int _integer;

            private string ValidateText()
            {
                if (String.IsNullOrEmpty(_text))
                    return "Text is required.";

                if (_text.Length > 20)
                    return "Text is too long.";

                return String.Empty;
            }
        }

        [Test]
        public void TestInterfaces()
        {
            TestViewModel viewModel = new TestViewModel();
            Assert.That(viewModel, Is.InstanceOf<INotifyPropertyChanged>());
            Assert.That(viewModel, Is.InstanceOf<IDataErrorInfo>());
        }

        [Test]
        public void TestInitialization()
        {
            TestViewModel viewModel = new TestViewModel();
            Assert.That(viewModel.Text, Is.Null);
            Assert.That(viewModel.IsValid, Is.False);
            Assert.That(viewModel.Validate(), Is.EqualTo("Text is required."));
        }

        [Test]
        public void TestPropertyChanged()
        {
            TestViewModel viewModel = new TestViewModel();
            Assert.That(viewModel.Text, Is.Null);

            List<string> propertiesChanged = new List<string>();
            viewModel.PropertyChanged += (o, e) => propertiesChanged.Add(e.PropertyName);

            viewModel.Text = "Valid";
            Assert.That(propertiesChanged, Contains.Item("Text"));
        }

        [Test]
        public void TestValidation()
        {
            TestViewModel viewModel = new TestViewModel();
            IDataErrorInfo error = (IDataErrorInfo)viewModel;

            Assert.That(viewModel.Text, Is.Null);
            Assert.That(viewModel.IsValid, Is.False);
            Assert.That(error["Text"], Is.EqualTo("Text is required."));

            List<string> propertiesChanged = new List<string>();
            viewModel.PropertyChanged += (o, e) => propertiesChanged.Add(e.PropertyName);

            viewModel.Text = "This string is way to long to fit into 20 characters.";
            Assert.That(viewModel.IsValid, Is.False);
            Assert.That(error["Text"], Is.EqualTo("Text is too long."));
            Assert.That(propertiesChanged, Has.No.Member("IsValid"));

            viewModel.Text = "Valid";
            Assert.That(viewModel.IsValid, Is.True);
            Assert.That(error["Text"], Is.EqualTo(""));
            Assert.That(propertiesChanged, Contains.Item("IsValid"));

            propertiesChanged.Clear();
            viewModel.Text = String.Empty;
            Assert.That(viewModel.IsValid, Is.False);
            Assert.That(error["Text"], Is.EqualTo("Text is required."));
            Assert.That(propertiesChanged, Contains.Item("IsValid"));
        }
    }
}
