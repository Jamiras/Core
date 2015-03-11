using System;
using Jamiras.DataModels;
using Jamiras.DataModels.Metadata;

namespace Jamiras.ViewModels.Fields
{
    public class TextFieldViewModel : TextFieldViewModelBase
    {
        public TextFieldViewModel(string label, StringFieldMetadata metadata)
            : base(label, metadata.MaxLength)
        {
            IsMultiline = metadata.IsMultiline;
        }

        public TextFieldViewModel(string label, int maxLength)
            : base(label, maxLength)
        {
        }

        public static readonly ModelProperty IsMultilineProperty =
            ModelProperty.Register(typeof(TextFieldViewModel), "IsMultiline", typeof(bool), false);

        /// <summary>
        /// Gets or sets whether the field supports newlines.
        /// </summary>
        public bool IsMultiline
        {
            get { return (bool)GetValue(IsMultilineProperty); }
            set { SetValue(IsMultilineProperty, value); }
        }

        public static readonly ModelProperty IsRightAlignedProperty =
            ModelProperty.Register(typeof(TextFieldViewModel), "IsRightAligned", typeof(bool), false);

        /// <summary>
        /// Gets or sets whether the text should be right aligned within the text box.
        /// </summary>
        public bool IsRightAligned
        {
            get { return (bool)GetValue(IsRightAlignedProperty); }
            set { SetValue(IsRightAlignedProperty, value); }
        }

        /// <summary>
        /// Binds the ViewModel to a source model.
        /// </summary>
        /// <param name="source">Model to bind to.</param>
        /// <param name="property">Property on model to bind to.</param>
        /// <param name="mode">How to bind to the source model.</param>
        public void BindText(ModelBase source, ModelProperty property, ModelBindingMode mode = ModelBindingMode.Committed)
        {
            SetBinding(TextProperty, new ModelBinding(source, property, mode));
        }
    }
}
