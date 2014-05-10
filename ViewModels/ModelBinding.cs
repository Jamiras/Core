using System;
using System.Diagnostics;
using Jamiras.DataModels;
using Jamiras.ViewModels.Converters;

namespace Jamiras.ViewModels
{
    public class ModelBinding
    {
        /// <param name="source">Source object.</param>
        /// <param name="sourceProperty">Property to bind to on source object.</param>
        public ModelBinding(ModelBase source, ModelProperty sourceProperty)
            : this(source, sourceProperty, ModelBindingMode.TwoWay)
        {
        }

        /// <param name="source">Source object.</param>
        /// <param name="sourceProperty">Property to bind to on source object.</param>
        /// <param name="mode">When to synchronize data.</param>
        public ModelBinding(ModelBase source, ModelProperty sourceProperty, ModelBindingMode mode)
        {
            Source = source;
            SourceProperty = sourceProperty;
            Mode = mode;
        }

        /// <param name="source">Source object.</param>
        /// <param name="sourceProperty">Property to bind to on source object.</param>
        /// <param name="converter">Maps data to and from the source property.</param>
        public ModelBinding(ModelBase source, ModelProperty sourceProperty, IConverter converter)
            : this(source, sourceProperty)
        {
            Converter = converter;
        }

        /// <param name="source">Source object.</param>
        /// <param name="sourceProperty">Property to bind to on source object.</param>
        /// <param name="mode">When to synchronize data.</param>
        /// <param name="converter">Maps data to and from the source property.</param>
        public ModelBinding(ModelBase source, ModelProperty sourceProperty, ModelBindingMode mode, IConverter converter)
            : this(source, sourceProperty, mode)
        {
            Converter = converter;
        }

        public override string ToString()
        {   
            return String.Format("{0} {1} {2}", SourceProperty.FullName, Mode, Source);
        }

        /// <summary>
        /// Gets the source object for the binding.
        /// </summary>
        public ModelBase Source { get; private set; }

        /// <summary>
        /// Gets the property on the source object that is bound.
        /// </summary>
        public ModelProperty SourceProperty { get; private set; }

        /// <summary>
        /// Gets when to synchronize data to/from the source object.
        /// </summary>
        public ModelBindingMode Mode { get; private set; }

        /// <summary>
        /// Gets the converter used to map data to and from the source property.
        /// </summary>
        public IConverter Converter { get; private set; }

        /// <summary>
        /// Pulls a value from the source object.
        /// </summary>
        /// <param name="errorMessage"><c>null</c> if the value was pulled successfully, or an error message indicating why it was not.</param>
        /// <returns>The value from the source object.</returns>
        public object PullValue(out string errorMessage)
        {
            var value = Source.GetValue(SourceProperty);

            if (Converter != null)
            {
                errorMessage = Converter.Convert(ref value);
                if (!String.IsNullOrEmpty(errorMessage))
                    value = null;
            }
            else
            {
                errorMessage = null;
            }

            return value;
        }

        /// <summary>
        /// Attempts to pull a value from the source object.
        /// </summary>
        /// <param name="value"></param>
        /// <returns><c>true</c> if the value was retrieved, <c>false</c> if not.</returns>
        public bool TryPullValue(out object value)
        {
            string errorMessage;
            value = PullValue(out errorMessage);
            return (String.IsNullOrEmpty(errorMessage));
        }

        /// <summary>
        /// Pushes a value to the source object.
        /// </summary>
        /// <param name="value">The value to push to the source object.</param>
        /// <returns><c>null</c> if the value was pushed successfully, or an error message indicating why it was not.</returns>
        public string PushValue(object value)
        {
            if (Converter != null)
            {
                string errorMessage = Converter.ConvertBack(ref value);
                if (!String.IsNullOrEmpty(errorMessage))
                    return errorMessage;
            }

            Source.SetValue(SourceProperty, value);
            return null;
        }

        /// <summary>
        /// Attempts to push a value to the source object.
        /// </summary>
        /// <param name="value">The value to push to the source object.</param>
        /// <returns><c>true</c> if the source object was updated, <c>false</c> if not.</returns>
        public bool TryPushValue(object value)
        {
            string errorMessage = PushValue(value);
            return (String.IsNullOrEmpty(errorMessage));
        }
    }

    public enum ModelBindingMode
    {
        /// <summary>
        /// Unspecified.
        /// </summary>
        None = 0,

        /// <summary>
        /// The view model and source are kept in sync.
        /// </summary>
        TwoWay,

        /// <summary>
        /// Values are read from the source, but not written back to it.
        /// </summary>
        OneWay,

        /// <summary>
        /// Values are read from the source if the view model value has not changed, but not written back to the source until the view model is committed.
        /// </summary>
        Committed,
    }
}
