using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeosCSInteractive.Shared.JsonProtocols
{
    public class EnvironmentInfoArgs
    {
        public string WorkingDirectory { get; set; }
        public string ScriptDirectory { get; set; }
        public string[] Imports { get; set; }
    }
}
