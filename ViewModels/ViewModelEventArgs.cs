using System;

namespace Jamiras.ViewModels
{
    public class ViewModelEventArgs : EventArgs
    {
        public ViewModelEventArgs(ViewModelBase model)
        {
            ViewModel = model;
        }

        public ViewModelBase ViewModel { get; private set; }
    }
}
