using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Jamiras.Components;

namespace Jamiras.ViewModels
{
    public abstract class ViewModelBase : PropertyChangedObject, IDataErrorInfo
    {
        protected ViewModelBase()
        {
            _validationFunctions = new Dictionary<string, Func<string>>();
            _errorState = new Dictionary<string, string>();
            _isValid = true;
        }

        /// <summary>
        /// Gets whether the provided values are valid
        /// </summary>
        public bool IsValid
        {
            get { return _isValid; }
            private set
            {
                if (_isValid != value)
                {
                    _isValid = value;
                    OnPropertyChanged(() => IsValid);
                }
            }
        }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _isValid;

        /// <summary>
        /// Raised whenever a property of the ViewModel changes.
        /// </summary>
        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            bool revalidate = false;

            Func<string> validationFunction;
            if (_validationFunctions.TryGetValue(e.PropertyName, out validationFunction))
            {
                string error = validationFunction();

                string currentError;
                if (!_errorState.TryGetValue(e.PropertyName, out currentError) || currentError != error)
                {
                    _errorState[e.PropertyName] = error;
                    revalidate = true;
                }
            }

            base.OnPropertyChanged(e);

            if (revalidate)
                IsValid = !_errorState.Any(kvp => !String.IsNullOrEmpty(kvp.Value));
        }

        #region IDataErrorInfo Members

        private readonly Dictionary<string, Func<string>> _validationFunctions;
        private readonly Dictionary<string, string> _errorState;

        protected void AddValidation(string property, Func<string> validationFunction)
        {
            _validationFunctions[property] = validationFunction;

            string error = validationFunction();
            _errorState[property] = error;

            if (!String.IsNullOrEmpty(error))
                IsValid = false;
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
                if (_errorState.TryGetValue(columnName, out error))
                    return error;

                return String.Empty;
            }
        }

        /// <summary>
        /// Gets the list of current errors
        /// </summary>
        /// <returns></returns>
        public virtual string Validate()
        {
            StringBuilder builder = new StringBuilder();
            foreach (var kvp in _errorState)
            {
                if (!String.IsNullOrEmpty(kvp.Value))
                    builder.AppendLine(kvp.Value);
            }

            return builder.ToString().TrimEnd();
        }

        #endregion
    }
}
