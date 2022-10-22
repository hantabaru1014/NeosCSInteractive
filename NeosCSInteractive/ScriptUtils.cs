using System;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using LogMessage = NeosCSInteractive.Shared.LogMessage;
using System.Linq;
using FrooxEngine;

namespace NeosCSInteractive
{
    internal static class ScriptUtils
    {
        public static void RunScript(string code, Action<LogMessage> onLog)
        {
            var globals = new InjectGlobals(onLog);
            // TODO: 整理する
            var refs = AppDomain.CurrentDomain.GetAssemblies().Where(x => !x.IsDynamic && !string.IsNullOrEmpty(x.Location));
            var options = ScriptOptions.Default
                            .WithImports("System", "FrooxEngine", "BaseX")
                            .WithReferences(refs);
            var script = CSharpScript.Create(code, options, typeof(InjectGlobals));
            Engine.Current.WorldManager.FocusedWorld.RootSlot.RunSynchronously(() =>
            {
                // hierarchyいじろうとして、Modifications from a non-locking thread are disallowed! と言われるのを少し防ぐ
                try
                {
                    script.RunAsync(globals, ex =>
                    {
                        onLog.Invoke(new LogMessage(LogMessage.MessageType.Error, ex.Message));
                        return true;
                    });
                }
                catch (CompilationErrorException ex)
                {
                    onLog.Invoke(new LogMessage(LogMessage.MessageType.Error, "[CompileError] " + ex.Message));
                }
                catch (Exception ex)
                {
                    onLog.Invoke(new LogMessage(LogMessage.MessageType.Error, "[RuntimeException] " + ex.Message));
                }
            });
        }
    }
}
