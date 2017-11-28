using System.Collections.Generic;

namespace Jamiras.ViewModels
{
    /// <summary>
    /// Allows a <see cref="ViewModelBase"/> to define a hierarchy for validation.
    /// </summary>
    public interface ICompositeViewModel
    {
        /// <summary>
        /// Gets the children of the view model.
        /// </summary>
        IEnumerable<ViewModelBase> GetChildren();
    }
}
