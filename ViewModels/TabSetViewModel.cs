using System;
using System.Collections.ObjectModel;
using Jamiras.DataModels;
using Jamiras.Commands;

namespace Jamiras.ViewModels
{
    /// <summary>
    /// ViewModel for a tabset control.
    /// </summary>
    public class TabSetViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TabSetViewModel"/> class.
        /// </summary>
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
        /// Closes the specified tab (removes it from the tabset).
        /// </summary>
        /// <param name="tab">Tab to close.</param>
        /// <returns><c>true</c> if the tab was closed, <c>false</c> if not found.</returns>
        public bool CloseTab(TabViewModel tab)
        {
            if (tab != null)
                return Tabs.Remove(tab);

            return false;
        }

        /// <summary>
        /// Gets the tabs.
        /// </summary>
        public ObservableCollection<TabViewModel> Tabs { get; private set; }

        /// <summary>
        /// <see cref="ModelProperty"/> for <see cref="SelectedTab"/>
        /// </summary>
        public static readonly ModelProperty SelectedTabProperty = ModelProperty.Register(typeof(TabSetViewModel), "SelectedTab", typeof(TabViewModel), null);

        /// <summary>
        /// Gets or sets the selected tab.
        /// </summary>
        public TabViewModel SelectedTab
        {
            get { return (TabViewModel)GetValue(SelectedTabProperty); }
            set { SetValue(SelectedTabProperty, value); }
        }

        /// <summary>
        /// ViewModel for a single tab of the <see cref="TabSetViewModel"/>
        /// </summary>
        public class TabViewModel : ViewModelBase
        {
            internal TabSetViewModel Owner { get; set; }
            internal string Key { get; set; }

            /// <summary>
            /// <see cref="ModelProperty"/> for <see cref="Header"/>
            /// </summary>
            public static readonly ModelProperty HeaderProperty = ModelProperty.Register(typeof(TabViewModel), "Header", typeof(string), String.Empty);

            /// <summary>
            /// Gets or sets the text to display on the tab.
            /// </summary>
            public string Header
            {
                get { return (string)GetValue(HeaderProperty); }
                set { SetValue(HeaderProperty, value); }
            }

            /// <summary>
            /// <see cref="ModelProperty"/> for <see cref="Content"/>
            /// </summary>
            public static readonly ModelProperty ContentProperty = ModelProperty.Register(typeof(TabViewModel), "Content", typeof(ViewModelBase), null);

            /// <summary>
            /// Gets or sets a ViewModel for the tab content.
            /// </summary>
            public ViewModelBase Content
            {
                get { return (ViewModelBase)GetValue(ContentProperty); }
                set { SetValue(ContentProperty, value); }
            }

            /// <summary>
            /// Gets a bindable command that can be used to close the tab.
            /// </summary>
            public CommandBase CloseCommand
            {
                get { return new DelegateCommand(Close); }
            }

            /// <summary>
            /// Closes the tab (removes it from the tabset).
            /// </summary>
            public void Close()
            {
                if (Owner != null)
                    Owner.CloseTab(this);
            }
        }
    }
}
