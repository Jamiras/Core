using Jamiras.DataModels;
using System;
using System.Diagnostics;
using System.Text;

namespace Jamiras.ViewModels.Fields
{
    /// <summary>
    /// Base class for templated field controls.
    /// </summary>
    [DebuggerDisplay("{LabelWithoutAccelerators} {GetType().Name,nq}")]
    public abstract class FieldViewModelBase : ValidatedViewModelBase
    {
        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="IsRequired"/>
        /// </summary>
        public static readonly ModelProperty IsRequiredProperty =
            ModelProperty.Register(typeof(FieldViewModelBase), "IsRequired", typeof(bool), false);

        /// <summary>
        /// Gets or sets whether the field is required.
        /// </summary>
        public bool IsRequired
        {
            get { return (bool)GetValue(IsRequiredProperty); }
            set { SetValue(IsRequiredProperty, value); }
        }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="IsEnabled"/>
        /// </summary>
        public static readonly ModelProperty IsEnabledProperty =
            ModelProperty.Register(typeof(FieldViewModelBase), "IsEnabled", typeof(bool), true);

        /// <summary>
        /// Gets or sets whether the field is enabled.
        /// </summary>
        public bool IsEnabled
        {
            get { return (bool)GetValue(IsEnabledProperty); }
            set { SetValue(IsEnabledProperty, value); }
        }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="Label"/>
        /// </summary>
        public static readonly ModelProperty LabelProperty =
            ModelProperty.Register(typeof(FieldViewModelBase), "Label", typeof(string), "Value");

        /// <summary>
        /// Gets or sets a label to associate with the field.
        /// </summary>
        /// <remarks>
        /// Should include one letter prefixed with an underscore to use as an accelerator for the field.
        /// </remarks>
        public string Label
        {
            get { return (string)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }

        private static readonly ModelProperty CustomNameProperty =
            ModelProperty.Register(typeof(FieldViewModelBase), null, typeof(string), null);

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="Name"/>
        /// </summary>
        public static readonly ModelProperty NameProperty =
            ModelProperty.RegisterDependant(typeof(FieldViewModelBase), "Name", typeof(string),
                new ModelProperty[] { LabelProperty, CustomNameProperty }, GetName);

        /// <summary>
        /// Gets or sets the unique identifier for the field.
        /// </summary>
        public string Name
        {
            get { return (string)GetValue(NameProperty); }
            set { SetValue(CustomNameProperty, value); }
        }

        private static string GetName(ModelBase model)
        {
            var fieldViewModel = (FieldViewModelBase)model;
            var customName = (string)fieldViewModel.GetValue(CustomNameProperty);
            if (customName != null)
                return customName;

            var builder = new StringBuilder();
            var makeUpper = false;
            var makeLower = true;
            var seenAccelerator = false;
            foreach (var c in fieldViewModel.Label ?? String.Empty)
            {
                if (Char.IsLetterOrDigit(c))
                {
                    if (makeUpper)
                    {
                        makeUpper = false;
                        builder.Append(Char.ToUpper(c));
                        makeLower = true;
                    }
                    else if (makeLower)
                    {
                        builder.Append(Char.ToLower(c));
                    }
                    else
                    {
                        builder.Append(c);
                        makeLower = true;
                    }
                }
                else if (c == '_' && !seenAccelerator)
                {
                    seenAccelerator = true;
                    makeLower = false;
                }
                else
                {
                    makeUpper = true;
                    makeLower = false;
                }
            }

            var typeName = fieldViewModel.GetType().Name;
            int index = typeName.IndexOf("ViewModel", StringComparison.Ordinal);
            if (index > 0)
                builder.Append(typeName, 0, index);
            else
                builder.Append(typeName);

            return builder.ToString();
        }

        /// <summary>
        /// Formats an error message.
        /// </summary>
        protected override string FormatErrorMessage(string errorMessage)
        {
            return String.Format(errorMessage, LabelWithoutAccelerators);
        }

        /// <summary>
        /// Gets the value of the <see cref="Label"/> property with the accelerators removed.
        /// </summary>
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
