using System;
using Jamiras.DataModels;
using Jamiras.DataModels.Metadata;

namespace Jamiras.ViewModels.Fields
{
    public class DecimalFieldViewModel : TextFieldViewModelBase
    {
        public DecimalFieldViewModel(string label, FloatFieldMetadata metadata)
            : this(label)
        {
        }

        public DecimalFieldViewModel(string label)
            : base(label, 16)
        {
            IsTextBindingDelayed = true;
        }

        public static readonly ModelProperty PrecisionProperty =
            ModelProperty.Register(typeof(DecimalFieldViewModel), "Precision", typeof(int), 2, OnValueChanged);

        public int Precision
        {
            get { return (int)GetValue(PrecisionProperty); }
            set { SetValue(PrecisionProperty, value); }
        }

        public static readonly ModelProperty ValueProperty =
            ModelProperty.Register(typeof(DecimalFieldViewModel), "Value", typeof(float?), null, OnValueChanged);

        private static void OnValueChanged(object sender, ModelPropertyChangedEventArgs e)
        {
            var viewModel = (DecimalFieldViewModel)sender;
            var format = "{0:F" + viewModel.Precision + "}";
            viewModel.SetText((e.NewValue == null) ? String.Empty : String.Format(format, e.NewValue));
        }

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
                    float fVal;
                    if (!float.TryParse(strValue, out fVal))
                        return String.Format("{0} is not a valid decimal", LabelWithoutAccelerators);

                    Value = (float)Math.Round(fVal, Precision);
                }
            }

            return base.Validate(property, value);
        }
    }
}
