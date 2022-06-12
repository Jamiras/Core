using Jamiras.DataModels;

namespace Jamiras.ViewModels.Fields
{
    /// <summary>
    /// ViewModel for readonly string data.
    /// </summary>
    public class ReadOnlyTextFieldViewModel : FieldViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadOnlyTextFieldViewModel"/> class.
        /// </summary>
        /// <param name="label">The field label.</param>
        public ReadOnlyTextFieldViewModel(string label)
        {
            Label = label;
        }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="Text"/>
        /// </summary>
        public static readonly ModelProperty TextProperty =
            ModelProperty.Register(typeof(ReadOnlyTextFieldViewModel), "Text", typeof(string), null);

        /// <summary>
        /// Gets or sets the text in the field.
        /// </summary>
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="IsRightAligned"/>
        /// </summary>
        public static readonly ModelProperty IsRightAlignedProperty =
            ModelProperty.Register(typeof(ReadOnlyTextFieldViewModel), "IsRightAligned", typeof(bool), false);

        /// <summary>
        /// Gets or sets whether the text should be right aligned within the text box.
        /// </summary>
        public bool IsRightAligned
        {
            get { return (bool)GetValue(IsRightAlignedProperty); }
            set { SetValue(IsRightAlignedProperty, value); }
        }
    }
}
