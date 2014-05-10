using System.Diagnostics;
using Jamiras.DataModels;

namespace Jamiras.ViewModels.Fields
{
    [DebuggerDisplay("{Label} {GetType().Name,nq}")]
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
    }
}
