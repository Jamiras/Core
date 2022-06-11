using System;
using Jamiras.Components;
using Jamiras.DataModels;
using Jamiras.DataModels.Metadata;
using Jamiras.ViewModels.Converters;
using Jamiras.ViewModels.Fields;

namespace Jamiras.ViewModels.Grid
{
    /// <summary>
    /// Defines a date column for the <see cref="GridViewModel"/>.
    /// </summary>
    public class DateColumnDefinition : GridColumnDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DateColumnDefinition"/> class.
        /// </summary>
        /// <param name="header">The column header text.</param>
        /// <param name="sourceProperty">The property bound to the column.</param>
        /// <param name="metadata">Information about the data for the column.</param>
        public DateColumnDefinition(string header, ModelProperty sourceProperty, DateTimeFieldMetadata metadata)
            : base(header, sourceProperty)
        {
            _metadata = metadata;
        }

        private readonly DateTimeFieldMetadata _metadata;

        private static readonly IConverter _dateConverter = new DelegateConverter(ConvertDate, null);

        private static string ConvertDate(object c)
        {
            if (c is Date)
                return ((Date)c).ToString("MM/dd/yyyy");
            if (c is DateTime)
                return ((DateTime)c).ToShortDateString();

            return c.ToString();
        }

        /// <summary>
        /// Creates the FieldViewModel responsible for rendering this column and binds it to the provided row.
        /// </summary>
        protected override FieldViewModelBase CreateFieldViewModel(GridRowViewModel row)
        {
            if (IsReadOnly)
            {
                var textViewModel = new ReadOnlyTextFieldViewModel(Header);
                textViewModel.IsRightAligned = true;
                textViewModel.SetBinding(ReadOnlyTextFieldViewModel.TextProperty, new ModelBinding(row, SourceProperty, ModelBindingMode.OneWay, _dateConverter));
                return textViewModel;
            }

            var viewModel = new DateFieldViewModel(Header, _metadata);
            if (SourceProperty.PropertyType == typeof(Date))
                viewModel.BindDate(row, SourceProperty, ModelBindingMode.TwoWay);
            else
                viewModel.SetBinding(DateFieldViewModel.DateTimeProperty, new ModelBinding(row, SourceProperty, ModelBindingMode.TwoWay));

            return viewModel;
        }
    }
}
