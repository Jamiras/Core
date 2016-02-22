using System.Collections.Generic;
using System.Linq;
using Jamiras.DataModels;

namespace Jamiras.ViewModels.Fields
{
    public class LookupFieldViewModel : FieldViewModelBase
    {
        public LookupFieldViewModel(string label)
        {
            Label = label;
        }

        public LookupFieldViewModel(string label, IEnumerable<LookupItem> items)
            : this(label)
        {
            Items = items;
        }

        public static readonly ModelProperty ItemsProperty =
            ModelProperty.Register(typeof(LookupFieldViewModel), "Items", typeof(IEnumerable<LookupItem>), null);

        /// <summary>
        /// Gets or sets the items to choose from.
        /// </summary>
        public IEnumerable<LookupItem> Items
        {
            get { return (IEnumerable<LookupItem>)GetValue(ItemsProperty); }
            set { SetValue(ItemsProperty, value); }
        }

        public static readonly ModelProperty SelectedIdProperty =
            ModelProperty.Register(typeof(LookupFieldViewModel), "SelectedId", typeof(int), 0);

        /// <summary>
        /// Gets or sets the currently selected item.
        /// </summary>
        public int SelectedId
        {
            get { return (int)GetValue(SelectedIdProperty); }
            set { SetValue(SelectedIdProperty, value); }
        }

        public static readonly ModelProperty SelectedLabelProperty =
            ModelProperty.RegisterDependant(typeof(LookupFieldViewModel), "SelectedLabel", typeof(string), new[] { SelectedIdProperty }, GetSelectedLabel);

        private static object GetSelectedLabel(ModelBase model)
        {
            var vm = (LookupFieldViewModel)model;
            var selectedItem = vm.Items.FirstOrDefault(i => i.Id == vm.SelectedId);
            return (selectedItem != null) ? selectedItem.Label : "";
        }

        /// <summary>
        /// Gets the label for the currently selected item.
        /// </summary>
        public string SelectedLabel
        {
            get { return (string)GetValue(SelectedLabelProperty); }
        }

        protected override string Validate(ModelProperty property, object value)
        {
            if (property == SelectedIdProperty && IsRequired && (value == null || (int)value == 0))
                return FormatErrorMessage("{0} is required.");

            return base.Validate(property, value);
        }

        protected override void OnModelPropertyChanged(ModelPropertyChangedEventArgs e)
        {
            if (e.Property == IsRequiredProperty)
                Validate(SelectedIdProperty);

            base.OnModelPropertyChanged(e);
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
    }
}
