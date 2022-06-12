using Jamiras.Components;
using Jamiras.DataModels;
using Jamiras.ViewModels.Converters;
using Jamiras.ViewModels.Fields;

namespace Jamiras.ViewModels.Grid
{
    /// <summary>
    /// Defines a read only text column for the <see cref="GridViewModel"/>.
    /// </summary>
    public class DisplayTextColumnDefinition : GridColumnDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DisplayTextColumnDefinition"/> class.
        /// </summary>
        /// <param name="header">The column header text.</param>
        /// <param name="sourceProperty">The property bound to the column.</param>
        public DisplayTextColumnDefinition(string header, ModelProperty sourceProperty)
            : base(header, sourceProperty)
        {
            SetValueCore(IsReadOnlyProperty, true);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DisplayTextColumnDefinition"/> class.
        /// </summary>
        /// <param name="header">The column header text.</param>
        /// <param name="sourceProperty">The property bound to the column.</param>
        /// <param name="converter">Converter to format the text.</param>
        public DisplayTextColumnDefinition(string header, ModelProperty sourceProperty, IConverter converter)
            : this(header, sourceProperty)
        {
            _converter = converter;
        }

        private readonly IConverter _converter;

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="IsRightAligned"/>
        /// </summary>
        public static readonly ModelProperty IsRightAlignedProperty =
            ModelProperty.Register(typeof(DisplayTextColumnDefinition), "IsRightAligned", typeof(bool), false);

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
            var viewModel = new ReadOnlyTextFieldViewModel(Header);
            viewModel.IsRightAligned = IsRightAligned;
            viewModel.SetBinding(ReadOnlyTextFieldViewModel.TextProperty, new ModelBinding(row, SourceProperty, ModelBindingMode.OneWay, _converter));
            return viewModel;
        }
    }
}
