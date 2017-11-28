using System;
using System.Collections.Generic;
using Jamiras.DataModels;
using Jamiras.DataModels.Metadata;
using Jamiras.ViewModels.Converters;
using Jamiras.ViewModels.Fields;

namespace Jamiras.ViewModels.Grid
{
    /// <summary>
    /// Defines an autocomplete column for the <see cref="GridViewModel"/>.
    /// </summary>
    public class AutoCompleteColumnDefinition : GridColumnDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AutoCompleteColumnDefinition"/> class.
        /// </summary>
        /// <param name="header">The column header text.</param>
        /// <param name="idProperty">The integer property bound to the selected item.</param>
        /// <param name="stringProperty">The string property bound to the displayed text.</param>
        /// <param name="metadata">Information about the data for the column.</param>
        /// <param name="searchFunction">Function to call to provide suggestions for current text.</param>
        /// <param name="lookupLabelFunction">Function to call to get text for the selected item.</param>
        public AutoCompleteColumnDefinition(string header, ModelProperty idProperty, ModelProperty stringProperty,
            StringFieldMetadata metadata, Func<string, IEnumerable<LookupItem>> searchFunction, Func<int, string> lookupLabelFunction)
            : base(header, idProperty)
        {
            _metadata = metadata;
            _stringProperty = stringProperty;
            _searchFunction = searchFunction;
            _lookupLabelFunction = lookupLabelFunction;
        }

        private readonly StringFieldMetadata _metadata;
        private readonly ModelProperty _stringProperty;
        private readonly Func<string, IEnumerable<LookupItem>> _searchFunction;
        private readonly Func<int, string> _lookupLabelFunction;

        /// <summary>
        /// Gets or sets a value indicating whether this instance is display text different from search text.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is display text different from search text; otherwise, <c>false</c>.
        /// </value>
        public bool IsDisplayTextDifferentFromSearchText { get; set; }

        /// <summary>
        /// Creates the FieldViewModel responsible for rendering this column and binds it to the provided row.
        /// </summary>
        protected override FieldViewModelBase CreateFieldViewModel(GridRowViewModel row)
        {
            if (IsReadOnly)
            {
                var textViewModel = new ReadOnlyTextFieldViewModel(Header);
                textViewModel.SetBinding(ReadOnlyTextFieldViewModel.TextProperty, 
                    new ModelBinding(row, SourceProperty, ModelBindingMode.OneWay, new DelegateConverter(id => _lookupLabelFunction((int)id), null)));
                return textViewModel;
            }

            var viewModel = new AutoCompleteFieldViewModel(Header, _metadata, _searchFunction, _lookupLabelFunction);
            viewModel.IsDisplayTextDifferentFromSearchText = IsDisplayTextDifferentFromSearchText;

            // bind the property through to the data model
            row.SetBinding(_stringProperty, new ModelBinding(row.Model, _stringProperty));

            // bind text first so selection binding will cause it to be updated
            viewModel.SetBinding(AutoCompleteFieldViewModel.TextProperty, new ModelBinding(row, _stringProperty, ModelBindingMode.TwoWay));
            viewModel.BindSelection(row, SourceProperty, ModelBindingMode.TwoWay);

            return viewModel;
        }
    }
}
