using System;
using Jamiras.DataModels;
using Jamiras.DataModels.Metadata;
using Jamiras.Components;
using Jamiras.ViewModels.Converters;

namespace Jamiras.ViewModels.Fields
{
    public class DateFieldViewModel : FieldViewModelBase
    {
        public DateFieldViewModel(string label, DateTimeFieldMetadata metadata)
        {
            Label = label;

            SetBinding(DateTimeProperty, new ModelBinding(this, DateProperty, new DateToDateTimeConverter()));
        }

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
    }
}
