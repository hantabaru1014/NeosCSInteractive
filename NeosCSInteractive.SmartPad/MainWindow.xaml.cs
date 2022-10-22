using RoslynPad.Editor;
using RoslynPad.Roslyn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Reflection;
using NeosCSInteractive.Shared;
using WebSocketSharp;
using NeosCSInteractive.Shared.JsonProtocols;

namespace NeosCSInteractive.SmartPad
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private RoslynHostWithGlobals scriptHost;
        private WebSocket? webSocket;

        public MainWindow()
        {
            InitializeComponent();

            scriptHost = new RoslynHostWithGlobals(additionalAssemblies: new[]
            {
                Assembly.Load("RoslynPad.Roslyn.Windows"),
                Assembly.Load("RoslynPad.Editor.Windows"),
            }, RoslynHostReferences.NamespaceDefault.With(assemblyReferences: new[]
            {
                typeof(object).Assembly,
                typeof(Enumerable).Assembly,
            }, typeNamespaceImports: new[]
            {
                typeof(InjectGlobals),
                typeof(FrooxEngine.Engine),
                typeof(BaseX.color),
            }));

            InitWebSocketClient();
        }

        private void ScriptEditor_Loaded(object sender, RoutedEventArgs e)
        {
            scriptEditor.Initialize(scriptHost, new ClassificationHighlightColors(), Directory.GetCurrentDirectory(), string.Empty);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            webSocket?.Connect();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            webSocket?.Close();
        }

        private void InitWebSocketClient()
        {
            webSocket = new WebSocket("ws://localhost:51014/SmartPad");
            webSocket.OnMessage += WebSocket_OnMessage;
        }

        private void WebSocket_OnMessage(object? sender, MessageEventArgs e)
        {
            try
            {
                var command = CommandJson.Deserialize(e.Data);
                switch (command.Command)
                {
                    case CommandJson.CommandType.Output:
                        var args = command.GetArgs<OutputArgs>();
                        if (args.ConsoleId == "SmartPad:Script")
                            AddOutputMessage(args.Message);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                AddOutputMessage(new LogMessage(LogMessage.MessageType.Error, $"[WebSocket Error] {ex.Message}"));
            }
        }

        private void AddOutputMessage(LogMessage message)
        {
            switch (message.Type)
            {
                case LogMessage.MessageType.Info:
                    AppendOutputText($"{message.Time.ToLongTimeString()} [INFO] {message.Message}\n");
                    break;
                case LogMessage.MessageType.Warning:
                    AppendOutputText($"{message.Time.ToLongTimeString()} [WARN] {message.Message}\n");
                    break;
                case LogMessage.MessageType.Error:
                    AppendOutputText($"{message.Time.ToLongTimeString()} [ERROR] {message.Message}\n");
                    break;
                default:
                    break;
            }
        }

        private void AppendOutputText(string text)
        {
            Dispatcher.Invoke(() =>
            {
                outputTextBox.AppendText(text);
            });
        }

        private void ClearOutputMessages()
        {
            Dispatcher.Invoke(() =>
            {
                outputTextBox.Document.Blocks.Clear();
            });
        }

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            if (webSocket is null || !webSocket.IsAlive) return;
            ClearOutputMessages();
            var cmd = new CommandJson(CommandJson.CommandType.RunScript, new RunScriptArgs("SmartPad:Script", scriptEditor.Text));
            webSocket.Send(cmd.Serialize());
        }
    }
}
