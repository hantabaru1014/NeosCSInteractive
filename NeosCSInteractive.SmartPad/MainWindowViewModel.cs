using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NeosCSInteractive.SmartPad
{
    internal class MainWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _Address = "";
        public string Address
        {
            get => _Address;
            set
            {
                if (_Address != value)
                {
                    _Address = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string _Password = "";
        public string Password
        {
            get => _Password;
            set
            {
                if (_Password != value)
                {
                    _Password = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool _IsConnected = false;
        public bool IsConnected
        {
            get => _IsConnected;
            set
            {
                if (_IsConnected != value)
                {
                    _IsConnected = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int _ScriptEditorFontSize = 16;
        public int ScriptEditorFontSize
        {
            get => _ScriptEditorFontSize;
            set
            {
                if (_ScriptEditorFontSize != value)
                {
                    _ScriptEditorFontSize = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int _ScriptOutputFontSize = 14;
        public int ScriptOutputFontSize
        {
            get => _ScriptOutputFontSize;
            set
            {
                if (_ScriptOutputFontSize != value)
                {
                    _ScriptOutputFontSize = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int _ReplEditorFontSize = 16;
        public int ReplEditorFontSize
        {
            get => _ReplEditorFontSize;
            set
            {
                if (_ReplEditorFontSize != value)
                {
                    _ReplEditorFontSize = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int _ReplOutputFontSize = 14;
        public int ReplOutputFontSize
        {
            get => _ReplOutputFontSize;
            set
            {
                if (_ReplOutputFontSize != value)
                {
                    _ReplOutputFontSize = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}
