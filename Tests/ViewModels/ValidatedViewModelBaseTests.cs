using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Jamiras.DataModels;
using Jamiras.DataModels.Metadata;
using Jamiras.ViewModels;
using Moq;
using NUnit.Framework;
using Jamiras.Components;

namespace Jamiras.Core.Tests.ViewModels
{
    [TestFixture]
    public class ValidatedViewModelBaseTests
    {
        private class TestModel : ModelBase
        {
            public static readonly ModelProperty StrProperty = ModelProperty.Register(typeof(TestModel), "Str", typeof(string), "Happy");

            public string Str
            {
                get { return (string)GetValue(StrProperty); }
                set { SetValue(StrProperty, value); }
            }

            public static readonly ModelProperty IntegerProperty = ModelProperty.Register(typeof(TestModel), "Integer", typeof(int), 0);

            public int Integer
            {
                get { return (int)GetValue(IntegerProperty); }
                set { SetValue(IntegerProperty, value); }
            }
        }

        private class TestViewModel : ValidatedViewModelBase
        {
            public static readonly ModelProperty TextProperty = ModelProperty.Register(typeof(TestViewModel), "Text", typeof(string), null);

            public string Text
            {
                get { return (string)GetValue(TextProperty); }
                set { SetValue(TextProperty, value); }
            }

            public static readonly ModelProperty IntegerProperty = ModelProperty.Register(typeof(TestViewModel), "Integer", typeof(int), 1);

            public int Integer
            {
                get { return (int)GetValue(IntegerProperty); }
                set { SetValue(IntegerProperty, value); }
            }

            protected override string Validate(ModelProperty property, object value)
            {
                if (property == TextProperty)
                {
                    var text = (string)value;
                    if (String.IsNullOrEmpty(text))
                        return "Text is required.";

                    if (text.Length > 20)
                        return "Text is too long.";
                }

                return base.Validate(property, value);
            }
        }

        [SetUp]
        public void Setup()
        {
            _model = new TestModel();
            _viewModel = new TestViewModel();

            _mockMetadataRepository = new Mock<IDataModelMetadataRepository>();
            typeof(ValidatedViewModelBase).GetField("_metadataRepository", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(_viewModel, _mockMetadataRepository.Object);
        }

        private TestModel _model;
        private TestViewModel _viewModel;
        private Mock<IDataModelMetadataRepository> _mockMetadataRepository;

        [Test]
        public void TestInterfaces()
        {
            Assert.That(_viewModel, Is.InstanceOf<INotifyPropertyChanged>());
            Assert.That(_viewModel, Is.InstanceOf<IDataErrorInfo>());
        }

        [Test]
        public void TestInitialization()
        {
            IDataErrorInfo error = _viewModel;

            Assert.That(_viewModel.Text, Is.Null);
            Assert.That(_viewModel.IsValid, Is.False);
            Assert.That(error["Text"], Is.EqualTo("Text is required."));
        }

        [Test]
        public void TestValidationUnbound()
        {
            IDataErrorInfo error = _viewModel;

            Assert.That(_viewModel.Text, Is.Null);
            Assert.That(_viewModel.IsValid, Is.False);
            Assert.That(error["Text"], Is.EqualTo("Text is required."));

            List<string> propertiesChanged = new List<string>();
            _viewModel.PropertyChanged += (o, e) => propertiesChanged.Add(e.PropertyName);

            _viewModel.Text = "This string is way to long to fit into 20 characters.";
            Assert.That(_viewModel.IsValid, Is.False);
            Assert.That(error["Text"], Is.EqualTo("Text is too long."));
            Assert.That(propertiesChanged, Has.No.Member("IsValid"));

            _viewModel.Text = "Valid";
            Assert.That(_viewModel.IsValid, Is.True);
            Assert.That(error["Text"], Is.EqualTo(""));
            Assert.That(propertiesChanged, Contains.Item("IsValid"));

            propertiesChanged.Clear();
            _viewModel.Text = String.Empty;
            Assert.That(_viewModel.IsValid, Is.False);
            Assert.That(error["Text"], Is.EqualTo("Text is required."));
            Assert.That(propertiesChanged, Contains.Item("IsValid"));
        }

        [Test]
        public void TestValidationConverter()
        {
            _viewModel.SetBinding(TestViewModel.TextProperty, new ModelBinding(_model, TestModel.IntegerProperty, new ViewModelBaseTests.NumberToStringConverter()));

            IDataErrorInfo error = _viewModel;

            Assert.That(_viewModel.Text, Is.EqualTo("0"));
            Assert.That(_viewModel.IsValid, Is.True);
            Assert.That(error["Text"], Is.EqualTo(""));

            _viewModel.Text = "This string is way to long to fit into 20 characters.";
            Assert.That(_viewModel.IsValid, Is.False);
            Assert.That(error["Text"], Is.EqualTo("Text is too long."));

            _viewModel.Text = "Not a number";
            Assert.That(_viewModel.IsValid, Is.False);
            Assert.That(error["Text"], Is.EqualTo("parse error"));

            _viewModel.Text = "123";
            Assert.That(_viewModel.IsValid, Is.True);
            Assert.That(error["Text"], Is.EqualTo(""));
        }

        private class TestFieldMetadata : FieldMetadata
        {
            public TestFieldMetadata()
                : base("Text", typeof(string))
            {
            }

            public override string Validate(ModelBase model, object value)
            {
                return "metadata error";
            }
        }

        private class TestModelMetadata : ModelMetadata
        {
            public TestModelMetadata()
            {
                RegisterFieldMetadata(TestModel.IntegerProperty, new TestFieldMetadata());
            }
        }

        [Test]
        public void TestValidationMetadata()
        {
            _mockMetadataRepository.Setup(r => r.GetModelMetadata(typeof(TestModel))).Returns(new TestModelMetadata());
            _viewModel.SetBinding(TestViewModel.TextProperty, new ModelBinding(_model, TestModel.IntegerProperty, new ViewModelBaseTests.NumberToStringConverter()));

            IDataErrorInfo error = _viewModel;

            Assert.That(_viewModel.Text, Is.EqualTo("0"));
            Assert.That(_viewModel.IsValid, Is.False);
            Assert.That(error["Text"], Is.EqualTo("metadata error"));

            _viewModel.Text = "Not a number";
            Assert.That(_viewModel.IsValid, Is.False);
            Assert.That(error["Text"], Is.EqualTo("parse error"));

            _viewModel.Text = "123";
            Assert.That(_viewModel.IsValid, Is.False);
            Assert.That(error["Text"], Is.EqualTo("metadata error"));
        }

        [Test]
        public void TestValidate()
        {
            Assert.That(_viewModel.Text, Is.Null);
            Assert.That(_viewModel.IsValid, Is.False);
            Assert.That(_viewModel.Validate(), Is.EqualTo("Text is required."));

            _viewModel.Text = "Banana";
            Assert.That(_viewModel.IsValid, Is.True);
            Assert.That(_viewModel.Validate(), Is.EqualTo(""));
        }
    }
}
