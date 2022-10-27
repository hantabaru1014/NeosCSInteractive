using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace NeosCSInteractive
{
    public class FileUtils
    {
        public static string SmartPadExePath { get => Path.Combine(Environment.CurrentDirectory, "Tools\\NeosCSInteractive.SmartPad\\NeosCSInteractive.SmartPad.exe"); }

        public static string ScriptDirectory { get => Path.Combine(Environment.CurrentDirectory, "ncsi_scripts"); }

        public static bool IsProcessRunning(string path)
        {
            var ps = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(path));
            return ps.Length > 0;
        }
    }
}
