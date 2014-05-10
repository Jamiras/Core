using System.Collections.Generic;

namespace Jamiras.ViewModels
{
    public interface ICompositeViewModel
    {
        IEnumerable<ViewModelBase> GetChildren();
    }
}
