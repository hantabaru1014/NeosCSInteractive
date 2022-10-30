namespace NeosCSInteractive.Shared.JsonProtocols
{
    public class RunContinueFromResultArgs
    {
        public string ConsoleId { get; set; }
        public string Code { get; set; }
        public int BaseResultId { get; set; }

        public RunContinueFromResultArgs(string consoleId, string code, int baseResultId)
        {
            ConsoleId = consoleId;
            Code = code;
            BaseResultId = baseResultId;
        }
    }
}
