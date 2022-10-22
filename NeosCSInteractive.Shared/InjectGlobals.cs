using FrooxEngine;

namespace NeosCSInteractive.Shared
{
    public abstract class InjectGlobals
    {
        public abstract World FocusedWorld { get; }

        public abstract void Msg(object value);
        public abstract void Warn(object value);
        public abstract void Error(object value);
    }
}