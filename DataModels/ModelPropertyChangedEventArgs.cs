using System;

namespace Jamiras.DataModels
{
    /// <summary>
    /// Information about a <see cref="ModelProperty"/> that changed.
    /// </summary>
    public class ModelPropertyChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Constructs a new <see cref="ModelPropertyChangedEventArgs"/>.
        /// </summary>
        public ModelPropertyChangedEventArgs(ModelProperty property, object oldValue, object newValue)
        {
            Property = property;
            OldValue = oldValue;
            NewValue = newValue;
        }

        /// <summary>
        /// Gets the <see cref="ModelProperty"/> that changed.
        /// </summary>
        public ModelProperty Property { get; private set; }

        /// <summary>
        /// Gets the old value for the property.
        /// </summary>
        public object OldValue { get; private set; }

        /// <summary>
        /// Gets the new value for the property.
        /// </summary>
        public object NewValue { get; private set; }
    }
}
