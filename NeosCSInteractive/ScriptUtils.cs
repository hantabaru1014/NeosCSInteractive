using System;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using LogMessage = NeosCSInteractive.Shared.LogMessage;
using System.Linq;
using FrooxEngine;
using Microsoft.CodeAnalysis;
using System.Reflection;

namespace NeosCSInteractive
{
    internal static class ScriptUtils
    {
        public static string[] Imports { get => new[] { "System", "FrooxEngine", "BaseX" }; }

        private static Assembly[] _referenceAssemblies = null;
        public static Assembly[] ReferenceAssemblies
        {
            get
            {
                if (_referenceAssemblies is null)
                {
                    // TODO: 整理する
                    _referenceAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(x => !x.IsDynamic && !string.IsNullOrEmpty(x.Location)).ToArray();
                }
                return _referenceAssemblies;
            }
        }

        public static void RunScript(string code, Action<LogMessage> onLog)
        {
            var globals = new InjectGlobals(onLog);
            var options = ScriptOptions.Default
                            .WithImports(Imports)
                            .WithReferences(ReferenceAssemblies)
                            .WithSourceResolver(new SourceFileResolver(new string[0], FileUtils.ScriptDirectory));
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
