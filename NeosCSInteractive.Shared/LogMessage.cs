using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System;

namespace NeosCSInteractive.Shared
{
    public class LogMessage
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public enum MessageType
        {
            Info,
            Warning,
            Error,
        }

        public DateTime Time { get; set; }
        public MessageType Type { get; set; }
        public string Message { get; set; }

        [JsonConstructor]
        public LogMessage(DateTime time, MessageType type, string message)
        {
            Time = time;
            Type = type;
            Message = message;
        }

        public LogMessage(MessageType type, string message) : this(DateTime.Now, type, message) { }
    }
}
