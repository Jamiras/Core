using System;
using Jamiras.DataModels;
using Jamiras.ViewModels.Converters;
using Jamiras.ViewModels.Fields;

namespace Jamiras.ViewModels.Grid
{
    public class DisplayTextColumnDefinition : GridColumnDefinition
    {
        public DisplayTextColumnDefinition(string header, ModelProperty sourceProperty)
            : base(header, sourceProperty)
        {
            SetValueCore(IsReadOnlyProperty, true);
        }

        public DisplayTextColumnDefinition(string header, ModelProperty sourceProperty, Func<object, string> getDisplayTextFunction)
            : this(header, sourceProperty)
        {
            _converter = new DelegateConverter(o => getDisplayTextFunction(o), null);
        }

        private readonly IConverter _converter;

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

        protected override FieldViewModelBase CreateFieldViewModel(GridRowViewModel row)
        {
            var viewModel = new ReadOnlyTextFieldViewModel(Header);
            viewModel.IsRightAligned = IsRightAligned;
            viewModel.SetBinding(ReadOnlyTextFieldViewModel.TextProperty, new ModelBinding(row, SourceProperty, ModelBindingMode.OneWay, _converter));
            return viewModel;
        }
    }
}
