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
using Microsoft.WindowsAPICodePack.Dialogs;

namespace NeosCSInteractive.SmartPad
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewModel viewModel = new MainWindowViewModel();
        private WebSocket? webSocket;
        private string scriptDirectoryPath = "";

        public Dictionary<string, string> ParsedCmdLineArgs { get; private set; } = new Dictionary<string, string>();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void InitializeScriptEditor(string scriptDirectory, string[] imports, string[] referenceAssemblies)
        {
            scriptDirectoryPath = scriptDirectory;
            var scriptHost = new RoslynHostWithGlobals(additionalAssemblies: new[]
            {
                Assembly.Load("RoslynPad.Roslyn.Windows"),
                Assembly.Load("RoslynPad.Editor.Windows"),
            }, RoslynHostReferences.NamespaceDefault.With(
                imports: imports,
                assemblyPathReferences: referenceAssemblies
            ));
            scriptEditor.Initialize(scriptHost, new ClassificationHighlightColors(), scriptDirectory, scriptEditor.Text);
            AddOutputMessage(new LogMessage(LogMessage.MessageType.Info, "Ready"));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ParseCommandLineArgs();
            CmdLineArgsToViewModel();

            if (!viewModel.Address.IsNullOrEmpty() && !viewModel.Password.IsNullOrEmpty())
            {
                InitWebSocketClient();
                webSocket?.ConnectAsync();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            webSocket?.Close();
        }

        private void ParseCommandLineArgs()
        {
            var parsedArgs = new Dictionary<string, string>();
            var args = Environment.GetCommandLineArgs();
            for (var i=1; i < args.Length; i++)
            {
                if (args.Length == i+1 || args[i + 1].StartsWith("-"))
                {
                    parsedArgs.Add(args[i].ToLower(), string.Empty);
                }
                else
                {
                    parsedArgs.Add(args[i].ToLower(), args[i + 1]);
                    i++;
                }
            }
            ParsedCmdLineArgs = parsedArgs;
        }

        private void CmdLineArgsToViewModel()
        {
            if (ParsedCmdLineArgs.TryGetValue("-port", out var port))
            {
                viewModel.Address = $"ws://localhost:{port}/SmartPad";
            }
            if (ParsedCmdLineArgs.TryGetValue("-address", out var address))
            {
                viewModel.Address = address;
            }
            if (ParsedCmdLineArgs.TryGetValue("-password", out var pass))
            {
                viewModel.Password = pass;
            }
        }

        private void InitWebSocketClient()
        {
            webSocket = new WebSocket(viewModel.Address);
            webSocket.SetCredentials("smartpad", viewModel.Password, false);
            webSocket.OnMessage += WebSocket_OnMessage;
            webSocket.OnOpen += WebSocket_OnOpen;
            webSocket.OnClose += WebSocket_OnClose;
            webSocket.OnError += WebSocket_OnError;
        }

        private void WebSocket_OnError(object? sender, WebSocketSharp.ErrorEventArgs e)
        {
            AddOutputMessage(new LogMessage(LogMessage.MessageType.Error, $"[WebSocket Error] {e.Message}"));
        }

        private void WebSocket_OnClose(object? sender, CloseEventArgs e)
        {
            viewModel.IsConnected = false;
            AddOutputMessage(new LogMessage(LogMessage.MessageType.Info, "[WebSocket] Disconnected"));
        }

        private void WebSocket_OnOpen(object? sender, EventArgs e)
        {
            viewModel.IsConnected = true;
            AddOutputMessage(new LogMessage(LogMessage.MessageType.Info, "[WebSocket] Connected"));
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
                    case CommandJson.CommandType.CloseClient:
                        CloseWindow();
                        break;
                    case CommandJson.CommandType.EnvironmentInfo:
                        Dispatcher.Invoke(() =>
                        {
                            var args2 = command.GetArgs<EnvironmentInfoArgs>();
                            InitializeScriptEditor(args2.ScriptDirectory, args2.Imports, args2.ReferenceAssemblies);
                        });
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
                    AppendOutputText($"{message.Time.ToLongTimeString()} [INFO] {message.Message}");
                    break;
                case LogMessage.MessageType.Warning:
                    AppendOutputText($"{message.Time.ToLongTimeString()} [WARN] {message.Message}");
                    break;
                case LogMessage.MessageType.Error:
                    AppendOutputText($"{message.Time.ToLongTimeString()} [ERROR] {message.Message}");
                    break;
                default:
                    break;
            }
        }

        private void AppendOutputText(string text)
        {
            Dispatcher.Invoke(() =>
            {
                outputTextBox.AppendText(text+"\r");
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

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            InitWebSocketClient();
            webSocket?.ConnectAsync();
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            webSocket?.CloseAsync();
        }

        private void CloseWindow()
        {
            Dispatcher.Invoke(() =>
            {
                Close();
            });
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog("Load Script File");
            dialog.DefaultDirectory = scriptDirectoryPath;
            dialog.Filters.Add(new CommonFileDialogFilter("C# Script File", "*.csx"));
            dialog.Filters.Add(new CommonFileDialogFilter("C# Source Code", "*.cs"));
            dialog.Filters.Add(new CommonFileDialogFilter("All Files", "*.*"));

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                scriptEditor.Text = File.ReadAllText(dialog.FileName, Encoding.UTF8);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonSaveFileDialog("Save Script File");
            dialog.DefaultDirectory = scriptDirectoryPath;
            dialog.DefaultExtension = ".csx";
            dialog.Filters.Add(new CommonFileDialogFilter("C# Script File", "*.csx"));

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                File.WriteAllText(dialog.FileName, scriptEditor.Text, Encoding.UTF8);
            }
        }
    }
}
