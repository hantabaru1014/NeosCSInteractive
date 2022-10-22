namespace NeosCSInteractive.Shared.JsonProtocols
{
    public class OutputArgs
    {
        public string ConsoleId { get; set; }
        public LogMessage Message { get; set; }

        public OutputArgs(string consoleId, LogMessage message)
        {
            ConsoleId = consoleId;
            Message = message;
        }
    }
}
