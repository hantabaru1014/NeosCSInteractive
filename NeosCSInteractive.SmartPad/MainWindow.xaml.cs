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
using System.Collections.ObjectModel;

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
        private readonly ObservableCollection<ReplItemViewModel> replItemModels;
        private RoslynHostWithGlobals? replHost;
        private int replHistoryCursor = 0;

        public Dictionary<string, string> ParsedCmdLineArgs { get; private set; } = new Dictionary<string, string>();

        public MainWindow()
        {
            InitializeComponent();

            replItemModels = new ObservableCollection<ReplItemViewModel>();
            DataContext = viewModel;
            ReplItems.ItemsSource = replItemModels;
        }

        private void InitializeRoslynHost(string scriptDirectory, string[] imports, string[] referenceAssemblies)
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

            replHost = new RoslynHostWithGlobals(additionalAssemblies: new[]
            {
                Assembly.Load("RoslynPad.Roslyn.Windows"),
                Assembly.Load("RoslynPad.Editor.Windows"),
            }, RoslynHostReferences.NamespaceDefault.With(
                imports: imports,
                assemblyPathReferences: referenceAssemblies
            ));
            AddNewReplItem();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ((MainWindow)sender).Loaded -= Window_Loaded;
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
            Dispatcher.Invoke(() =>
            {
                ClearReplItems();
            });
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
                        if (args.ConsoleId == "SmartPad:REPL")
                            Dispatcher.Invoke(() => replItemModels.LastOrDefault()?.AddOutputMessage(args.Message));
                        break;
                    case CommandJson.CommandType.CloseClient:
                        CloseWindow();
                        break;
                    case CommandJson.CommandType.EnvironmentInfo:
                        Dispatcher.Invoke(() =>
                        {
                            var args2 = command.GetArgs<EnvironmentInfoArgs>();
                            InitializeRoslynHost(args2.ScriptDirectory, args2.Imports, args2.ReferenceAssemblies);
                        });
                        break;
                    case CommandJson.CommandType.ExecutionResult:
                        var args3 = command.GetArgs<ExecutionResultArgs>();
                        if (args3.ConsoleId != "SmartPad:REPL") break;
                        Dispatcher.Invoke(() =>
                        {
                            replItemModels.LastOrDefault()?.SetResult(args3.ResultId, args3.Result, args3.IsError);
                            AddNewReplItem(replItemModels.LastOrDefault());
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
            dialog.InitialDirectory = scriptDirectoryPath;
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
            dialog.InitialDirectory = scriptDirectoryPath;
            dialog.DefaultExtension = ".csx";
            dialog.Filters.Add(new CommonFileDialogFilter("C# Script File", "*.csx"));

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                File.WriteAllText(dialog.FileName, scriptEditor.Text, Encoding.UTF8);
            }
        }

        private void ReplItem_Editor_Loaded(object sender, RoutedEventArgs e)
        {
            var editor = (RoslynCodeEditor)sender;
            editor.Loaded -= ReplItem_Editor_Loaded;
            editor.Focus();

            var viewModel = (ReplItemViewModel)editor.DataContext;
            var previous = viewModel.LastGoodPrevious;
            if (previous?.Id is not null)
            {
                editor.CreatingDocument += (o, args) =>
                {
                    args.DocumentId = replHost?.AddRelatedDocument(previous.Id, new DocumentCreationArgs(
                        args.TextContainer, scriptDirectoryPath, args.ProcessDiagnostics, args.TextContainer.UpdateText));
                };
            }
            var documentId = editor.Initialize(replHost!, new ClassificationHighlightColors(), scriptDirectoryPath, string.Empty);
            viewModel.Initialize(documentId);
        }

        private void ReplItem_Editor_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var editor = (RoslynCodeEditor)sender;
            if (editor.IsCompletionWindowOpen) return;
            var viewModel = (ReplItemViewModel)editor.DataContext;

            if (e.Key == Key.Enter && e.KeyboardDevice.Modifiers != ModifierKeys.Shift)
            {
                if (webSocket is null || !webSocket.IsAlive) return;
                e.Handled = true;

                if (viewModel.IsReadOnly) return;
                viewModel.Text = editor.Text;
                if (viewModel.TrySubmit())
                {
                    var cmd = new CommandJson(CommandJson.CommandType.RunContinueFromResult,
                        new RunContinueFromResultArgs("SmartPad:REPL", viewModel.Text, viewModel.LastGoodPrevious?.ResultId ?? -1));
                    webSocket.Send(cmd.Serialize());
                }
            }
            else if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                if (e.Key == Key.Up)
                {
                    if (replHistoryCursor + 2 > replItemModels.Count) return;
                    if (replHistoryCursor == 0) viewModel.Text = editor.Text;
                    replHistoryCursor++;
                    editor.Text = replItemModels[replItemModels.Count - replHistoryCursor - 1].Text;
                }
                else if (e.Key == Key.Down)
                {
                    if (replHistoryCursor == 0) return;
                    replHistoryCursor--;
                    editor.Text = replItemModels[replItemModels.Count - replHistoryCursor - 1].Text;
                }
            }
        }

        private void AddNewReplItem(ReplItemViewModel? previous = null)
        {
            if (replHost is null) return;
            replHistoryCursor = 0;
            replItemModels.Add(new ReplItemViewModel(previous));
        }

        private void ClearReplItems()
        {
            replItemModels.Clear();
        }
    }
}
