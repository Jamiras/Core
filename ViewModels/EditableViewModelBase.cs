using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Jamiras.Components;
using Jamiras.DataModels;
using Jamiras.DataModels.Metadata;

namespace Jamiras.ViewModels
{
    public abstract class EditableViewModelBase : ViewModelBase, IDataErrorInfo
    {
        protected EditableViewModelBase()
        {
            _errors = EmptyTinyDictionary<string, string>.Instance;
        }

        private ITinyDictionary<string, string> _errors;
        private IDataModelMetadataRepository _metadataRepository;

        private static readonly ModelProperty IsAutoCommitProperty = 
            ModelProperty.Register(typeof(EditableViewModelBase), "IsAutoCommit", typeof(bool), false);

        /// <summary>
        /// Gets or sets whether modifications to the view model are automatically pushed to the bound data models
        /// </summary>
        protected bool IsAutoCommit 
        {
            get { return (bool)GetValue(IsAutoCommitProperty); }
            set { SetValue(IsAutoCommitProperty, value); }
        }

        public static readonly ModelProperty IsValidProperty =
            ModelProperty.Register(typeof(EditableViewModelBase), "IsValid", typeof(bool), true);

        /// <summary>
        /// Gets whether or not the view model is valid (does not contain errors).
        /// </summary>
        public bool IsValid
        {
            get { return (bool)GetValue(IsValidProperty); }
            private set { SetValue(IsValidProperty, value); }
        }

        protected override void OnModelPropertyChanged(ModelPropertyChangedEventArgs e)
        {
            if (e.Property.Key != BindingModelPropertyKey)
            {
                foreach (var binding in Bindings.Where(b => b.TargetProperty == e.Property))
                    UpdateSource(binding, e.NewValue);
            }

            base.OnModelPropertyChanged(e);
        }

        private void UpdateSource(ModelBinding binding, object newValue)
        {
            var value = newValue;
            if (binding.Converter != null)
                value = binding.Converter.ConvertBack(value);

            if (_metadataRepository == null)
                _metadataRepository = ServiceRepository.Instance.FindService<IDataModelMetadataRepository>();

            var metadata = _metadataRepository.GetModelMetadata(binding.Source.GetType());
            var fieldMetadata = metadata.GetFieldMetadata(binding.SourceProperty);

            var error = fieldMetadata.Validate(binding.Source, value);
            if (!String.IsNullOrEmpty(error))
            {
                _errors = _errors.AddOrUpdate(binding.TargetProperty.PropertyName, error);
                IsValid = false;
            }
            else
            {
                if (_errors.Count > 0)
                {
                    _errors = _errors.Remove(binding.TargetProperty.PropertyName);
                    if (_errors.Count == 0)
                        IsValid = true;
                }

                if (IsAutoCommit)
                {
                    BindingModelPropertyKey = binding.SourceProperty.Key;
                    try
                    {
                        binding.Source.SetValue(binding.SourceProperty, value);
                    }
                    finally
                    {
                        BindingModelPropertyKey = -1;
                    }
                }
            }
        }

        /// <summary>
        /// Commits any pending changes to the bound models. Warning: does not do validation.
        /// </summary>
        public void Commit()
        {
            foreach (var nestedViewModel in GetNestedViewModels().OfType<EditableViewModelBase>())
                nestedViewModel.Commit();

            foreach (var binding in Bindings)
            {
                var value = GetValue(binding.TargetProperty);
                if (binding.Converter != null)
                    value = binding.Converter.ConvertBack(value);

                BindingModelPropertyKey = binding.SourceProperty.Key;
                binding.Source.SetValue(binding.SourceProperty, value);
                BindingModelPropertyKey = -1;
            }
        }

        #region IDataErrorInfo Members

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
        public virtual string Validate()
        {
            StringBuilder builder = new StringBuilder();
            foreach (var kvp in _errors)
            {
                if (!String.IsNullOrEmpty(kvp.Value))
                    builder.AppendLine(kvp.Value);
            }

            foreach (var nestedViewModel in GetNestedViewModels().OfType<EditableViewModelBase>())
            {
                var nestedValidation = nestedViewModel.Validate();
                if (!String.IsNullOrEmpty(nestedValidation))
                    builder.AppendLine(nestedValidation);
            }

            while (builder.Length > 0 && Char.IsWhiteSpace(builder[builder.Length - 1]))
                builder.Length--;

            return builder.ToString();
        }

        #endregion
    }
}
