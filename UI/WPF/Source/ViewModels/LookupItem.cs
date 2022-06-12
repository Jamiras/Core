using Jamiras.Components;
using System.Collections.Generic;
using System.Diagnostics;

namespace Jamiras.ViewModels
{
    /// <summary>
    /// A simple key/value object for building ComboBoxes and TreeViews
    /// </summary>
    /// <seealso cref="Jamiras.Components.PropertyChangedObject" />
    [DebuggerDisplay("{Label} ({Id})")]
    public class LookupItem : PropertyChangedObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LookupItem"/> class.
        /// </summary>
        /// <param name="id">The unique identifier of the item.</param>
        /// <param name="label">The label of the item.</param>
        public LookupItem(int id, string label)
        {
            Id = id;
            _label = label;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        public override string ToString()
        {
            return _label;
        }

        /// <summary>
        /// Gets the unique identifier of the <see cref="LookupItem"/>.
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// Gets or sets the label for the <see cref="LookupItem"/>.
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
        /// Gets or sets whether the <see cref="LookupItem"/> is selected.
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

    /// <summary>
    /// A <see cref="LookupItem"/> that may have children.
    /// </summary>
    public class HierarchicalLookupItem : LookupItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HierarchicalLookupItem"/> class.
        /// </summary>
        /// <param name="id">The unique identifier of the item.</param>
        /// <param name="label">The label of the item.</param>
        public HierarchicalLookupItem(int id, string label)
            : base(id, label)
        {
            _children = _noChildren;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HierarchicalLookupItem"/> class.
        /// </summary>
        /// <param name="id">The unique identifier of the item.</param>
        /// <param name="label">The label of the item.</param>
        /// <param name="children">The children of the item.</param>
        public HierarchicalLookupItem(int id, string label, IEnumerable<HierarchicalLookupItem> children)
            : base(id, label)
        {
            _children = children;
        }

        /// <summary>
        /// Gets or sets the children of the LookupItem
        /// </summary>
        public IEnumerable<HierarchicalLookupItem> Children
        {
            get { return _children; }
            set
            {
                if (_children != value)
                {
                    _children = value;
                    OnPropertyChanged(() => Children);
                }
            }
        }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IEnumerable<HierarchicalLookupItem> _children;

        private static HierarchicalLookupItem[] _noChildren = new HierarchicalLookupItem[0];
    }
}
