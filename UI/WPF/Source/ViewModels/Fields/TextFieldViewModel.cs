﻿using Jamiras.DataModels;
using Jamiras.DataModels.Metadata;

namespace Jamiras.ViewModels.Fields
{
    /// <summary>
    /// ViewModel for string input.
    /// </summary>
    public class TextFieldViewModel : TextFieldViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TextFieldViewModel"/> class.
        /// </summary>
        /// <param name="label">The field label.</param>
        /// <param name="metadata">Information about the field.</param>
        public TextFieldViewModel(string label, StringFieldMetadata metadata)
            : base(label, metadata.MaxLength)
        {
            IsMultiline = metadata.IsMultiline;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextFieldViewModel"/> class.
        /// </summary>
        /// <param name="label">The field label.</param>
        /// <param name="maxLength">The maximum length of the field.</param>
        public TextFieldViewModel(string label, int maxLength)
            : base(label, maxLength)
        {
        }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="IsMultiline"/>
        /// </summary>
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

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="IsRightAligned"/>
        /// </summary>
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
        /// <see cref="ModelProperty"/> for <see cref="Pattern"/>
        /// </summary>
        public static readonly ModelProperty PatternProperty =
            ModelProperty.Register(typeof(TextFieldViewModel), "Pattern", typeof(string), null);

        /// <summary>
        /// Gets or sets the pattern mask for the field (#=number, A=letter, ?=any, all others are fixed characters).
        /// </summary>
        public string Pattern
        {
            get { return (string)GetValue(PatternProperty); }
            set { SetValue(PatternProperty, value); }
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
