using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.CodeAnalysis;
using System.Runtime.CompilerServices;
using NeosCSInteractive.Shared;

namespace NeosCSInteractive.SmartPad
{
    internal class ReplItemViewModel : INotifyPropertyChanged
    {
        private bool _isReadOnly;
        private bool _isWaitingExecutionResult;
        private string? _result;
        private string _outputText = string.Empty;

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

        public ReplItemViewModel(ReplItemViewModel? previous)
        {
            Previous = previous;
        }

        internal void Initialize(DocumentId id)
        {
            Id = id;
        }

        public bool TrySubmit()
        {
            Result = null;
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
