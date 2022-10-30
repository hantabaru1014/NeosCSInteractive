using System;
using System.Linq;
using WebSocketSharp;
using WebSocketSharp.Server;
using NeosCSInteractive.Shared.JsonProtocols;
using BaseX;

namespace NeosCSInteractive
{
    internal class SmartPadConnector : IDisposable
    {
        class WSBehavior : WebSocketBehavior // セッション毎にひとつインスタンスが作られる
        {
            public event Action<CloseEventArgs> OnDisconnected;

            public WSBehavior(Action<CloseEventArgs> onDisconnected)
            {
                OnDisconnected = onDisconnected;
            }

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
                        case CommandJson.CommandType.RunContinueFromResult:
                            var args2 = command.GetArgs<RunContinueFromResultArgs>();
                            ScriptUtils.RunContinueFromResult(args2.Code, args2.BaseResultId, message =>
                            {
                                var outCmd = new CommandJson(CommandJson.CommandType.Output, new OutputArgs(args2.ConsoleId, message));
                                Send(outCmd.Serialize());
                            }, result =>
                            {
                                var outCmd = new CommandJson(CommandJson.CommandType.ExecutionResult,
                                    new ExecutionResultArgs(args2.ConsoleId, result.ResultId, result.Result, result.IsError));
                                Send(outCmd.Serialize());
                            }, false);
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
                OnDisconnected?.Invoke(e);
            }

            protected override void OnOpen()
            {
                var outCmd = new CommandJson(CommandJson.CommandType.EnvironmentInfo,
                    new EnvironmentInfoArgs(
                        FileUtils.ScriptDirectory,
                        ScriptUtils.Imports,
                        ScriptUtils.ReferenceAssemblies.Select(x => x.Location).ToArray()
                    ));
                Send(outCmd.Serialize());
            }
        }

        private WebSocketServer server;

        public bool IsListening { get => server?.IsListening ?? false; }
        public string Url { get => $"ws://localhost:{server.Port}/SmartPad"; }
        public string UserId { get => "smartpad"; }
        public string Password { get; private set; }
        public bool AutoStop { get; set; }
        public int ClientCount { get => server?.WebSocketServices.SessionCount ?? 0; }

        public SmartPadConnector(int port, string password, bool autoStop)
        {
            server = new WebSocketServer(port == 0 ? NetUtils.GetAvailablePort() : port);
            server.AuthenticationSchemes = WebSocketSharp.Net.AuthenticationSchemes.Digest;
            Password = password;
            server.UserCredentialsFinder = id =>
            {
                return id.Name == UserId ? new WebSocketSharp.Net.NetworkCredential(UserId, Password) : null;
            };
            AutoStop = autoStop;
            server.AddWebSocketService("/SmartPad", () => new WSBehavior(_ => { if (AutoStop && server.WebSocketServices.SessionCount <= 1) server.Stop(); }));
        }

        public string Start()
        {
            server?.Start();
            return Url;
        }

        public void Stop()
        {
            UniLog.Log("[NeosModLoader/NeosCSInteractive] [WebSocket Server] Stopping...");
            CloseClients();
            server?.Stop();
        }

        public void Dispose()
        {
            Stop();
        }

        public void CloseClients()
        {
            var outCmd = new CommandJson(CommandJson.CommandType.CloseClient, null);
            server?.WebSocketServices.Broadcast(outCmd.Serialize());
        }
    }
}
