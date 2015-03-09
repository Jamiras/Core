using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Jamiras.DataModels;

namespace Jamiras.ViewModels.Grid
{
    public class GridViewModel : ViewModelBase, ICompositeViewModel
    {
        public GridViewModel()
        {
            Columns = new List<GridColumnDefinition>();
            Rows = new ObservableCollection<GridRowViewModel>();
        }

        public bool CanReorder { get; set; }
        public bool CanRemove { get; set; }

        public ICommand DoubleClickCommand { get; set; }

        public List<GridColumnDefinition> Columns { get; private set; }

        public ObservableCollection<GridRowViewModel> Rows { get; private set; }

        public GridRowViewModel AddRow(ModelBase model, ModelBindingMode bindingMode = ModelBindingMode.Committed)
        {
            var row = new GridRowViewModel(model, Columns, bindingMode);
            Rows.Add(row);
            return row;
        }

        IEnumerable<ViewModelBase> ICompositeViewModel.GetChildren()
        {
            foreach (var row in Rows)
                yield return row;
        }
    }
}
