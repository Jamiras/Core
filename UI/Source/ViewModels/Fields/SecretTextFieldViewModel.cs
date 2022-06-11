using Jamiras.Commands;
using Jamiras.DataModels;
using Jamiras.DataModels.Metadata;
using System.Linq;

namespace Jamiras.ViewModels.Fields
{
    /// <summary>
    /// ViewModel for hideable string input.
    /// </summary>
    public class SecretTextFieldViewModel : TextFieldViewModelBase
    {
        public const char MaskChar = '\x2022';

        /// <summary>
        /// Initializes a new instance of the <see cref="SecretTextFieldViewModel"/> class.
        /// </summary>
        /// <param name="label">The field label.</param>
        /// <param name="metadata">Information about the field.</param>
        public SecretTextFieldViewModel(string label, StringFieldMetadata metadata)
            : this(label, metadata.MaxLength)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecretTextFieldViewModel"/> class.
        /// </summary>
        /// <param name="label">The field label.</param>
        /// <param name="maxLength">The maximum length of the field.</param>
        public SecretTextFieldViewModel(string label, int maxLength)
            : base(label, maxLength)
        {
            ToggleIsUnmaskedCommand = new DelegateCommand(ToggleIsUnmasked);
        }

        private bool _isSynchronizingSecretText = false;

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="IsUnmasked"/>
        /// </summary>
        public static readonly ModelProperty IsUnmaskedProperty =
            ModelProperty.Register(typeof(SecretTextFieldViewModel), "IsUnmasked", typeof(bool), false);

        /// <summary>
        /// Gets or sets whether the <see cref="Text"/> field is being masked.
        /// </summary>
        public bool IsUnmasked
        {
            get { return (bool)GetValue(IsUnmaskedProperty); }
            set { SetValue(IsUnmaskedProperty, value); }
        }

        /// <summary>
        /// Gets the command to toggle the <see cref="IsUnmasked"/> property.
        /// </summary>
        public CommandBase ToggleIsUnmaskedCommand { get; private set; }

        private void ToggleIsUnmasked()
        {
            IsUnmasked = !IsUnmasked;
        }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="Pattern"/>
        /// </summary>
        public static readonly ModelProperty SecretTextProperty =
            ModelProperty.Register(typeof(SecretTextFieldViewModel), "SecretText", typeof(string), null);

        /// <summary>
        /// Gets or sets the unmasked text for the field.
        /// </summary>
        public string SecretText
        {
            get { return (string)GetValue(SecretTextProperty); }
            set { SetValue(SecretTextProperty, value); }
        }

        /// <summary>
        /// Binds the ViewModel to a source model.
        /// </summary>
        /// <param name="source">Model to bind to.</param>
        /// <param name="property">Property on model to bind to.</param>
        /// <param name="mode">How to bind to the source model.</param>
        public void BindSecretText(ModelBase source, ModelProperty property, ModelBindingMode mode = ModelBindingMode.Committed)
        {
            SetBinding(SecretTextProperty, new ModelBinding(source, property, mode));
        }

        protected override void OnModelPropertyChanged(ModelPropertyChangedEventArgs e)
        {
            if (!_isSynchronizingSecretText)
            {
                if (e.Property == SecretTextProperty)
                {
                    _isSynchronizingSecretText = true;

                    if (IsUnmasked)
                        SetText((string)e.NewValue);
                    else
                        SetText(new string(MaskChar, ((string)e.NewValue).Length));

                    _isSynchronizingSecretText = false;
                }
                else if (e.Property == TextProperty)
                {
                    var newSecretText = (string)e.NewValue;
                    if (newSecretText.Length > 0 && newSecretText.All(c => c == MaskChar))
                    {
                        // only masked characters, ignore. assume the user is deleting the old value
                    }
                    else
                    {
                        _isSynchronizingSecretText = true;

                        SecretText = (string)e.NewValue;

                        _isSynchronizingSecretText = false;

                        if (!IsUnmasked)
                        {
                            SetText(new string(MaskChar, ((string)e.NewValue).Length));
                            return;
                        }
                    }
                }
            }

            if (e.Property == IsUnmaskedProperty)
            {
                _isSynchronizingSecretText = true;

                if ((bool)e.NewValue)
                    SetText(SecretText);
                else
                    SetText(new string(MaskChar, SecretText.Length));

                _isSynchronizingSecretText = false;
            }

            base.OnModelPropertyChanged(e);
        }
    }
}
