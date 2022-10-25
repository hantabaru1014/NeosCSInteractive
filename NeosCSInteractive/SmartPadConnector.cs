using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;
using NeosCSInteractive.Shared.JsonProtocols;
using NeosCSInteractive.Shared;
using BaseX;

namespace NeosCSInteractive
{
    internal class SmartPadConnector : IDisposable
    {
        class WSBehavior : WebSocketBehavior
        {
            public event Action<CloseEventArgs> OnDisconnected;

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
                    UniLog.Error($"[NeosModLoader/NeosCSInteractive] [WebSocket Server Error] {ex.Message}");
                }
            }

            protected override void OnClose(CloseEventArgs e)
            {
                UniLog.Log("[NeosModLoader/NeosCSInteractive] [WebSocket Server] OnClose");
                OnDisconnected?.Invoke(e);
            }

            protected override void OnOpen()
            {
                UniLog.Log("[NeosModLoader/NeosCSInteractive] [WebSocket Server] OnOpen");
            }

            public void CloseClients()
            {
                var outCmd = new CommandJson(CommandJson.CommandType.CloseClient, null);
                Sessions.Broadcast(outCmd.Serialize());
            }
        }

        private WebSocketServer server;
        private WSBehavior wsBehavior;

        public bool IsListening { get => server?.IsListening ?? false; }
        public string Url { get => $"ws://localhost:{server.Port}/SmartPad"; }
        public string UserId { get => "smartpad"; }
        public string Password { get; private set; }

        public SmartPadConnector(int port, string password, bool autoStop)
        {
            server = new WebSocketServer(port == 0 ? NetUtils.GetAvailablePort() : port);
            server.AuthenticationSchemes = WebSocketSharp.Net.AuthenticationSchemes.Digest;
            Password = password;
            server.UserCredentialsFinder = id =>
            {
                return id.Name == UserId ? new WebSocketSharp.Net.NetworkCredential(UserId, Password) : null;
            };
            wsBehavior = new WSBehavior();
            wsBehavior.OnDisconnected += e =>
            {
                if (autoStop) server.Stop();
            };
            server.AddWebSocketService("/SmartPad", () => wsBehavior);
        }

        public string Start()
        {
            server?.Start();
            return Url;
        }

        public void Stop()
        {
            wsBehavior?.CloseClients();
            server?.Stop();
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
