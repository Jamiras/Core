using System;
using System.Collections.Generic;
using Jamiras.DataModels;
using Jamiras.DataModels.Metadata;
using Jamiras.ViewModels.Converters;
using Jamiras.ViewModels.Fields;

namespace Jamiras.ViewModels.Grid
{
    public class AutoCompleteColumnDefinition : GridColumnDefinition
    {
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

        public bool IsDisplayTextDifferentFromSearchText { get; set; }

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
