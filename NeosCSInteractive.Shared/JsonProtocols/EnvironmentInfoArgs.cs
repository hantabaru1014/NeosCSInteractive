namespace NeosCSInteractive.Shared.JsonProtocols
{
    public class EnvironmentInfoArgs
    {
        public string ScriptDirectory { get; set; }
        public string[] Imports { get; set; }
        public string[] ReferenceAssemblies { get; set; }

        public EnvironmentInfoArgs(string scriptDirectory, string[] imports, string[] referenceAssemblies)
        {
            ScriptDirectory = scriptDirectory;
            Imports = imports;
            ReferenceAssemblies = referenceAssemblies;
        }
    }
}
