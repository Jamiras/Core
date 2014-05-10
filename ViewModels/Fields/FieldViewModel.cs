using System;
using System.Diagnostics;
using Jamiras.DataModels;

namespace Jamiras.ViewModels.Fields
{
    [DebuggerDisplay("{LabelWithoutAccelerators} {GetType().Name,nq}")]
    public abstract class FieldViewModelBase : ValidatedViewModelBase
    {
        public static readonly ModelProperty IsRequiredProperty =
            ModelProperty.Register(typeof(FieldViewModelBase), "IsRequired", typeof(bool), false);

        public bool IsRequired
        {
            get { return (bool)GetValue(IsRequiredProperty); }
            set { SetValue(IsRequiredProperty, value); }
        }

        public static readonly ModelProperty LabelProperty =
            ModelProperty.Register(typeof(FieldViewModelBase), "Label", typeof(string), "Value");

        public string Label
        {
            get { return (string)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }

        protected override string FormatErrorMessage(string errorMessage)
        {
            return String.Format(errorMessage, LabelWithoutAccelerators);
        }

        protected string LabelWithoutAccelerators
        {
            get
            {
                string label = Label ?? String.Empty;
                int idx = label.IndexOf('_');
                if (idx == -1)
                    return label;

                if (idx == 0)
                    return label.Substring(1);

                return label.Substring(0, idx) + label.Substring(idx + 1);
            }
        }
    }
}
