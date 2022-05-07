using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Jamiras.Components;

namespace Jamiras.Services
{
    [Export(typeof(IPersistantDataRepository))]
    internal class PersistantDataRepository : IPersistantDataRepository
    {
        [ImportingConstructor]
        public PersistantDataRepository(IFileSystemService fileSystemService)
        {
            _fileSystemService = fileSystemService;
        }

        private readonly IFileSystemService _fileSystemService;
        private Dictionary<string, string> _entries;
        private bool _updatesPending;
        private int _updateCount;
        private string _appDataDirectory;

        /// <summary>
        /// Gets the path where application-specific user data files should be stored.
        /// </summary>
        public string ApplicationUserDataDirectory
        {
            get { return _appDataDirectory ?? (_appDataDirectory = GetAppDataDirectory()); }
        }

        private static string GetAppDataDirectory()
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            path = Path.Combine(path, "Jamiras");

            string appName = GetAppName();
            return Path.Combine(path, appName);
        }

        internal static string GetFileName()
        {
            return Path.Combine(GetAppDataDirectory(), "userdata.ini");
        }

        private static string GetAppName()
        {
            var assembly = Assembly.GetEntryAssembly();
            if (assembly != null)
            {
                object[] attributes = assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                if (attributes.Length > 0)
                {
                    AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
                    if (!String.IsNullOrEmpty(titleAttribute.Title))
                        return titleAttribute.Title;
                }
            }

            return System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location);
        }

        private void LoadEntries()
        {
            _entries = new Dictionary<string, string>();

            string file = GetFileName();
            if (_fileSystemService.FileExists(file))
            {
                Stream stream = _fileSystemService.OpenFile(file, OpenFileMode.Read);
                if (stream != null)
                {
                    StreamReader reader = new StreamReader(stream);
                    do
                    {
                        string line = reader.ReadLine();
                        if (line == null)
                            break;

                        int split = line.IndexOf('=');
                        if (split > 0)
                        {
                            string key = line.Substring(0, split);
                            string value = line.Substring(split + 1);
                            _entries[Unescape(key)] = Unescape(value);
                        }
                    } while (true);

                    stream.Close();
                }
            }
        }

        private void SaveEntries()
        {
            string file = GetFileName();
            if (!_fileSystemService.FileExists(file))
            {
                string path = Path.GetDirectoryName(file);

                if (!_fileSystemService.DirectoryExists(path))
                    _fileSystemService.CreateDirectory(path);
            }

            Stream stream = _fileSystemService.CreateFile(file);
            StreamWriter writer = new StreamWriter(stream);
            foreach (var kvp in _entries)
            {
                var line = String.Format("{0}={1}", Escape(kvp.Key), Escape(kvp.Value));
                writer.WriteLine(line);
            }

            writer.Flush();
            stream.Close();
        }

        private static string Unescape(string text)
        {
            if (text.IndexOf('\\') == -1)
                return text;

            var builder = new StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (c == '\\' && i + 1 < text.Length)
                {
                    c = text[++i];
                    if (c == 'n')
                        builder.Append('\n');
                    else if (c == 'r')
                        builder.Append('\r');
                    else
                        builder.Append(c);
                }
                else
                {
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }

        private static string Escape(string text)
        {
            return text.Replace("\\", "\\\\").Replace("\r", "\\r").Replace("\n", "\\n");
        }

        #region IPersistantDataRepository Members

        public string GetValue(string key)
        {
            if (_entries == null)
                LoadEntries();

            string value;
            _entries.TryGetValue(key, out value);
            return value;
        }

        public void SetValue(string key, string newValue)
        {
            if (newValue != GetValue(key))
            {
                lock (_entries)
                {
                    if (newValue == null)
                        _entries.Remove(key);
                    else
                        _entries[key] = newValue;

                    if (_updateCount == 0)
                        SaveEntries();
                    else
                        _updatesPending = true;
                }
            }
        }

        public void BeginUpdate()
        {
            if (_entries == null)
                LoadEntries();

            lock (_entries)
            {
                _updateCount++;
            }
        }

        public void EndUpdate()
        {
            if (_updateCount == 0)
                return;

            lock (_entries)
            {
                _updateCount--;

                if (_updateCount == 0 && _updatesPending)
                {
                    SaveEntries();
                    _updatesPending = false;
                }
            }
        }

        #endregion
    }
}
