using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Jamiras.Components
{
    /// <summary>
    /// Base class implementing INotifyPropertyChanged with a helper function for compile-time PropertyChanged event raising.
    /// </summary>
    public abstract class PropertyChangedObject : INotifyPropertyChanged
    {
        /// <summary>
        /// Raised whenever the value of a property of the ViewModel changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the PropertyChanged event.
        /// </summary>
        /// <param name="e">Information about which property changed</param>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, e);
        }

        /// <summary>
        /// Raises the property changed event for a property of this ViewModel.
        /// Example: OnPropertyChanged( () => PropertyName )
        /// </summary>
        /// <typeparam name="TPropertyType">The type of the property (can be inferred)</typeparam>
        /// <param name="expression">A lambda expression for the property: () => PropertyName</param>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "Lambda inference at the call site doesn't work without the derived type.")]
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        protected void OnPropertyChanged<TPropertyType>(Expression<Func<TPropertyType>> expression)
        {
            string propertyName = expression.GetMemberName();
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }
    }
}
