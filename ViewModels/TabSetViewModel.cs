using System;
using System.Collections.ObjectModel;
using Jamiras.DataModels;
using Jamiras.Commands;

namespace Jamiras.ViewModels
{
    public class TabSetViewModel : ViewModelBase
    {
        public TabSetViewModel()
        {
            Tabs = new ObservableCollection<TabViewModel>();
        }

        /// <summary>
        /// Finds the tab associated with the specified key.
        /// </summary>
        /// <param name="key">Unique identifier of tab.</param>
        /// <returns>Associated tab, or null if not found.</returns>
        public TabViewModel GetTab(string key)
        {
            foreach (var tab in Tabs)
            {
                if (tab.Key == key)
                    return tab;
            }

            return null;
        }

        /// <summary>
        /// Focuses the tab associated with the specified key. If not already existing, uses the <paramref name="createViewModel"/> callback to instantiate the tab.
        /// </summary>
        /// <param name="key">Unique identifier of the tab.</param>
        /// <param name="header">Text to display in the tab header.</param>
        /// <param name="createViewModel">Callback to instantiate the tab if not already created.</param>
        /// <returns>Associated tab.</returns>
        public TabViewModel ShowTab(string key, string header, Func<ViewModelBase> createViewModel)
        {
            var tab = GetTab(key);
            if (tab == null)
            {
                tab = new TabViewModel { Owner = this, Key = key, Header = header, Content = createViewModel() };
                Tabs.Add(tab);
            }

            SelectedTab = tab;
            return tab;
        }

        /// <summary>
        /// Closes the tab associate with the specific key.
        /// </summary>
        /// <param name="key">Unique identifier of the tab.</param>
        /// <returns><c>true</c> if the tab was closed, <c>false</c> if not found.</returns>
        public bool CloseTab(string key)
        {
            return CloseTab(GetTab(key));
        }

        /// <summary>
        /// Closes the specified tab.
        /// </summary>
        /// <param name="tab">Tab to close.</param>
        /// <returns><c>true</c> if the tab was closed, <c>false</c> if not found.</returns>
        public bool CloseTab(TabViewModel tab)
        {
            if (tab != null)
                return Tabs.Remove(tab);

            return false;
        }

        public ObservableCollection<TabViewModel> Tabs { get; private set; }

        public static readonly ModelProperty SelectedTabProperty = ModelProperty.Register(typeof(TabSetViewModel), "SelectedTab", typeof(TabViewModel), null);
        public TabViewModel SelectedTab
        {
            get { return (TabViewModel)GetValue(SelectedTabProperty); }
            set { SetValue(SelectedTabProperty, value); }
        }

        public class TabViewModel : ViewModelBase
        {
            internal TabSetViewModel Owner { get; set; }
            internal string Key { get; set; }

            public static readonly ModelProperty HeaderProperty = ModelProperty.Register(typeof(TabViewModel), "Header", typeof(string), String.Empty);
            public string Header
            {
                get { return (string)GetValue(HeaderProperty); }
                set { SetValue(HeaderProperty, value); }
            }

            public static readonly ModelProperty ContentProperty = ModelProperty.Register(typeof(TabViewModel), "Content", typeof(ViewModelBase), null);
            public ViewModelBase Content
            {
                get { return (ViewModelBase)GetValue(ContentProperty); }
                set { SetValue(ContentProperty, value); }
            }

            public CommandBase CloseCommand
            {
                get { return new DelegateCommand(Close); }
            }

            public void Close()
            {
                if (Owner != null)
                    Owner.CloseTab(this);
            }
        }
    }
}
