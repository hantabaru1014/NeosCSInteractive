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
        public static bool IsProcessRunning(string path)
        {
            var ps = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(path));
            return ps.Length > 0;
        }
    }
}
