using System.Collections.Generic;
using Jamiras.DataModels;
using Jamiras.ViewModels.Fields;

namespace Jamiras.ViewModels.Grid
{
    public class GridRowViewModel : ViewModelBase, ICompositeViewModel
    {
        internal GridRowViewModel(ModelBase model, IEnumerable<GridColumnDefinition> columns, ModelBindingMode bindingMode)
        {
            Model = model;

            int count = 0;
            foreach (var column in columns)
            {
                SetBinding(column.SourceProperty, new ModelBinding(model, column.SourceProperty, bindingMode));
                count++;
            }

            Cells = new FieldViewModelBase[count];
        }

        public ModelBase Model { get; private set; }

        protected override void OnModelPropertyChanged(ModelPropertyChangedEventArgs e)
        {
            // call HandleModelPropertyChanged and NotifyPropertyChangedHandlers instead of
            // base.OnModelPropertyChanged to avoid raising the ModelProperty.PropertyChangeHandlers
            // since we're just serving as a passthrough. 
            HandleModelPropertyChanged(e);
            OnPropertyChanged(e);
        }

        internal FieldViewModelBase[] Cells { get; private set; }
        internal GridRowCommandsViewModel Commands { get; set; }

        IEnumerable<ViewModelBase> ICompositeViewModel.GetChildren()
        {
            foreach (var cell in Cells)
            {
                if (cell != null)
                    yield return cell;
            }
        }
    }
}
