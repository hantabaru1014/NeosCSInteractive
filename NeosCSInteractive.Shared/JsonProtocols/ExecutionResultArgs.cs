namespace NeosCSInteractive.Shared.JsonProtocols
{
    public class ExecutionResultArgs
    {
        public string ConsoleId { get; set; }
        public int ResultId { get; set; }
        public string Result { get; set; }
        public bool IsError { get; set; }

        public ExecutionResultArgs(string consoleId, int resultId, string result, bool isError)
        {
            ConsoleId = consoleId;
            ResultId = resultId;
            Result = result;
            IsError = isError;
        }
    }
}
