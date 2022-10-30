using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using Microsoft.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using NeosCSInteractive.Shared;
using Microsoft.CodeAnalysis.CSharp.Scripting.Hosting;
using Microsoft.CodeAnalysis.Scripting.Hosting;

namespace NeosCSInteractive.SmartPad
{
    internal class ReplItemViewModel : INotifyPropertyChanged
    {
        private bool _isReadOnly;
        private bool _isWaitingExecutionResult;
        private readonly RoslynHostWithGlobals _host;
        private string? _result;
        private string _outputText = string.Empty;
        private string scriptDirectory;

        private static PrintOptions PrintOptions { get; } =
                new PrintOptions { MemberDisplayFormat = MemberDisplayFormat.SeparateLines };

        public DocumentId? Id { get; private set; }

        public bool IsReadOnly
        {
            get => _isReadOnly;
            private set => SetProperty(ref _isReadOnly, value);
        }

        public bool IsWaitingExecutionResult
        {
            get => _isWaitingExecutionResult;
            private set => SetProperty(ref _isWaitingExecutionResult, value);
        }

        public ReplItemViewModel? Previous { get; }

        public ReplItemViewModel? LastGoodPrevious
        {
            get
            {
                var previous = Previous;
                while (previous != null && previous.HasError)
                {
                    previous = previous.Previous;
                }
                return previous;
            }
        }

        public string Text { get; set; } = string.Empty;

        public string? Result
        {
            get => _result;
            set => SetProperty(ref _result, value);
        }

        public int ResultId { get; private set; } = -1;

        public bool HasError { get; private set; }

        public string OutputText
        {
            get => _outputText;
            set => SetProperty(ref _outputText, value);
        }

        public Script<object>? Script { get; private set; }

        public ReplItemViewModel(RoslynHostWithGlobals host, ReplItemViewModel? previous, string scriptDirectory)
        {
            _host = host;
            Previous = previous;
            this.scriptDirectory = scriptDirectory;
        }

        internal void Initialize(DocumentId id)
        {
            Id = id;
        }

        public bool TrySubmit()
        {
            Result = null;
            /*Script = LastGoodPrevious?.Script?.ContinueWith(Text) ?? 
                CSharpScript.Create(Text, ScriptOptions.Default
                    .WithReferences(_host.DefaultReferences)
                    .WithImports(_host.DefaultImports)
                    .WithSourceResolver(new SourceFileResolver(new string[0], scriptDirectory)), typeof(InjectGlobals));
            var diagnostics = Script.Compile();
            if (diagnostics.Any(t => t.Severity == DiagnosticSeverity.Error))
            {
                Result = string.Join(Environment.NewLine, diagnostics.Select(FormatObject));
                return false;
            }*/

            IsReadOnly = true;
            IsWaitingExecutionResult = true;
            return true;
        }

        public void SetResult(int id, string result, bool isError)
        {
            IsWaitingExecutionResult = false;
            ResultId = id;
            Result = result;
            HasError = isError;
        }

        public void AddOutputMessage(LogMessage message)
        {
            switch (message.Type)
            {
                case LogMessage.MessageType.Info:
                    OutputText += $"{message.Time.ToLongTimeString()} [INFO] {message.Message}\n";
                    break;
                case LogMessage.MessageType.Warning:
                    OutputText += $"{message.Time.ToLongTimeString()} [WARN] {message.Message}\n";
                    break;
                case LogMessage.MessageType.Error:
                    OutputText += $"{message.Time.ToLongTimeString()} [ERROR] {message.Message}\n";
                    break;
                default:
                    break;
            }
        }

        private static string FormatObject(object o)
        {
            return CSharpObjectFormatter.Instance.FormatObject(o, PrintOptions);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                OnPropertyChanged(propertyName);
                return true;
            }
            return false;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
