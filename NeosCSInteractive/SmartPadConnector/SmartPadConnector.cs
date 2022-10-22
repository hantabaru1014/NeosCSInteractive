using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;
using NeosCSInteractive.Shared.JsonProtocols;

namespace NeosCSInteractive
{
    internal class SmartPadConnector : IDisposable
    {
        class WSBehavior : WebSocketBehavior
        {
            protected override void OnMessage(MessageEventArgs e)
            {
                try
                {
                    var command = CommandJson.Deserialize(e.Data);
                    switch (command.Command)
                    {
                        case CommandJson.CommandType.RunScript:
                            var args = command.GetArgs<RunScriptArgs>();
                            ScriptUtils.RunScript(args.Code, message =>
                            {
                                var outCmd = new CommandJson(CommandJson.CommandType.Output, new OutputArgs(args.ConsoleId, message));
                                Send(outCmd.Serialize());
                            });
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception ex)
                {
                    // TODO: log
                }
            }
        }

        private WebSocketServer server;

        public SmartPadConnector(int port)
        {
            server = new WebSocketServer(port);
            server.AddWebSocketService<WSBehavior>("/SmartPad");
        }

        public string Start()
        {
            server.Start();
            return $"ws://localhost:{server.Port}/SmartPad";
        }

        public void Stop()
        {
            server.Stop();
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
