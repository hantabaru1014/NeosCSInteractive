using System;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using LogMessage = NeosCSInteractive.Shared.LogMessage;
using System.Linq;
using FrooxEngine;
using Microsoft.CodeAnalysis;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.CSharp.Scripting.Hosting;

namespace NeosCSInteractive
{
    internal static class ScriptUtils
    {
        public class ExecutionResult
        {
            public int ResultId { get; set; }
            public string Result { get; set; }
            public bool IsError { get; set; }

            public ExecutionResult(int resultId, string result, bool isError)
            {
                ResultId = resultId;
                Result = result;
                IsError = isError;
            }
        }

        public class AppendOnlySafeList<T>
        {
            private List<T> list;

            public AppendOnlySafeList()
            {
                list = new List<T>();
            }

            public int Add(T item)
            {
                lock (list)
                {
                    list.Add(item);
                    return list.Count - 1;
                }
            }

            public T this[int i]
            {
                get => list[i];
            }

            public bool IsInRange(int i)
            {
                return i >= 0 && i < list.Count;
            }
        }

        private static AppendOnlySafeList<ScriptState<object>> scriptResults = new();
        private static PrintOptions PrintOptions { get; } = new PrintOptions { MemberDisplayFormat = MemberDisplayFormat.SeparateLines };
        private static MethodInfo HasSubmissionResult { get; } =
                typeof(Compilation).GetMethod(nameof(HasSubmissionResult), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

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

        public static void RunScript(string code, Action<LogMessage> onLog, Action<ExecutionResult> onFinished, bool catchException = true)
        {
            var globals = new InjectGlobals(onLog);
            var options = ScriptOptions.Default
                            .WithImports(Imports)
                            .WithReferences(ReferenceAssemblies)
                            .WithSourceResolver(new SourceFileResolver(new string[0], FileUtils.ScriptDirectory));
            var script = CSharpScript.Create(code, options, typeof(InjectGlobals));
            Engine.Current.WorldManager.FocusedWorld.RootSlot.RunSynchronously(async () =>
            {
                // hierarchyいじろうとして、Modifications from a non-locking thread are disallowed! と言われるのを少し防ぐ
                try
                {
                    var state = await script.RunAsync(globals, ex =>
                    {
                        if (!catchException) return false;
                        onLog.Invoke(new LogMessage(LogMessage.MessageType.Error, ex.Message));
                        return true;
                    });
                    if (onFinished is null) return;
                    var id = scriptResults.Add(state);
                    var isError = state.Exception is not null;
                    var hasResult = (bool)HasSubmissionResult.Invoke(script.GetCompilation(), null);
                    var result = isError ? FormatException(state.Exception) : hasResult ? FormatObject(state.ReturnValue) : string.Empty;
                    onFinished.Invoke(new ExecutionResult(id, result, isError));
                }
                catch (CompilationErrorException ex)
                {
                    if (onFinished is null)
                    {
                        onLog.Invoke(new LogMessage(LogMessage.MessageType.Error, "[CompileError] " + ex.Message));
                    }
                    else
                    {
                        onFinished.Invoke(new ExecutionResult(-2, "[CompileError] " + ex.Message, true));
                    }
                }
                catch (Exception ex)
                {
                    if (onFinished is null)
                    {
                        onLog.Invoke(new LogMessage(LogMessage.MessageType.Error, "[RuntimeException] " + ex.Message));
                    }
                    else
                    {
                        onFinished.Invoke(new ExecutionResult(-2, "[RuntimeException] " + ex.Message, true));
                    }
                }
            });
        }

        public static void RunScript(string code, Action<LogMessage> onLog)
        {
            RunScript(code, onLog, null, true);
        }

        public static void RunContinueFromResult(string code, int baseResultId, Action<LogMessage> onLog, Action<ExecutionResult> onFinished, bool catchException = true)
        {
            if (scriptResults.IsInRange(baseResultId))
            {
                try
                {
                    Engine.Current.WorldManager.FocusedWorld.RootSlot.RunSynchronously(async () =>
                    {
                        var script = scriptResults[baseResultId].Script.ContinueWith(code);
                        var diagnostics = script.Compile().Where(d => d.Severity == DiagnosticSeverity.Error);
                        if (diagnostics.Count() > 0)
                        {
                            if (onFinished is null)
                            {
                                onLog.Invoke(new LogMessage(LogMessage.MessageType.Error, "[CompileError] " + FormatObject(diagnostics.First())));
                            }
                            else
                            {
                                onFinished.Invoke(new ExecutionResult(-2, "[CompileError] " + FormatObject(diagnostics.First()), true));
                            }
                            return;
                        }
                        var state = await script.RunFromAsync(scriptResults[baseResultId], ex =>
                        {
                            if (!catchException) return false;
                            onLog.Invoke(new LogMessage(LogMessage.MessageType.Error, ex.Message));
                            return true;
                        });
                        if (onFinished is null) return;
                        var id = scriptResults.Add(state);
                        var isError = state.Exception is not null;
                        var hasResult = (bool)HasSubmissionResult.Invoke(script.GetCompilation(), null);
                        var result = isError ? FormatException(state.Exception) : hasResult ? FormatObject(state.ReturnValue) : string.Empty;
                        onFinished.Invoke(new ExecutionResult(id, result, isError));
                    });
                }
                catch (CompilationErrorException ex)
                {
                    if (onFinished is null)
                    {
                        onLog.Invoke(new LogMessage(LogMessage.MessageType.Error, "[CompileError] " + ex.Message));
                    }
                    else
                    {
                        onFinished.Invoke(new ExecutionResult(-2, "[CompileError] " + ex.Message, true));
                    }
                }
                catch (Exception ex)
                {
                    if (onFinished is null)
                    {
                        onLog.Invoke(new LogMessage(LogMessage.MessageType.Error, "[RuntimeException] " + ex.Message));
                    }
                    else
                    {
                        onFinished.Invoke(new ExecutionResult(-2, "[RuntimeException] " + ex.Message, true));
                    }
                }
            }
            else
            {
                RunScript(code, onLog, onFinished, false);
            }
        }

        public static string FormatException(Exception ex)
        {
            return CSharpObjectFormatter.Instance.FormatException(ex);
        }

        public static string FormatObject(object o)
        {
            return CSharpObjectFormatter.Instance.FormatObject(o, PrintOptions);
        }
    }
}
