using System;

namespace Jamiras.ViewModels
{
    /// <summary>
    /// <see cref="EventArgs"/> with a reference to a <see cref="ViewModelBase"/>.
    /// </summary>
    public class ViewModelEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ViewModelEventArgs"/> class.
        /// </summary>
        /// <param name="model">The model associated to the event.</param>
        public ViewModelEventArgs(ViewModelBase model)
        {
            ViewModel = model;
        }

        /// <summary>
        /// Gets the view model associated to the event.
        /// </summary>
        public ViewModelBase ViewModel { get; private set; }
    }
}
