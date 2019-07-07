using System;
using System.Collections.Generic;
using System.IO;
using Jamiras.Components;

namespace Jamiras.IO
{
    /// <summary>
    /// Class for reading an .ini file.
    /// </summary>
    public class IniFile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IniFile"/> class using a file next to the executable.
        /// </summary>
        public IniFile()
            : this(GetDefaultIniPath())
        {
        }

        private static string GetDefaultIniPath()
        {
            string exeName = Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName);
            if (exeName.EndsWith(".vshost"))
                exeName = exeName.Substring(0, exeName.Length - 7);
            string iniFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, exeName + ".ini");
            return iniFile;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IniFile"/> class with the specified file.
        /// </summary>
        public IniFile(string iniPath)
        {
            _iniPath = iniPath;
        }

        private static string _iniPath;

        /// <summary>
        /// Reads this ini file.
        /// </summary>
        /// <returns>Dictionary of key/value pairs for entries read from the .ini file.</returns>
        /// <exception cref="FileNotFoundException">The specified file was not found.</exception>
        public IDictionary<string, string> Read()
        {
            if (!File.Exists(_iniPath))
                throw new FileNotFoundException(_iniPath + " not found");

            var values = new TinyDictionary<string, string>();
            using (var reader = new StreamReader(File.Open(_iniPath, FileMode.Open, FileAccess.Read)))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var lineToken = new Token(line, 0, line.Length).Trim();
                    int index = lineToken.IndexOf('#');
                    if (index >= 0)
                        lineToken = lineToken.SubToken(0, index).TrimRight();

                    index = lineToken.IndexOf('=');
                    if (index > 0)
                    {
                        var key = lineToken.SubToken(0, index).TrimRight();
                        var value = lineToken.SubToken(index + 1).TrimLeft();
                        values[key.ToString()] = value.ToString();
                    }
                }
            }

            return values;
        }

        /// <summary>
        /// Writes this ini file.
        /// </summary>
        /// <parameter name="values">Dictionary of key/value pairs to write to the .ini file.</returns>
        /// <remarks>Completely replaces the .ini file. Recommend calling <see cref="Read"/> and merging changes before calling <see cref="Write"/>. Comments will be lost.</remarks>
        public void Write(IDictionary<string, string> values)
        {
            using (var writer = File.CreateText(_iniPath))
            {
                foreach (var pair in values)
                {
                    writer.Write(pair.Key);
                    writer.Write('=');
                    writer.Write(pair.Value);
                    writer.WriteLine();
                }
            }
        }
    }
}
