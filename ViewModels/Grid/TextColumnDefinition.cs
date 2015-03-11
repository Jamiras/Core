using Jamiras.DataModels;
using Jamiras.DataModels.Metadata;
using Jamiras.ViewModels.Converters;
using Jamiras.ViewModels.Fields;

namespace Jamiras.ViewModels.Grid
{
    public class TextColumnDefinition : GridColumnDefinition
    {
        public TextColumnDefinition(string header, ModelProperty sourceProperty, StringFieldMetadata metadata, IConverter converter)
            : this(header, sourceProperty, metadata)
        {
            _converter = converter;
        }

        public TextColumnDefinition(string header, ModelProperty sourceProperty, StringFieldMetadata metadata)
            : base(header, sourceProperty)
        {
            _metadata = metadata;
        }

        private readonly StringFieldMetadata _metadata;
        private readonly IConverter _converter;

        public static readonly ModelProperty IsRightAlignedProperty =
            ModelProperty.Register(typeof(TextColumnDefinition), "IsRightAligned", typeof(bool), false);

        /// <summary>
        /// Gets or sets whether text in this column is right aligned.
        /// </summary>
        public bool IsRightAligned
        {
            get { return (bool)GetValue(IsRightAlignedProperty); }
            set { SetValue(IsRightAlignedProperty, value); }
        }

        protected override FieldViewModelBase CreateFieldViewModel(GridRowViewModel row)
        {
            if (IsReadOnly)
            {
                var textViewModel = new ReadOnlyTextFieldViewModel(Header);
                textViewModel.IsRightAligned = IsRightAligned;
                textViewModel.SetBinding(ReadOnlyTextFieldViewModel.TextProperty, new ModelBinding(row, SourceProperty, ModelBindingMode.OneWay, _converter));
                return textViewModel;
            }

            var viewModel = new TextFieldViewModel(Header, _metadata);
            viewModel.SetBinding(TextFieldViewModel.TextProperty, new ModelBinding(row, SourceProperty, ModelBindingMode.TwoWay, _converter));
            viewModel.IsRightAligned = IsRightAligned;
            return viewModel;
        }
    }
}
