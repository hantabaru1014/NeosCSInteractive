namespace NeosCSInteractive.Shared.JsonProtocols
{
    public class RunScriptArgs
    {
        public string ConsoleId { get; set; }
        public string Code { get; set; }

        public RunScriptArgs(string consoleId, string code)
        {
            ConsoleId = consoleId;
            Code = code;
        }
    }
}
