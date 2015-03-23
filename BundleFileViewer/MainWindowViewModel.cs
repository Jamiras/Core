using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Jamiras.Commands;
using Jamiras.DataModels;
using Jamiras.DataModels.Metadata;
using Jamiras.IO;
using Jamiras.Services;
using Jamiras.ViewModels;
using Jamiras.ViewModels.Grid;
using Microsoft.Win32;

namespace BundleFileViewer
{
    public class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel()
        {
            Columns = new GridColumnDefinition[] {
                new TextColumnDefinition("Name", FileViewModel.NameProperty, new StringFieldMetadata("Name", 80)) { IsReadOnly = true, WidthType = GridColumnWidthType.Fill },
                new DateColumnDefinition("Modified", FileViewModel.ModifiedProperty, new DateTimeFieldMetadata("Modified")) { IsReadOnly = true, Width = 100 },
                new IntegerColumnDefinition("Size", FileViewModel.SizeProperty, new IntegerFieldMetadata("Size", 0, Int32.MaxValue)) { IsReadOnly = true, Width = 100 },
            };

            NewBundleCommand = new DelegateCommand(CreateBundle);
            OpenBundleCommand = new DelegateCommand(OpenBundle);
            OpenRecentBundleCommand = new DelegateCommand<string>(OpenBundle);
            MergeFileCommand = new DelegateCommand(MergeFile);
            RenameFolderCommand = new DelegateCommand<FolderViewModel>(RenameFolder);
            NewFolderCommand = new DelegateCommand<FolderViewModel>(NewFolder);
            OpenItemCommand = new DelegateCommand<FileViewModel>(OpenItem);

            RecentFiles = new ObservableCollection<string>();
            Folders = new ObservableCollection<FolderViewModel>();
            Items = new ObservableCollection<FileViewModel>();
        }

        private FileBundle _bundle;
        private readonly Dispatcher _dispatcher;

        public static readonly ModelProperty TitleProperty = ModelProperty.Register(typeof(MainWindowViewModel), "Title", typeof(string), "Bundle File Viewer");

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            private set { SetValue(TitleProperty, value); }
        }

        public static MainWindowViewModel Instance = new MainWindowViewModel();

        public IEnumerable<GridColumnDefinition> Columns { get; private set; }

        public ObservableCollection<FolderViewModel> Folders { get; private set; }
        public IEnumerable<FileViewModel> Items { get; private set; }

        public ObservableCollection<string> RecentFiles { get; private set; }

        private void AddRecentFile(string fileName)
        {
            RecentFiles.Remove(fileName);
            RecentFiles.Insert(0, fileName);

            if (RecentFiles.Count > 10)
                RecentFiles.RemoveAt(10);
        }

        public CommandBase ExitCommand { get; set; }

        public CommandBase NewBundleCommand { get; private set; }

        private void CreateBundle()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.AddExtension = true;
            dlg.DefaultExt = "jbd";
            dlg.Filter = "Jamiras Bundle (*.jbd)|*.jbd";
            dlg.FilterIndex = 1;
            dlg.Multiselect = false;
            dlg.CheckFileExists = false;
            dlg.Title = "Create File";
            if (dlg.ShowDialog() == true)
            {
                Bundle = new FileBundle(dlg.FileName);
                AddRecentFile(dlg.FileName);
            }
        }

        public CommandBase OpenBundleCommand { get; private set; }

        private void OpenBundle()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.AddExtension = true;
            dlg.DefaultExt = "jbd";
            dlg.Filter = "Jamiras Bundle (*.jbd)|*.jbd";
            dlg.FilterIndex = 1;
            dlg.Multiselect = false;
            dlg.CheckFileExists = true;
            dlg.Title = "Open File";
            if (dlg.ShowDialog() == true)
            {
                OpenBundle(dlg.FileName);
            }
        }

        public CommandBase<string> OpenRecentBundleCommand { get; private set; }

        private void OpenBundle(string fileName)
        {
            Bundle = new FileBundle(fileName);
            AddRecentFile(fileName);
        }

        private FileBundle Bundle
        {
            get { return _bundle; }
            set
            {
                _bundle = value;
                Title = TitleProperty.DefaultValue + " - " + _bundle.FileName;

                var root = new FolderViewModel(Path.GetFileName(_bundle.FileName), null);
                Folders.Clear();
                Folders.Add(root);

                foreach (var file in _bundle.GetDirectories())
                {
                    var path = file.Split('\\');
                    AddFolder(root, path, 0);
                }
            }
        }

        private static void AddFolder(FolderViewModel parent, string[] path, int pathIndex)
        {
            FolderViewModel child = parent.Children.FirstOrDefault(f => String.Compare(f.Name, path[pathIndex], StringComparison.OrdinalIgnoreCase) == 0);
            if (child == null)
            {
                child = new FolderViewModel(path[pathIndex], parent);
                parent.Children.Add(child);
            }

            pathIndex++;
            if (pathIndex < path.Length)
                AddFolder(child, path, pathIndex);
        }

        public static readonly ModelProperty SelectedFolderProperty = 
            ModelProperty.Register(typeof(MainWindowViewModel), "SelectedFolder", typeof(FolderViewModel), null, OnSelectedFolderChanged);

        public FolderViewModel SelectedFolder
        {
            get { return (FolderViewModel)GetValue(SelectedFolderProperty); }
            set { SetValue(SelectedFolderProperty, value); }
        }

        private static void OnSelectedFolderChanged(object sender, ModelPropertyChangedEventArgs e)
        {
            ((MainWindowViewModel)sender).UpdateItems();
        }

        private void UpdateItems()
        {
            var items = new List<FileViewModel>();
            foreach (var path in _bundle.GetFiles(GetSelectedFolderPath()))
            {
                string file = path;
                int index = file.LastIndexOf('\\');
                if (index > 0)
                    file = path.Substring(index + 1);

                var vm = new FileViewModel();
                vm.SetValue(FileViewModel.NameProperty, file);
                vm.SetValue(FileViewModel.SizeProperty, _bundle.GetSize(path));
                vm.SetValue(FileViewModel.ModifiedProperty, _bundle.GetModified(path));

                items.Add(vm);
            }

            items.Sort((l, r) => String.Compare(l.Name, r.Name, StringComparison.OrdinalIgnoreCase));

            Items = items; 
            OnPropertyChanged(() => Items);
        }

        public CommandBase<FolderViewModel> RenameFolderCommand { get; private set; }

        private void RenameFolder(FolderViewModel folder)
        {
            if (!Folders.Contains(folder))
                folder.SetValue(FolderViewModel.IsEditingProperty, true);
        }

        public CommandBase<FolderViewModel> NewFolderCommand { get; private set; }

        private void NewFolder(FolderViewModel folder)
        {
            var child = new FolderViewModel("New Folder", folder);
            child.SetValue(FolderViewModel.IsEditingProperty, true);
            folder.Children.Add(child);
            folder.IsExpanded = true;
        }

        public CommandBase MergeFileCommand { get; private set; }

        private void MergeFile()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.AddExtension = true;
            dlg.Multiselect = true;
            dlg.CheckFileExists = true;
            dlg.Title = "Merge Files";
            if (dlg.ShowDialog() == true)
            {
                var path = GetSelectedFolderPath();
                if (path != null && !_bundle.DirectoryExists(path))
                    _bundle.CreateDirectory(path);

                foreach (var fileName in dlg.FileNames)
                {
                    string file = Path.GetFileName(fileName);
                    if (path != null)
                        file = String.Format("{0}\\{1}", path, file);

                    FileInfo info = new FileInfo(fileName);

                    if (_bundle.FileExists(file))
                    {
                        if (_bundle.GetSize(file) == info.Length && _bundle.GetModified(file).ToUniversalTime() == info.LastWriteTimeUtc)
                            continue;

                        _bundle.DeleteFile(file);
                    }

                    using (Stream outputStream = _bundle.CreateFile(file))
                    {
                        _bundle.SetModified(file, info.LastWriteTimeUtc);

                        using (Stream inputStream = File.OpenRead(fileName))
                        {
                            byte[] buffer = new byte[8192];
                            do
                            {
                                int read = inputStream.Read(buffer, 0, buffer.Length);
                                if (read <= 0)
                                    break;

                                outputStream.Write(buffer, 0, read);
                            } while (true);
                        }
                    }
                }

                UpdateItems();
            }
        }

        private string GetSelectedFolderPath()
        {
            var path = String.Empty;
            var parent = SelectedFolder;
            while (parent.Parent != null)
            {
                if (path.Length > 0)
                    path = String.Format("{0}\\{1}", parent.Name, path);
                else
                    path = parent.Name;

                parent = parent.Parent;
            }

            return path;
        }

        private string GetFullPath(string fileName)
        {
            var path = GetSelectedFolderPath();
            if (path.Length == 0)
                return fileName;

            return String.Format("{0}\\{1}", path, fileName);
        }

        public CommandBase<FileViewModel> OpenItemCommand { get; private set; }

        private void OpenItem(FileViewModel file)
        {
            var extension = Path.GetExtension(file.Name).ToLower();
            switch (extension)
            {
                case ".jpg":
                case ".gif":
                case ".png":
                case ".bmp":
                    ShowImage(GetFullPath(file.Name));
                    break;
            }
        }

        private void ShowImage(string fileName)
        {
            Window window = new Window();
            window.Title = fileName;
            window.SizeToContent = SizeToContent.WidthAndHeight;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.Owner = Application.Current.MainWindow;

            Grid grid = new Grid();
            window.Content = grid;

            Image image = new Image();
            grid.Children.Add(image);

            var imageSource = new BitmapImage();
            using (var stream = _bundle.OpenFile(fileName, OpenFileMode.Read))
            {
                imageSource.BeginInit();
                imageSource.StreamSource = stream;
                imageSource.CacheOption = BitmapCacheOption.OnLoad;
                imageSource.EndInit();
                imageSource.Freeze();

                grid.Width = imageSource.PixelWidth;
                grid.Height = imageSource.PixelHeight;
            }

            image.Source = imageSource;

            window.ShowDialog();
        }
    }
}
