using System;
using System.Collections.Generic;
using Jamiras.DataModels;
using Jamiras.DataModels.Metadata;

namespace Jamiras.ViewModels.Fields
{
    public class AutoCompleteFieldViewModel : TextFieldViewModelBase
    {
        public AutoCompleteFieldViewModel(string label, StringFieldMetadata metadata, 
            Func<string, IEnumerable<LookupItem>> searchFunction, Func<int, string> lookupLabelFunction)
            : base(label, metadata.MaxLength)
        {
            IsTextBindingDelayed = true;

            _searchFunction = searchFunction;
            _lookupLabelFunction = lookupLabelFunction;

            // subscribe to our own PropertyChanged so we can benefit from the IsTextBindingDelayed
            AddPropertyChangedHandler(TextProperty, OnTextChanged);
        }

        private readonly Func<string, IEnumerable<LookupItem>> _searchFunction;
        private readonly Func<int, string> _lookupLabelFunction;
        private string _searchText;
        private bool _searchDisabled, _searchPending;

        protected override void OnModelPropertyChanged(ModelPropertyChangedEventArgs e)
        {
            if (e.Property == TextFieldViewModel.TextProperty && !_searchDisabled)
                _searchPending = true;

            base.OnModelPropertyChanged(e);
        }

        private void OnTextChanged(object sender, ModelPropertyChangedEventArgs e)
        {
            if (_searchPending)
            {
                _searchPending = false;
                PerformSearch();
            }
        }

        private void PerformSearch()
        {
            lock (_searchFunction)
            {
                if (_searchText == Text)
                    return;

                _searchText = Text;
            }

            var suggestions = String.IsNullOrEmpty(_searchText) ? null : _searchFunction(_searchText);

            lock (_searchFunction)
            {
                if (_searchText == Text)
                    SetValue(SuggestionsProperty, suggestions);
            }
        }

        public static readonly ModelProperty SuggestionsProperty = 
            ModelProperty.Register(typeof(AutoCompleteFieldViewModel), "Suggestions", typeof(IEnumerable<LookupItem>), null);

        /// <summary>
        /// Gets the suggestions matching the current Text value.
        /// </summary>
        public IEnumerable<LookupItem> Suggestions
        {
            get { return (IEnumerable<LookupItem>)GetValue(SuggestionsProperty); }
        }

        /// <summary>
        /// Binds the ViewModel to a source model.
        /// </summary>
        /// <param name="source">Model to bind to.</param>
        /// <param name="property">Property on model to bind to.</param>
        /// <param name="mode">How to bind to the source model.</param>
        public void BindSelection(ModelBase source, ModelProperty property, ModelBindingMode mode = ModelBindingMode.Committed)
        {
            SetBinding(SelectedIdProperty, new ModelBinding(source, property, mode));
        }

        public static readonly ModelProperty SelectedIdProperty =
            ModelProperty.Register(typeof(AutoCompleteFieldViewModel), "SelectedId", typeof(int), 0, OnSelectedIdChanged);

        /// <summary>
        /// Gets or sets the unique identifier of the matching item.
        /// </summary>
        public int SelectedId
        {
            get { return (int)GetValue(SelectedIdProperty); }
            set { SetValue(SelectedIdProperty, value); }
        }

        private static void OnSelectedIdChanged(object sender, ModelPropertyChangedEventArgs e)
        {
            var vm = (AutoCompleteFieldViewModel)sender;
            var id = (int)e.NewValue;

            if (vm.Suggestions != null)
            {
                foreach (var lookupItem in vm.Suggestions)
                {
                    if (lookupItem.Id == id)
                    {
                        vm.SetText(lookupItem.Label);
                        return;
                    }
                }
            }

            if (id != 0)
            {
                var label = vm._lookupLabelFunction(id);
                vm.SetText(label);
            }
        }

        internal override void SetText(string value)
        {
            _searchDisabled = true;
            try
            {
                _searchPending = false;
                base.SetText(value);
            }
            finally
            {
                _searchDisabled = false;
            }
        }

        public static readonly ModelProperty IsMatchRequiredProperty =
            ModelProperty.Register(typeof(FieldViewModelBase), "IsMatchRequired", typeof(bool), false);

        /// <summary>
        /// Gets or sets whether an exact match is required.
        /// </summary>
        public bool IsMatchRequired
        {
            get { return (bool)GetValue(IsMatchRequiredProperty); }
            set { SetValue(IsMatchRequiredProperty, value); }
        }

        protected override string Validate(ModelProperty property, object value)
        {
            if (property == TextProperty && IsMatchRequired && !IsFixedSelection)
            {
                if (SelectedId == 0 && !String.IsNullOrEmpty(Text))
                    return String.Format("{0} is not a matching value.", LabelWithoutAccelerators);
            }

            return base.Validate(property, value);
        }

        public static readonly ModelProperty IsFixedSelectionProperty =
            ModelProperty.Register(typeof(AutoCompleteFieldViewModel), "IsFixedSelection", typeof(bool), false, OnIsFixedSelectionChanged);

        /// <summary>
        /// Gets or sets whether selection is fixed (allows control to be reused in cases where the name can be edited, but the selected id cannot).
        /// </summary>
        public bool IsFixedSelection
        {
            get { return (bool)GetValue(IsFixedSelectionProperty); }
            set { SetValue(IsFixedSelectionProperty, value); }
        }

        private static void OnIsFixedSelectionChanged(object sender, ModelPropertyChangedEventArgs e)
        {
            var vm = (AutoCompleteFieldViewModel)sender;
            if ((bool)e.NewValue)
                vm.RemovePropertyChangedHandler(TextProperty, vm.OnTextChanged);
            else
                vm.AddPropertyChangedHandler(TextProperty, vm.OnTextChanged);
        }
    }
}
