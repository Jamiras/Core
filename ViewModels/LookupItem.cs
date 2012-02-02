using System.Diagnostics;

namespace Jamiras.ViewModels
{
    [DebuggerDisplay("{Label} ({Id})")]
    public class LookupItem : ViewModelBase
    {
        public LookupItem(int id, string label)
        {
            Id = id;
            Label = label;
        }

        public override string ToString()
        {
            return Label;
        }

        /// <summary>
        /// Gets the ID for the LookupItem
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// Gets the label for the LookupItem
        /// </summary>
        public string Label { get; private set; }

        /// <summary>
        /// Gets or sets whether the LookupItem is selected
        /// </summary>
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(() => IsSelected);
                }
            }
        }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _isSelected;
    }
}
