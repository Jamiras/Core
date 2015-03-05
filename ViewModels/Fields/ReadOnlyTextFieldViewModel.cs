using Jamiras.DataModels;

namespace Jamiras.ViewModels.Fields
{
    public class ReadOnlyTextFieldViewModel : FieldViewModelBase
    {
        public ReadOnlyTextFieldViewModel(string label)
        {
            Label = label;
        }

        public static readonly ModelProperty TextProperty =
            ModelProperty.Register(typeof(ReadOnlyTextFieldViewModel), "Text", typeof(string), null);

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly ModelProperty IsRightAlignedProperty =
            ModelProperty.Register(typeof(ReadOnlyTextFieldViewModel), "IsRightAligned", typeof(bool), false);

        public bool IsRightAligned
        {
            get { return (bool)GetValue(IsRightAlignedProperty); }
            set { SetValue(IsRightAlignedProperty, value); }
        }
    }
}
