using System.Collections;
using Jamiras.Commands;
using Jamiras.Components;
using Jamiras.Controls;

namespace Jamiras.ViewModels.Grid
{
    public class GridRowCommandsViewModel : PropertyChangedObject
    {
        public GridRowCommandsViewModel(GridView view, GridRowViewModel row)
        {
            _view = view;
            _row = row;

            RemoveCommand = new DelegateCommand(Remove);

            if (view.CanReorder)
            {
                MoveUpCommand = new DelegateCommand(MoveUp);
                MoveDownCommand = new DelegateCommand(MoveDown);

                UpdateMoveCommands();
            }
        }

        private readonly GridView _view;
        private readonly GridRowViewModel _row;
        private bool _isMoveUpEnabled, _isMoveDownEnabled;

        public bool CanRemove
        {
            get { return _view.CanRemove; }
        }

        public bool CanReorder
        {
            get { return _view.CanReorder; }
        }

        public bool IsMoveUpEnabled
        {
            get { return _isMoveUpEnabled; }
            private set
            {
                if (_isMoveUpEnabled != value)
                {
                    _isMoveUpEnabled = value;
                    OnPropertyChanged(() => IsMoveUpEnabled);
                }
            }
        }

        public bool IsMoveDownEnabled
        {
            get { return _isMoveDownEnabled; }
            private set
            {
                if (_isMoveDownEnabled != value)
                {
                    _isMoveDownEnabled = value;
                    OnPropertyChanged(() => IsMoveDownEnabled);
                }
            }
        }

        internal void UpdateMoveCommands()
        {
            bool isFirst = true;
            var enumerator = _view.RowViewModels.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (ReferenceEquals(enumerator.Current, _row))
                {
                    IsMoveUpEnabled = !isFirst;
                    IsMoveDownEnabled = enumerator.MoveNext();
                    return;
                }

                isFirst = false;
            }

            // no match, disable both
            IsMoveUpEnabled = IsMoveDownEnabled = false;
        }

        public CommandBase MoveUpCommand { get; private set; }

        private void MoveUp()
        {
            var index = _view.RowViewModels.IndexOf(_row);
            if (index > 0)
            {
                _view.RowViewModels.Move(index, index - 1);

                var list = _view.Rows as IList;
                if (list != null)
                {
                    list[index] = list[index - 1];
                    list[index - 1] = _row;
                }

                _row.Commands.UpdateMoveCommands();
                _view.RowViewModels[index].Commands.UpdateMoveCommands();
            }
        }

        public CommandBase MoveDownCommand { get; private set; }

        private void MoveDown()
        {
            var index = _view.RowViewModels.IndexOf(_row);
            if (index < _view.RowViewModels.Count - 1)
            {
                _view.RowViewModels.Move(index, index + 1);

                var list = _view.Rows as IList;
                if (list != null)
                {
                    list[index] = list[index + 1];
                    list[index + 1] = _row;
                }

                _row.Commands.UpdateMoveCommands();
                _view.RowViewModels[index].Commands.UpdateMoveCommands();
            }
        }

        public CommandBase RemoveCommand { get; private set; }

        private void Remove()
        {
            var index = _view.RowViewModels.IndexOf(_row);
            if (index >= 0)
            {
                _view.RowViewModels.RemoveAt(index);

                var list = _view.Rows as IList;
                if (list != null)
                    list.RemoveAt(index);

                if (index > 0)
                    _view.RowViewModels[index - 1].Commands.UpdateMoveCommands();
                if (index < _view.RowViewModels.Count - 1)
                    _view.RowViewModels[index + 1].Commands.UpdateMoveCommands();
            }
        }
    }
}
