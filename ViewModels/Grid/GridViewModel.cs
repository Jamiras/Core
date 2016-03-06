using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using Jamiras.DataModels;
using Jamiras.Components;
using Jamiras.Services;
using System.Windows;
using System.Windows.Media;

namespace Jamiras.ViewModels.Grid
{
    [DebuggerDisplay("GridViewModel (Rows = {Rows.Count})")]
    public class GridViewModel : ViewModelBase, ICompositeViewModel
    {
        public GridViewModel()
        {
            Columns = new List<GridColumnDefinition>();
            Rows = new ObservableCollection<GridRowViewModel>();
        }

        /// <summary>
        /// Gets or sets whether reorder arrows should appear for each row.
        /// </summary>
        public bool CanReorder { get; set; }

        /// <summary>
        /// Gets or sets whether remove icon should appear for each row.
        /// </summary>
        public bool CanRemove { get; set; }

        /// <summary>
        /// Gets or sets whether rows can be dynamically added by tabbing past the last field of the last row.
        /// If set to <c>true</c>, make sure to also set the <see cref="GenerateDynamicRow" /> property.
        /// </summary>
        public bool CanAddRowsDynamically { get; set; }

        /// <summary>
        /// Sets the method to call to initially populate a dynamically generated row.
        /// </summary>
        public Func<ModelBase> GenerateDynamicRow { get; set; }

        /// <summary>
        /// Specifies the command to call if a row is double clicked.
        /// </summary>
        /// <remarks>
        /// CommandParameter will be the GridRowViewModel that was double clicked
        /// </remarks>
        public ICommand DoubleClickCommand { get; set; }

        /// <summary>
        /// Gets the collection of columns to display in the grid.
        /// </summary>
        public List<GridColumnDefinition> Columns { get; private set; }

        /// <summary>
        /// Gets the collection of rows currently displayed in the grid.
        /// </summary>
        public ObservableCollection<GridRowViewModel> Rows { get; private set; }

        /// <summary>
        /// Adds a row to the end of the grid.
        /// </summary>
        /// <param name="model">The model to bind to the new row.</param>
        /// <param name="bindingMode">How to bind the model to the new row.</param>
        /// <returns>The newly created row.</returns>
        public GridRowViewModel AddRow(ModelBase model, ModelBindingMode bindingMode = ModelBindingMode.Committed)
        {
            var row = new GridRowViewModel(model, Columns, bindingMode);
            Rows.Add(row);
            return row;
        }

        /// <summary>
        /// Adds a row at the specified index within the grid.
        /// </summary>
        /// <param name="model">The model to bind to the new row.</param>
        /// <param name="bindingMode">How to bind the model to the new row.</param>
        /// <returns>The newly created row.</returns>
        public GridRowViewModel InsertRow(int index, ModelBase model, ModelBindingMode bindingMode = ModelBindingMode.Committed)
        {
            var row = new GridRowViewModel(model, Columns, bindingMode);
            Rows.Insert(index, row);
            return row;
        }

        /// <summary>
        /// Gets the row for the provided model.
        /// </summary>
        /// <param name="model">The model to find.</param>
        /// <returns>The first row bound to the model, or <c>null</c> if not found.</returns>
        public GridRowViewModel GetRow(ModelBase model)
        {
            return Rows.FirstOrDefault(r => ReferenceEquals(r.Model, model));
        }

        /// <summary>
        /// Removes the row associated to the provided model.
        /// </summary>
        /// <param name="model">The model to find.</param>
        /// <returns><c>true</c> if the row was removed, <c>false</c> if it was not found.</returns>
        public bool RemoveRow(ModelBase model)
        {
            var row = GetRow(model);
            if (row == null)
                return false;

            Rows.Remove(row);
            return true;
        }

        IEnumerable<ViewModelBase> ICompositeViewModel.GetChildren()
        {
            foreach (var row in Rows)
                yield return row;
        }

        public GridRowViewModel GetFocusedRow()
        {
            GridRowViewModel row = null;

            var service = ServiceRepository.Instance.FindService<IBackgroundWorkerService>();
            service.InvokeOnUiThread(() =>
            {
                DependencyObject obj = Keyboard.FocusedElement as DependencyObject;
                while (obj != null)
                {
                    var element = obj as FrameworkElement;
                    if (element != null)
                    {
                        row = element.DataContext as GridRowViewModel;
                        if (row != null)
                            break;
                    }

                    obj = VisualTreeHelper.GetParent(obj);
                }
            });

            if (row != null && Rows.Contains(row))
                return row;

            return null;
        }
    }
}
