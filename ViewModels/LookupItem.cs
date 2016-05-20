using System.Diagnostics;
using Jamiras.Components;

namespace Jamiras.ViewModels
{
    [DebuggerDisplay("{Label} ({Id})")]
    public class LookupItem : PropertyChangedObject
    {
        public LookupItem(int id, string label)
        {
            Id = id;
            _label = label;
        }

        public override string ToString()
        {
            return _label;
        }

        /// <summary>
        /// Gets the ID for the LookupItem
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// Gets or sets the label for the LookupItem
        /// </summary>
        public string Label 
        {
            get { return _label; }
            set
            {
                if (_label != value)
                {
                    _label = value;
                    OnPropertyChanged(() => Label);
                }
            }
        }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _label;

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
