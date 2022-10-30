using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;

namespace NeosCSInteractive.Shared.JsonProtocols
{
    public class CommandJson
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public enum CommandType
        {
            Ping,

            // Client -> Server
            RunScript,
            RunContinueFromResult,

            // Server -> Client
            Output,
            ExecutionResult,
            EnvironmentInfo,
            CloseClient,
        }
        
        public CommandType Command { get; set; }
        public object Args { get; set; }

        public CommandJson(CommandType command, object args)
        {
            Command = command;
            Args = args;
        }

        public string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static CommandJson Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<CommandJson>(json);
        }

        public T GetArgs<T>()
        {
            if (Args.GetType() == typeof(JObject))
                return (Args as JObject).ToObject<T>();
            else
                return (T)Args;
        }
    }
}
