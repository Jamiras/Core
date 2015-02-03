﻿using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Jamiras.Components;
using Jamiras.DataModels;
using Jamiras.DataModels.Metadata;

namespace Jamiras.ViewModels
{
    public abstract class ValidatedViewModelBase : ViewModelBase, IDataErrorInfo
    {
        protected ValidatedViewModelBase()
        {
            _errors = EmptyTinyDictionary<string, string>.Instance;

            ValidateDefaults();
        }

        private void ValidateDefaults()
        {
            foreach (var property in ModelProperty.GetPropertiesForType(GetType()))
            {
                var value = property.DefaultValue;
                var error = Validate(property, value);
                if (!String.IsNullOrEmpty(error))
                    SetError(property, error);
            }
        }

        private ITinyDictionary<string, string> _errors;
        private IDataModelMetadataRepository _metadataRepository;

        public static readonly ModelProperty IsValidProperty =
            ModelProperty.Register(typeof(ValidatedViewModelBase), "IsValid", typeof(bool), true);

        /// <summary>
        /// Gets whether or not the view model is valid (does not contain errors).
        /// </summary>
        public bool IsValid
        {
            get { return (bool)GetValue(IsValidProperty); }
            private set { SetValue(IsValidProperty, value); }
        }

        internal override void RefreshBinding(int localPropertyKey, ModelBinding binding)
        {
            var property = ModelProperty.GetPropertyForKey(localPropertyKey);

            string error;
            object value = binding.PullValue(out error);
            if (!String.IsNullOrEmpty(error))
            {
                SetError(property, error);
            }
            else
            {
                error = Validate(property, value);
                SetError(property, error);

                SynchronizeValue(this, property, value);
            }
        }

        internal override void HandleUnboundPropertyChanged(ModelPropertyChangedEventArgs e)
        {
            var error = Validate(e.Property, e.NewValue);
            SetError(e.Property, error);

            base.HandleUnboundPropertyChanged(e);
        }

        internal override void HandleBoundPropertyChanged(ModelBinding binding, ModelPropertyChangedEventArgs e, bool pushToSource)
        {
            var value = e.NewValue;

            // first, validate against the view model
            string errorMessage = Validate(e.Property, value);
            if (String.IsNullOrEmpty(errorMessage))
            {
                // then validate the value can be coerced into the source model
                if (binding.Converter != null)
                    errorMessage = binding.Converter.ConvertBack(ref value);

                if (String.IsNullOrEmpty(errorMessage))
                {
                    // finally, validate against the field metadata
                    var fieldMetadata = GetFieldMetadata(binding.Source.GetType(), binding.SourceProperty);
                    if (fieldMetadata != null)
                        errorMessage = fieldMetadata.Validate(binding.Source, value);
                }
            }

            if (!String.IsNullOrEmpty(errorMessage))
            {
                SetError(e.Property, FormatErrorMessage(errorMessage));
            }
            else
            {
                SetError(e.Property, null);

                base.HandleBoundPropertyChanged(binding, e, pushToSource);
            }
        }

        /// <summary>
        /// Gets the field metadata for a property of the view model.
        /// </summary>
        /// <param name="viewModelProperty">The property to get the metadata for.</param>
        /// <returns>Requested metadata, <c>null</c> if not found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="viewModelProperty"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="viewModelProperty"/> is not a property on the view model.</exception>
        /// <exception cref="InvalidOperationException">No binding exists for <param name="viewModelProperty"/>, so the metadata source cannot be determined.</exception>
        protected FieldMetadata GetFieldMetadata(ModelProperty viewModelProperty)
        {
            if (viewModelProperty == null)
                throw new ArgumentNullException("viewModelProperty");
            if (!viewModelProperty.OwnerType.IsAssignableFrom(GetType()))
                throw new ArgumentException(viewModelProperty.FullName + " is not a property on " + GetType().FullName, "viewModelProperty");

            var binding = GetBinding(viewModelProperty);
            if (binding == null)
                throw new InvalidOperationException("Cannot determine metadata source without a binding: " + viewModelProperty.FullName);

            return GetFieldMetadata(binding.Source.GetType(), binding.SourceProperty);
        }

        /// <summary>
        /// Gets the field metadata for a property of a model.
        /// </summary>
        /// <param name="modelType">The model type where the metadata is registered.</param>
        /// <param name="modelProperty">The property to get the metadata for.</param>
        /// <returns>Requested metadata, <c>null</c> if not found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="modelType"/> or <paramref name="modelProperty"/> is null</exception>
        protected FieldMetadata GetFieldMetadata(Type modelType, ModelProperty modelProperty)
        {
            if (modelType == null)
                throw new ArgumentNullException("modelType");
            if (modelProperty == null)
                throw new ArgumentNullException("modelProperty");

            if (_metadataRepository == null)
                _metadataRepository = ServiceRepository.Instance.FindService<IDataModelMetadataRepository>();

            var metadata = _metadataRepository.GetModelMetadata(modelType);
            if (metadata != null)
                return metadata.GetFieldMetadata(modelProperty);

            return null;
        }

        protected virtual string FormatErrorMessage(string errorMessage)
        {
            return String.Format(errorMessage, "Value");
        }

        /// <summary>
        /// Validates a value being assigned to a property.
        /// </summary>
        /// <param name="property">Property being modified.</param>
        /// <param name="value">Value being assigned to the property.</param>
        /// <returns><c>null</c> if the value is valid for the property, or an error message indicating why the value is not valid.</returns>
        protected virtual string Validate(ModelProperty property, object value)
        {
            return null;
        }

        #region IDataErrorInfo Members

        /// <summary>
        /// Sets the error message for a property.
        /// </summary>
        /// <param name="property">Property to set error message for.</param>
        /// <param name="errorMessage">Message to set, <c>null</c> to clear message.</param>
        protected void SetError(ModelProperty property, string errorMessage)
        {
            SetError(property.PropertyName, errorMessage);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        protected void SetError(string propertyName, string errorMessage)
        {
            if (!String.IsNullOrEmpty(errorMessage))
            {
                _errors = _errors.AddOrUpdate(propertyName, errorMessage);
                IsValid = false;
            }
            else if (_errors.Count > 0)
            {
                _errors = _errors.Remove(propertyName);
                if (_errors.Count == 0)
                    IsValid = true;
            }
        }

        string IDataErrorInfo.Error
        {
            get { return Validate(); }
        }

        string IDataErrorInfo.this[string propertyName]
        {
            get
            {
                string error;
                if (_errors.TryGetValue(propertyName, out error))
                    return error;

                return String.Empty;
            }
        }

        /// <summary>
        /// Gets the list of current errors associated to this view model.
        /// </summary>
        /// <returns>String containing all current errors for the view model (separated by newlines).</returns>
        public virtual string Validate()
        {
            StringBuilder builder = new StringBuilder();
            AppendErrorMessages(builder);

            var compositeViewModel = this as ICompositeViewModel;
            if (compositeViewModel != null)
            {
                foreach (var child in compositeViewModel.GetChildren().OfType<ValidatedViewModelBase>())
                    child.AppendErrorMessages(builder);
            }

            while (builder.Length > 0 && Char.IsWhiteSpace(builder[builder.Length - 1]))
                builder.Length--;

            return builder.ToString();
        }

        private void AppendErrorMessages(StringBuilder builder)
        {
            foreach (var kvp in _errors)
            {
                if (!String.IsNullOrEmpty(kvp.Value))
                    builder.AppendLine(kvp.Value);
            }
        }

        #endregion
    }
}
