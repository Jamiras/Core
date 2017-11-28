using System.Collections.Generic;
using Jamiras.DataModels;
using Jamiras.ViewModels.Fields;

namespace Jamiras.ViewModels.Grid
{
    /// <summary>
    /// Binds a <see cref="ModelBase"/> to a row of the <see cref="GridViewModel"/>.
    /// </summary>
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

        /// <summary>
        /// Gets the model bound to the row.
        /// </summary>
        public ModelBase Model { get; private set; }

        /// <summary>
        /// Notifies any subscribers that the value of a <see cref="ModelProperty" /> has changed.
        /// </summary>
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
