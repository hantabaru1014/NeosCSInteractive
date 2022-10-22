using System;
using FrooxEngine;
using LogMessage = NeosCSInteractive.Shared.LogMessage;

namespace NeosCSInteractive
{
    public class InjectGlobals : Shared.InjectGlobals
    {
        private Action<LogMessage> logCallback;

        public override World FocusedWorld => Engine.Current.WorldManager.FocusedWorld;

        public InjectGlobals(Action<LogMessage> onLog)
        {
            logCallback = onLog;
        }

        public override void Error(object value)
        {
            logCallback.Invoke(new LogMessage(LogMessage.MessageType.Error, value.ToString()));
        }

        public override void Msg(object value)
        {
            logCallback.Invoke(new LogMessage(LogMessage.MessageType.Info, value.ToString()));
        }

        public override void Warn(object value)
        {
            logCallback.Invoke(new LogMessage(LogMessage.MessageType.Warning, value.ToString()));
        }
    }
}
