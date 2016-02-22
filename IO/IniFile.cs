using System;
using System.Collections.Generic;
using System.IO;
using Jamiras.Components;

namespace Jamiras.IO
{
    public class IniFile
    {
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

        public IniFile(string iniPath)
        {
            _iniPath = iniPath;
        }

        private static string _iniPath;

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
    }
}
