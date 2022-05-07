using Jamiras.DataModels;
using Jamiras.DataModels.Metadata;
using Jamiras.ViewModels.Converters;
using Jamiras.ViewModels.Fields;

namespace Jamiras.ViewModels.Grid
{
    /// <summary>
    /// Defines a text column for the <see cref="GridViewModel"/>.
    /// </summary>
    public class TextColumnDefinition : GridColumnDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TextColumnDefinition"/> class.
        /// </summary>
        /// <param name="header">The column header text.</param>
        /// <param name="sourceProperty">The property bound to the column.</param>
        /// <param name="metadata">Information about the data for the column.</param>
        /// <param name="converter">Converter to format text.</param>
        public TextColumnDefinition(string header, ModelProperty sourceProperty, StringFieldMetadata metadata, IConverter converter)
            : this(header, sourceProperty, metadata)
        {
            _converter = converter;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextColumnDefinition"/> class.
        /// </summary>
        /// <param name="header">The column header text.</param>
        /// <param name="sourceProperty">The property bound to the column.</param>
        /// <param name="metadata">Information about the data for the column.</param>
        public TextColumnDefinition(string header, ModelProperty sourceProperty, StringFieldMetadata metadata)
            : base(header, sourceProperty)
        {
            _metadata = metadata;
        }

        private readonly StringFieldMetadata _metadata;
        private readonly IConverter _converter;

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="IsRightAligned"/>
        /// </summary>
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

        /// <summary>
        /// Creates the FieldViewModel responsible for rendering this column and binds it to the provided row.
        /// </summary>
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
