using System;
using System.Diagnostics;
using System.Windows.Input;
using Jamiras.DataModels;
using Jamiras.ViewModels;

namespace BundleFileViewer
{
    [DebuggerDisplay("{Name}")]
    public class FileViewModel : ViewModelBase
    {
        public static readonly ModelProperty NameProperty = ModelProperty.Register(typeof(FileViewModel), "Name", typeof(string), null);

        public string Name
        {
            get { return (string)GetValue(NameProperty); }
        }

        public static readonly ModelProperty ModifiedProperty = ModelProperty.Register(typeof(FileViewModel), "Modified", typeof(DateTime), DateTime.MinValue);

        public DateTime Modified
        {
            get { return (DateTime)GetValue(ModifiedProperty); }
        }

        public static readonly ModelProperty SizeProperty = ModelProperty.Register(typeof(FileViewModel), "Size", typeof(int), 0);

        public int Size
        {
            get { return (int)GetValue(SizeProperty); }
        }

        public ICommand OpenItemCommand { get; set; }
    }
}
