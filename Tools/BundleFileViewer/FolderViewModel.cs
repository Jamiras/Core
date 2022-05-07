using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Jamiras.DataModels;
using Jamiras.ViewModels;

namespace BundleFileViewer
{
    [DebuggerDisplay("{Name}")]
    public class FolderViewModel : ViewModelBase
    {
        public FolderViewModel(string name, FolderViewModel parent)
        {
            _parent = parent;
            Children = new ObservableCollection<FolderViewModel>();
            SetValue(NameProperty, name);
        }

        public FolderViewModel Parent
        {
            get { return _parent; }
        }
        private readonly FolderViewModel _parent;

        public static readonly ModelProperty NameProperty = ModelProperty.Register(typeof(FolderViewModel), "Name", typeof(String), null);

        public string Name
        {
            get { return (string)GetValue(NameProperty); }
            set { SetValue(NameProperty, value); }
        }

        public static readonly ModelProperty IsExpandedProperty = ModelProperty.Register(typeof(FolderViewModel), "IsExpanded", typeof(bool), false);

        public bool IsExpanded
        {
            get { return (bool)GetValue(IsExpandedProperty); }
            set { SetValue(IsExpandedProperty, value); }
        }

        public static readonly ModelProperty IsEditingProperty = ModelProperty.Register(typeof(FolderViewModel), "IsEditing", typeof(bool), false);

        public bool IsEditing
        {
            get { return (bool)GetValue(IsEditingProperty); }
        }

        public ObservableCollection<FolderViewModel> Children { get; private set; }
    }
}
