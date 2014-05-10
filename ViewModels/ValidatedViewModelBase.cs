using System;
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

        internal override void PushValue(ModelBinding binding, ModelProperty localProperty, object value)
        {
            string errorMessage = Validate(localProperty, value);

            if (binding.Converter != null)
                errorMessage = binding.Converter.ConvertBack(ref value);

            if (errorMessage == null)
            {
                if (_metadataRepository == null)
                    _metadataRepository = ServiceRepository.Instance.FindService<IDataModelMetadataRepository>();

                var metadata = _metadataRepository.GetModelMetadata(binding.Source.GetType());
                if (metadata != null)
                {
                    var fieldMetadata = metadata.GetFieldMetadata(binding.SourceProperty);
                    errorMessage = fieldMetadata.Validate(binding.Source, value);
                }
            }

            if (!String.IsNullOrEmpty(errorMessage))
            {
                errorMessage = FormatErrorMessage(errorMessage);
                _errors = _errors.AddOrUpdate(localProperty.PropertyName, errorMessage);
                IsValid = false;
            }
            else
            {
                if (_errors.Count > 0)
                {
                    _errors = _errors.Remove(localProperty.PropertyName);
                    if (_errors.Count == 0)
                        IsValid = true;
                }

                SynchronizeValue(binding.Source, binding.SourceProperty, value);
            }
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

        protected void SetError(ModelProperty property, string errorMessage)
        {
            if (!String.IsNullOrEmpty(errorMessage))
            {
                _errors = _errors.AddOrUpdate(property.PropertyName, errorMessage);
                IsValid = false;
            }
            else if (_errors.Count > 0)
            {
                _errors = _errors.Remove(property.PropertyName);
                if (_errors.Count == 0)
                    IsValid = true;
            }
        }

        string IDataErrorInfo.Error
        {
            get { return Validate(); }
        }

        string IDataErrorInfo.this[string columnName]
        {
            get
            {
                string error;
                if (_errors.TryGetValue(columnName, out error))
                    return error;

                return String.Empty;
            }
        }

        /// <summary>
        /// Gets the list of current errors associated to this view model.
        /// </summary>
        /// <returns>String containing all current errors for the view model (separated by newlines).</returns>
        public string Validate()
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
