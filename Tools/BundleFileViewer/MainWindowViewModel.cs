using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Jamiras.Commands;
using Jamiras.Components;
using Jamiras.DataModels;
using Jamiras.DataModels.Metadata;
using Jamiras.IO;
using Jamiras.Services;
using Jamiras.ViewModels;
using Jamiras.ViewModels.Fields;
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
            MergeDirectoryCommand = new DelegateCommand(MergeDirectory);
            RenameFolderCommand = new DelegateCommand<FolderViewModel>(RenameFolder);
            NewFolderCommand = new DelegateCommand<FolderViewModel>(NewFolder);
            OpenItemCommand = new DelegateCommand<FileViewModel>(OpenItem);

            RecentFiles = new ObservableCollection<string>();
            Folders = new ObservableCollection<FolderViewModel>();
            Items = new ObservableCollection<FileViewModel>();

            Progress = new ProgressFieldViewModel() { IsEnabled = false };

            _backgroundWorkerService = ServiceRepository.Instance.FindService<IBackgroundWorkerService>();
        }

        private class FileBundleEx : FileBundle
        {
            public FileBundleEx(string fileName)
                : base(fileName)
            {
            }

            public FileBundleEx(string fileName, int numBuckets)
                : base(fileName, numBuckets)
            {
            }

            public IEnumerable<FileViewModel> GetFileViewModels(string path)
            {
                foreach (var info in EnumerateFiles())
                {
                    if (!info.IsDirectory && InFolder(info, path))
                    {
                        string file = info.FileName;
                        int index = file.LastIndexOf('\\');
                        if (index > 0)
                            file = file.Substring(index + 1);

                        var vm = new FileViewModel();
                        vm.SetValue(FileViewModel.NameProperty, file);
                        vm.SetValue(FileViewModel.SizeProperty, info.Size);
                        vm.SetValue(FileViewModel.ModifiedProperty, info.Modified);
                        yield return vm;
                    }
                }
            }

            private Stream _fileStream;
            private int _nextWrite;
            private int[] _bucketTail;

            public void BeginWrite()
            {
                _fileStream = File.Open(FileName, FileMode.Open, FileAccess.ReadWrite);
                _nextWrite = (int)_fileStream.Length;
                _bucketTail = new int[1024];
            }

            public void EndWrite()
            {
                _fileStream.Flush();
                _fileStream.Close();
            }

            protected override void Commit(FileInfo info)
            {
                var writer = new BinaryWriter(_fileStream);

                var bucket = GetBucket(info.FileName);
                var offset = _bucketTail[bucket];
                if (offset == 0)
                    writer.BaseStream.Seek(GetBucketOffset(bucket), SeekOrigin.Begin);
                else
                    writer.BaseStream.Seek(offset, SeekOrigin.Begin);

                writer.Write(_nextWrite);
                _bucketTail[bucket] = _nextWrite;

                WriteFile(info, _nextWrite, writer);
                _nextWrite = (int)writer.BaseStream.Position;
            }
        }

        private FileBundleEx _bundle;
        private readonly IBackgroundWorkerService _backgroundWorkerService;

        public static readonly ModelProperty TitleProperty = ModelProperty.Register(typeof(MainWindowViewModel), "Title", typeof(string), "Bundle File Viewer");

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            private set { SetValue(TitleProperty, value); }
        }

        public static MainWindowViewModel Instance 
        {
            get { return _instance ?? (_instance = new MainWindowViewModel()); }
        }
        private static MainWindowViewModel _instance;

        public IEnumerable<GridColumnDefinition> Columns { get; private set; }

        public ObservableCollection<FolderViewModel> Folders { get; private set; }
        public IEnumerable<FileViewModel> Items { get; private set; }

        public ProgressFieldViewModel Progress { get; private set; }

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
                Bundle = new FileBundleEx(dlg.FileName);
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
            Bundle = new FileBundleEx(fileName);
            AddRecentFile(fileName);
        }

        private FileBundleEx Bundle
        {
            get { return _bundle; }
            set
            {
                _bundle = value;
                Title = TitleProperty.DefaultValue + " - " + _bundle.FileName;

                var root = new FolderViewModel(Path.GetFileName(_bundle.FileName), null);
                Folders.Clear();
                Folders.Add(root);

                _backgroundWorkerService.RunAsync(LoadBundle);
            }
        }

        private void LoadBundle()
        {
            var root = Folders[0];
            foreach (var file in _bundle.GetDirectories())
            {
                var path = file.Split('\\');
                AddFolder(root, path, 0);
            }

#if DEBUG
            int[] buckets = new int[1024];
            int count = 0;
            foreach (var file in _bundle.GetFiles())
            {
                buckets[Bundle.GetBucket(file)]++;
                count++;
            }

            int numBuckets = 0;
            for (int i = buckets.Length - 1; i >= 0; i--)
            {
                if (buckets[i] != 0)
                {
                    numBuckets = i + 1;
                    break;
                }
            }

            Array.Sort(buckets, 0, numBuckets);
            int min = buckets[0], max = buckets[numBuckets - 1];
            int median = buckets[numBuckets / 2];

            Debug.WriteLine("{0} files ({1} buckets: {2} min, {3} max, {4} median fill)", count, numBuckets, min, max, median);
#endif
        }

        private void AddFolder(FolderViewModel parent, string[] path, int pathIndex)
        {
            FolderViewModel child = parent.Children.FirstOrDefault(f => String.Compare(f.Name, path[pathIndex], StringComparison.OrdinalIgnoreCase) == 0);
            if (child == null)
            {
                child = new FolderViewModel(path[pathIndex], parent);
                _backgroundWorkerService.InvokeOnUiThread(() => parent.Children.Add(child));
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
            var viewModel = (MainWindowViewModel)sender;
            viewModel._backgroundWorkerService.RunAsync(viewModel.UpdateItems);
        }

        private void UpdateItems()
        {
            var items = new List<FileViewModel>();
            var files = new List<string>(_bundle.GetFiles(GetSelectedFolderPath()));

            Progress.Label = "Reading";
            Progress.Reset(files.Count);
            Progress.IsEnabled = true;

            var s = Stopwatch.StartNew();

            foreach (var vm in _bundle.GetFileViewModels(GetSelectedFolderPath()))
            {
                var delegateVm = vm;
                vm.OpenItemCommand = new DelegateCommand(() => OpenItem(delegateVm));
                items.Add(vm);
                Progress.Current++;
            }

            s.Stop();
            Debug.WriteLine(s.ElapsedMilliseconds + "ms to scan " + items.Count + " files");

            items.Sort((l, r) => SortFunctions.NumericStringCaseInsensitiveCompare(l.Name, r.Name));

            _backgroundWorkerService.InvokeOnUiThread(() =>
            {
                Progress.IsEnabled = false;
                Items = items;
                OnPropertyChanged(() => Items);
            });
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

                    InjectFile(_bundle, file, fileName, info.LastWriteTimeUtc);
                }

                UpdateItems();
            }
        }

        private static void InjectFile(FileBundle bundle, string file, string fileName, DateTime lastWriteTime)
        {
            using (Stream outputStream = bundle.CreateFile(file))
            {
                bundle.SetModified(file, lastWriteTime);

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

        private static void CopyFile(FileBundle destBundle, FileBundle sourceBundle, string file)
        {
            using (Stream outputStream = destBundle.CreateFile(file))
            {
                destBundle.SetModified(file, sourceBundle.GetModified(file));

                using (Stream inputStream = sourceBundle.OpenFile(file, OpenFileMode.Read))
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

        public CommandBase MergeDirectoryCommand { get; private set; }

        private void MergeDirectory()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.AddExtension = true;
            dlg.Title = "Merge Folder";
            if (dlg.ShowDialog() == true)
            {
                var path = GetSelectedFolderPath();
                if (path != null && !_bundle.DirectoryExists(path))
                    _bundle.CreateDirectory(path);

                var bundleFiles = new List<string>(_bundle.GetFiles());

                var newBundle = new FileBundleEx(_bundle.FileName.Replace(".jbd", ".tmp.jbd"), 719); // TODO: numBuckets = prime_ceil(bundleFiles / 16)

                var folder = Path.GetDirectoryName(dlg.FileNames[0]);
                var files = Directory.GetFiles(folder);

                Progress.Reset(files.Length + bundleFiles.Count);
                Progress.Label = "Merging";
                Progress.IsEnabled = true;

                _backgroundWorkerService.RunAsync(() => Merge(newBundle, _bundle, bundleFiles, files));
            }
        }

        private void Merge(FileBundleEx destBundle, FileBundle srcBundle, List<string> bundleFiles, IEnumerable<string> files)
        {
            destBundle.BeginWrite();

            var paths = new List<string>();
            var path = GetSelectedFolderPath();
            AddPath(destBundle, path);
            paths.Add(path);

            foreach (var fileName in files)
            {
                string file = Path.GetFileName(fileName);
                if (path != null)
                    file = String.Format("{0}\\{1}", path, file);

                for (int i = 0; i < bundleFiles.Count; i++)
                {
                    if (String.Compare(bundleFiles[i], file, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        bundleFiles.RemoveAt(i);
                        Progress.Current++;
                        break;
                    }
                }

                FileInfo info = new FileInfo(fileName);
                InjectFile(destBundle, file, fileName, info.LastWriteTimeUtc);

                Progress.Current++;
            }

            foreach (var file in bundleFiles)
            {
                int index = file.LastIndexOf('\\');
                if (index > 0)
                {
                    path = file.Substring(0, index);
                    if (!paths.Contains(path))
                    {
                        AddPath(destBundle, path);
                        paths.Add(path);
                    }
                }

                CopyFile(destBundle, srcBundle, file);
                Progress.Current++;
            }

            destBundle.EndWrite();

            _backgroundWorkerService.InvokeOnUiThread(() =>
            {
                Progress.IsEnabled = false;
            });
        }

        private static void AddPath(FileBundle bundle, string path)
        {
            int index = path.LastIndexOf('\\');
            if (index > 0)
                AddPath(bundle, path.Substring(0, index));

            bundle.CreateDirectory(path);
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
