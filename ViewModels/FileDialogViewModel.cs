using System;
using System.Collections.Generic;
using System.Text;
using Jamiras.Components;
using Jamiras.Services;
using Microsoft.Win32;

namespace Jamiras.ViewModels
{
    public class FileDialogViewModel : DialogViewModelBase
    {
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
    }
}
