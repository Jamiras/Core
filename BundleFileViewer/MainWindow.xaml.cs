using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Jamiras.Commands;
using Jamiras.Components;
using Jamiras.Services;

namespace BundleFileViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            CoreServices.RegisterServices();
            ServiceRepository.Instance.FindService<IDialogService>().MainWindow = this;

            DataContext = MainWindowViewModel.Instance;
            MainWindowViewModel.Instance.ExitCommand = new DelegateCommand(new Action(Close));

            var windowSettingsRepository = ServiceRepository.Instance.FindService<IWindowSettingsRepository>();
            windowSettingsRepository.RestoreSettings(this);

            var data = ServiceRepository.Instance.FindService<IPersistantDataRepository>();
            var files = data.GetValue("RecentFiles");
            if (!String.IsNullOrEmpty(files))
            {
                foreach (var fileName in files.Split(';'))
                    MainWindowViewModel.Instance.RecentFiles.Add(fileName);
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            var windowSettingsRepository = ServiceRepository.Instance.FindService<IWindowSettingsRepository>();
            windowSettingsRepository.RememberSettings(this);

            if (MainWindowViewModel.Instance.RecentFiles.Count > 0)
            {
                var data = ServiceRepository.Instance.FindService<IPersistantDataRepository>();
                var files = String.Join(";", MainWindowViewModel.Instance.RecentFiles);
                data.SetValue("RecentFiles", files);
            }

            base.OnClosing(e);
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            MainWindowViewModel.Instance.SelectedFolder = (FolderViewModel)e.NewValue;
        }

        private void TextBox_Loaded(object sender, RoutedEventArgs e)
        {
            var textBox = (TextBox)sender;
            textBox.SelectAll();
            textBox.Focus();

            DependencyObject parent = textBox;
            do
            {
                var tvi = parent as TreeViewItem;
                if (tvi != null)
                {
                    tvi.IsSelected = true;
                    break;
                }

                parent = VisualTreeHelper.GetParent(parent);
            } while (parent != null);
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ((FolderViewModel)((TextBox)sender).DataContext).SetValue(FolderViewModel.IsEditingProperty, false);
        }

        private void ContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            ((Grid)sender).ContextMenu.DataContext = MainWindowViewModel.Instance;
        }
    }
}
