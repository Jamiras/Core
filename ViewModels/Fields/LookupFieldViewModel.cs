using System.Collections.Generic;
using Jamiras.DataModels;

namespace Jamiras.ViewModels.Fields
{
    public class LookupFieldViewModel : FieldViewModelBase
    {
        public LookupFieldViewModel(string label)
        {
            Label = label;
        }

        public LookupFieldViewModel(string label, IEnumerable<LookupItem> items)
            : this(label)
        {
            Items = items;
        }

        public static readonly ModelProperty ItemsProperty =
            ModelProperty.Register(typeof(LookupFieldViewModel), "Items", typeof(IEnumerable<LookupItem>), null);

        public IEnumerable<LookupItem> Items
        {
            get { return (IEnumerable<LookupItem>)GetValue(ItemsProperty); }
            set { SetValue(ItemsProperty, value); }
        }

        public static readonly ModelProperty SelectedIdProperty =
            ModelProperty.Register(typeof(LookupFieldViewModel), "SelectedId", typeof(int), 0);

        public int SelectedId
        {
            get { return (int)GetValue(SelectedIdProperty); }
            set { SetValue(SelectedIdProperty, value); }
        }
    }
}
