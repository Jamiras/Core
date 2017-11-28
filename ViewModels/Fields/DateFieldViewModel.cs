using System;
using Jamiras.DataModels;
using Jamiras.DataModels.Metadata;
using Jamiras.Components;
using Jamiras.ViewModels.Converters;

namespace Jamiras.ViewModels.Fields
{
    /// <summary>
    /// ViewModel for date input.
    /// </summary>
    public class DateFieldViewModel : FieldViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DateFieldViewModel"/> class.
        /// </summary>
        /// <param name="label">The field label.</param>
        /// <param name="metadata">Information about the field.</param>
        public DateFieldViewModel(string label, DateTimeFieldMetadata metadata)
        {
            Label = label;

            SetBinding(DateTimeProperty, new ModelBinding(this, DateProperty, new DateToDateTimeConverter()));
        }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="DateTime"/>
        /// </summary>
        public static readonly ModelProperty DateTimeProperty =
            ModelProperty.Register(typeof(DateFieldViewModel), "DateTime", typeof(DateTime?), null);

        /// <summary>
        /// Gets or sets the date/time value.
        /// </summary>
        public DateTime? DateTime
        {
            get { return (DateTime?)GetValue(DateTimeProperty); }
            set { SetValue(DateTimeProperty, value); }
        }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="Date"/>
        /// </summary>
        public static readonly ModelProperty DateProperty =
            ModelProperty.Register(typeof(DateFieldViewModel), "Date", typeof(Date), Date.Empty);

        /// <summary>
        /// Gets or sets the date value.
        /// </summary>
        public Date Date
        {
            get { return (Date)GetValue(DateProperty); }
            set { SetValue(DateProperty, value); }
        }

        /// <summary>
        /// Binds the ViewModel to a source model.
        /// </summary>
        /// <param name="source">Model to bind to.</param>
        /// <param name="property">Property on model to bind to.</param>
        /// <param name="mode">How to bind to the source model.</param>
        public void BindDate(ModelBase source, ModelProperty property, ModelBindingMode mode = ModelBindingMode.Committed)
        {
            SetBinding(DateProperty, new ModelBinding(source, property, mode));
        }
    }
}
