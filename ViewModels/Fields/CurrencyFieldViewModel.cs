using System;
using Jamiras.DataModels;
using Jamiras.DataModels.Metadata;

namespace Jamiras.ViewModels.Fields
{
    /// <summary>
    /// ViewModel for currency input.
    /// </summary>
    public class CurrencyFieldViewModel : TextFieldViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CurrencyFieldViewModel"/> class.
        /// </summary>
        /// <param name="label">The field label.</param>
        /// <param name="metadata">Information about the field.</param>
        public CurrencyFieldViewModel(string label, CurrencyFieldMetadata metadata)
            : base(label, 8)
        {
            IsTextBindingDelayed = true;
        }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="Value"/>
        /// </summary>
        public static readonly ModelProperty ValueProperty =
            ModelProperty.Register(typeof(CurrencyFieldViewModel), "Value", typeof(float?), null, OnValueChanged);

        private static void OnValueChanged(object sender, ModelPropertyChangedEventArgs e)
        {
            var viewModel = (CurrencyFieldViewModel)sender;
            viewModel.SetText((e.NewValue == null) ? String.Empty : String.Format("${0:F2}", e.NewValue));
        }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        public float? Value
        {
            get { return (float?)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        /// <summary>
        /// Binds the ViewModel to a source model.
        /// </summary>
        /// <param name="source">Model to bind to.</param>
        /// <param name="property">Property on model to bind to.</param>
        /// <param name="mode">How to bind to the source model.</param>
        public void BindValue(ModelBase source, ModelProperty property, ModelBindingMode mode = ModelBindingMode.Committed)
        {
            SetBinding(ValueProperty, new ModelBinding(source, property, mode));
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
            if (property == TextProperty)
            {
                string strValue = (string)value;
                if (String.IsNullOrEmpty(strValue))
                {
                    if (IsRequired)
                        return String.Format("{0} is required", LabelWithoutAccelerators);

                    Value = null;
                }
                else
                {
                    if (strValue[0] == '$')
                        strValue = strValue.Substring(1);

                    float fVal;
                    if (!float.TryParse(strValue, out fVal))
                        return String.Format("{0} is not a valid dollar amount", LabelWithoutAccelerators);

                    Value = (float)Math.Round(fVal, 2);
                }
            }

            return base.Validate(property, value);
        }
    }
}
