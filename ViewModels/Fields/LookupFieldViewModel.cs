using Jamiras.DataModels;
using System.Collections.Generic;
using System.Linq;

namespace Jamiras.ViewModels.Fields
{
    /// <summary>
    /// ViewModel for multiple choice input.
    /// </summary>
    public class LookupFieldViewModel : FieldViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LookupFieldViewModel"/> class.
        /// </summary>
        /// <param name="label">The field label.</param>
        public LookupFieldViewModel(string label)
        {
            Label = label;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LookupFieldViewModel"/> class.
        /// </summary>
        /// <param name="label">The field label.</param>
        /// <param name="items">The available items.</param>
        public LookupFieldViewModel(string label, IEnumerable<LookupItem> items)
            : this(label)
        {
            Items = items;
        }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="Items"/>
        /// </summary>
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

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="SelectedId"/>
        /// </summary>
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

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="SelectedLabel"/>
        /// </summary>
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

        /// <summary>
        /// Validates a value being assigned to a property.
        /// </summary>
        /// <param name="property">Property being modified.</param>
        /// <param name="value">Value being assigned to the property.</param>
        /// <returns>
        ///   <c>null</c> if the value is valid for the property, or an error message indicating why the value is not valid.
        /// </returns>
        protected override string Validate(ModelProperty property, object value)
        {
            if (property == SelectedIdProperty && IsRequired && (value == null || (int)value == 0))
                return FormatErrorMessage("{0} is required.");

            return base.Validate(property, value);
        }

        /// <summary>
        /// Notifies any subscribers that the value of a <see cref="ModelProperty" /> has changed.
        /// </summary>
        /// <param name="e"></param>
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
