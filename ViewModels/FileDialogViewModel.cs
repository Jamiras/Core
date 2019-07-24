using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Interop;

namespace Jamiras.ViewModels
{
    /// <summary>
    /// ViewModel for selecting files to read or write.
    /// </summary>
    public class FileDialogViewModel : DialogViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileDialogViewModel"/> class.
        /// </summary>
        public FileDialogViewModel()
        {
            Filters = new Dictionary<string, string>();

            // defaults
            AddExtension = true;
            CheckPathExists = true;
        }

        /// <summary>
        /// gets or sets a value indicating whether a file dialog automatically adds an extension to a file name if the user omits an extension.
        /// </summary>
        public bool AddExtension { get; set; }

        /// <summary>
        /// gets or sets a value indicating whether a file dialog displays a warning if the user specifies a file name that does not exist.
        /// </summary>
        public bool CheckFileExists { get; set; }

        /// <summary>
        /// gets or sets a value that specifies whether warnings are displayed if the user types invalid paths and file names.
        /// </summary>
        public bool CheckPathExists { get; set; }

        /// <summary>
        /// gets or sets a value that specifies the default extension string to use to filter the list of files that are displayed.
        /// </summary>
        public string DefaultExt { get; set; }

        /// <summary>
        /// gets or sets the array of selected files.
        /// </summary>
        public string[] FileNames { get; set; }

        /// <summary>
        /// gets an dictionary of description/extension strings that determine what types of files are displayed.
        /// extension strings should be in the format "*.txt". for multiple extensions, separate with semicolons "*.gif;*.jpg"
        /// </summary>
        public Dictionary<string, string> Filters { get; private set; }

        internal string FilterString
        {
            get
            {
                if (Filters.Count == 0)
                    return null;

                var builder = new StringBuilder();
                foreach (KeyValuePair<string, string> kvp in Filters)
                {
                    builder.Append(kvp.Key);
                    builder.Append('|');
                    builder.Append(kvp.Value);
                    builder.Append('|');
                }

                builder.Length--;
                return builder.ToString();
            }
        }

        /// <summary>
        /// gets or sets the initial directory that is displayed by a file dialog.
        /// </summary>
        public string InitialDirectory { get; set; }

        private void ShowFileDialog(FileDialog fileDialog)
        {
            fileDialog.AddExtension = AddExtension;
            fileDialog.CheckFileExists = CheckFileExists;
            fileDialog.CheckPathExists = CheckPathExists;
            fileDialog.DefaultExt = DefaultExt;

            if (FileNames != null && FileNames.Length > 0)
                fileDialog.FileName = FileNames[0];

            fileDialog.Filter = FilterString;
            fileDialog.InitialDirectory = InitialDirectory;
            fileDialog.Title = DialogTitle;

            if (!String.IsNullOrEmpty(DefaultExt))
            {
                string scan = "*." + DefaultExt;
                int i = 1;
                foreach (KeyValuePair<string, string> kvp in Filters)
                {
                    if (kvp.Value == scan || kvp.Value == DefaultExt)
                    {
                        fileDialog.FilterIndex = i;
                        break;
                    }

                    i++;
                }
            }

            bool? result = fileDialog.ShowDialog();

            FileNames = fileDialog.FileNames;
            DialogResult = (result == true) ? DialogResult.Ok : DialogResult.Cancel;
        }

        #region OpenFile mode

        /// <summary>
        /// gets or sets an option indicating whether users are able to select multiple files. (applies only to OpenFile mode)
        /// </summary>
        public bool MultiSelect { get; set; }

        /// <summary>
        /// gets or sets whether the read-only check box should be displayed. (applies only to OpenFile mode)
        /// </summary>
        public bool ShowReadOnly { get; set; }

        /// <summary>
        /// gets or sets whether the read-only check box is checked. (applies only to OpenFile mode)
        /// </summary>
        public bool ReadOnlyChecked { get; set; }

        /// <summary>
        /// shows the OpenFileDialog
        /// </summary>
        public DialogResult ShowOpenFileDialog()
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = MultiSelect;
            openFileDialog.ReadOnlyChecked = ReadOnlyChecked;
            openFileDialog.ShowReadOnly = ShowReadOnly;

            ShowFileDialog(openFileDialog);
            return DialogResult;
        }

        #endregion

        #region SaveFile mode

        /// <summary>
        /// gets or sets whether the user should be prompted to create a file if the user specifies a file that does not exist. (applies only to SaveFile mode)
        /// </summary>
        public bool CreatePrompt { get; set; }

        /// <summary>
        /// gets or sets whether the user should be prompted to overwrite a file if the user specifies a file that already exists. (applies only to SaveFile mode)
        /// </summary>
        public bool OverwritePrompt { get; set; }

        /// <summary>
        /// shows the SaveFileDialog
        /// </summary>
        public DialogResult ShowSaveFileDialog()
        {
            var saveFileDialog = new SaveFileDialog();
            saveFileDialog.CreatePrompt = CreatePrompt;
            saveFileDialog.OverwritePrompt = OverwritePrompt;

            ShowFileDialog(saveFileDialog);
            return DialogResult;
        }

        #endregion

        #region SelectFolder mode

        private DialogResult ShowVistaSelectFolderDialog()
        {
            // OpenFileDialog provides most of the functionality needed to show the Vista folder selection dialog,
            // we just need to set one additional bit. Unfortunately, it's all hidden behind internals, so we have
            // to use reflection. The inspiration for this was found here, but it's doing it with System.Windows.Forms:
            // https://stackoverflow.com/questions/4136477/trying-to-open-a-file-dialog-using-the-new-ifiledialog-and-ifileopendialog-inter
            var openFileDialog = new OpenFileDialog
            {
                AddExtension = false,
                CheckFileExists = false,
                DereferenceLinks = true,
                Filter = "Folders|\n",
                InitialDirectory = InitialDirectory,
                Multiselect = false,
                Title = DialogTitle
            };

            // we are effectively executing the RunVistaDialog method from https://referencesource.microsoft.com/#PresentationFramework/src/Framework/Microsoft/Win32/FileDialog.cs
            // with the inclusion of one additional line to set the FOS_PICKFOLDERS flag.
            //
            //  private bool RunVistaDialog(IntPtr hwndOwner)
            //  {
            //      IFileDialog dialog = CreateVistaDialog();
            //      PrepareVistaDialog(dialog);
            //
            //  +   dialog.SetOptions(dialog.GetOptions() | FOS.FOS_PICKFOLDERS);
            //
            //      using (VistaDialogEvents events = new VistaDialogEvents(dialog, HandleVistaFileOk))
            //      {
            //          return dialog.Show(hwndOwner).Succeeded;
            //      }
            //  }

            //      IFileDialog dialog = CreateVistaDialog();
            const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var win32Assembly = typeof(OpenFileDialog).Assembly;
            var iFileDialogType = win32Assembly.GetType("MS.Internal.AppModel.IFileDialog");
            var createVistaDialogMethodInfo = typeof(OpenFileDialog).GetMethod("CreateVistaDialog", bindingFlags);
            var iFileDialog = createVistaDialogMethodInfo.Invoke(openFileDialog, new object[] { });

            //      PrepareVistaDialog(dialog);
            var prepareVistaDialogMethodInfo = typeof(OpenFileDialog).GetMethod("PrepareVistaDialog", bindingFlags);
            prepareVistaDialogMethodInfo.Invoke(openFileDialog, new[] { iFileDialog });

            //  +   dialog.SetOptions(dialog.GetOptions() | FOS.PICKFOLDERS);
            var getOptionsMethodInfo = iFileDialogType.GetMethod("GetOptions", bindingFlags);
            var options = (uint)getOptionsMethodInfo.Invoke(iFileDialog, null);
            options |= 32; // FOS.PICKFOLDERS

            var setOptionsMethodInfo = iFileDialogType.GetMethod("SetOptions", bindingFlags);
            setOptionsMethodInfo.Invoke(iFileDialog, new object[] { options });

            //      using (VistaDialogEvents events = new VistaDialogEvents(dialog, HandleVistaFileOk))
            var vistaDialogEventsType = win32Assembly.GetType("Microsoft.Win32.FileDialog+VistaDialogEvents");
            var vistaDialogEventsConstructorInfo = vistaDialogEventsType.GetConstructors().First();
            var onOkCallbackDelegateType = vistaDialogEventsConstructorInfo.GetParameters()[1].ParameterType;
            var handleVistaFileOkMethodInfo = typeof(FileDialog).GetMethod("HandleVistaFileOk", bindingFlags);
            var handleVistaFileOkMethod = Delegate.CreateDelegate(onOkCallbackDelegateType, openFileDialog, handleVistaFileOkMethodInfo);
            var vistaDialogEvents = vistaDialogEventsConstructorInfo.Invoke(new object[] { iFileDialog, handleVistaFileOkMethod });

            //          return dialog.Show(hwndOwner).Succeeded;
            var showMethodInfo = iFileDialogType.GetMethod("Show");
            bool succeeded = false;
            try
            {
                var ownerWindow = Application.Current.Windows.OfType<Window>().SingleOrDefault(w => w.IsActive);
                var hWndOwner = (ownerWindow != null) ? new WindowInteropHelper(ownerWindow).Handle : IntPtr.Zero;

                var showResult = showMethodInfo.Invoke(iFileDialog, new object[] { hWndOwner });
                succeeded = (bool)showResult.GetType().GetProperty("Succeeded").GetValue(showResult, null);
            }
            finally
            {
                // end of using block
                ((IDisposable)vistaDialogEvents).Dispose();
            }

            // end of reflection magic

            FileNames = new string[] { openFileDialog.FileName };
            DialogResult = succeeded ? DialogResult.Ok : DialogResult.Cancel;
            return DialogResult;
        }

        /// <summary>
        /// shows the SelectFolderDialog
        /// </summary>
        public DialogResult ShowSelectFolderDialog()
        {
            if (Environment.OSVersion.Version.Major >= 6)
            {
                try
                {
                    return ShowVistaSelectFolderDialog();
                }
                catch (Exception)
                {
                    // something in the reflection failed, fallback to pre-Vista logic
                }
            }

            // not Vista or newer, or reflection failed, show Open File dialog and just
            // take the path from the selected file.
            var result = ShowOpenFileDialog();
            if (result == DialogResult.Ok)
                FileNames[0] = Path.GetDirectoryName(FileNames[0]);

            return result;
        }

        #endregion
    }
}
