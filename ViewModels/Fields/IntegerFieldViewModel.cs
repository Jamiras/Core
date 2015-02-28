using System;
using Jamiras.DataModels;
using Jamiras.DataModels.Metadata;

namespace Jamiras.ViewModels.Fields
{
    public class IntegerFieldViewModel : TextFieldViewModelBase
    {
        public IntegerFieldViewModel(string label, IntegerFieldMetadata metadata)
            : this(label, metadata.MinimumValue, metadata.MaximumValue)
        {
        }

        public IntegerFieldViewModel(string label, int minValue, int maxValue)
            : base(label, GetMaxLength(minValue, maxValue))
        {
            Label = label;
            MinValue = minValue;
            MaxValue = maxValue;
        }

        private static int GetMaxLength(int minValue, int maxValue)
        {
            return Math.Max(minValue.ToString().Length, maxValue.ToString().Length);
        }

        public static readonly ModelProperty ValueProperty =
            ModelProperty.Register(typeof(IntegerFieldViewModel), "Value", typeof(int?), null, OnValueChanged);

        private static void OnValueChanged(object sender, ModelPropertyChangedEventArgs e)
        {
            ((IntegerFieldViewModel)sender).Text = (e.NewValue == null) ? String.Empty : e.NewValue.ToString();
        }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        public int? Value
        {
            get { return (int?)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly ModelProperty MinValueProperty =
            ModelProperty.Register(typeof(IntegerFieldViewModel), "MinValue", typeof(int), 0);

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
        /// Gets or sets the minimum valid value.
        /// </summary>
        public int MinValue
        {
            get { return (int)GetValue(MinValueProperty); }
            set { SetValue(MinValueProperty, value); }
        }

        public static readonly ModelProperty MaxValueProperty =
            ModelProperty.Register(typeof(IntegerFieldViewModel), "MaxValue", typeof(int), Int32.MaxValue);

        /// <summary>
        /// Gets or sets the maximum valid value.
        /// </summary>
        public int MaxValue
        {
            get { return (int)GetValue(MaxValueProperty); }
            set { SetValue(MaxValueProperty, value); }
        }

        protected override string Validate(ModelProperty property, object value)
        {
            if (property == TextProperty)
            {
                if (String.IsNullOrEmpty((string)value))
                {
                    if (IsRequired)
                        return String.Format("{0} is required", LabelWithoutAccelerators);

                    Value = null;
                }
                else
                {
                    Int32 iVal;
                    if (!Int32.TryParse((string)value, out iVal))
                        return String.Format("{0} is not a valid number", LabelWithoutAccelerators);

                    if (iVal < MinValue)
                        return String.Format("{0} cannot be lower than {1}", LabelWithoutAccelerators, MinValue);

                    if (iVal > MaxValue)
                        return String.Format("{0} cannot be higher than {1}", LabelWithoutAccelerators, MaxValue);

                    Value = iVal;                    
                }
            }

            return base.Validate(property, value);
        }
    }
}
