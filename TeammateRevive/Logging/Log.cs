using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using BepInEx.Logging;

namespace TeammateRevival.Logging
{
    public class Log
    {
        public static Log Instance;

        public static void Init(PluginConfig config, ManualLogSource log)
        {
            Instance = new Log
            {
                Targets = new[]
                    {
                        CreateIf(config.ConsoleLogging, log,() => new ConsoleLoggerTarget(log)),
                        CreateIf(config.FileLogging, log,() => new DebugFileTarget(config.FileLoggingPath, log)),
                        CreateIf(config.ChatLogging, log,() => new ChatLogTarget()),
                        new ServerLogTarget(config.ServerLogging)
                    }
                    .Where(t => t != null)
                    .ToList()
            };
        }

        private static ILogTarget CreateIf(bool condition, ManualLogSource consoleLog, Func<ILogTarget> create)
        {
            if (!condition) return null;
            try
            {
                return create();
            }
            catch (Exception ex)
            {
                consoleLog.LogWarning("Error on logging target creation: " + ex);
                return null;
            }
        }

        public List<ILogTarget> Targets = new();
        
        protected Log()
        {
        }

        public static void Info(object msg) => Instance?.Write(LogLevel.Info, msg);
        public static void Warn(object msg) => Instance?.Write(LogLevel.Warning, msg);
        public static void WarnMethod(object msg, [CallerMemberName] string callingMember = null, [CallerLineNumber] int lineNo = 0)
        {
            Instance?.Write(LogLevel.Warning, $"[{NameOfCallingClass()}.{callingMember}:{lineNo}] {msg}");
        }

        public static void Error(object msg) => Instance?.Write(LogLevel.Error, msg);
        
        [Conditional("DEBUG")]
        public static void Debug(object msg) => Instance?.Write(LogLevel.Debug, msg);
        
        [Conditional("DEBUG")]
        public static void DebugMethod(object message = null, [CallerMemberName] string callingMember = null, [CallerLineNumber] int lineNo = 0)
        {
            Instance?.Write(LogLevel.Debug, $"[{NameOfCallingClass()}.{callingMember}:{lineNo}] {message ?? "called"}");
        }
        
        public static string NameOfCallingClass()
        {
            string fullName;
            Type declaringType;
            int skipFrames = 2;
            do
            {
                var method = new StackFrame(skipFrames, false).GetMethod();
                declaringType = method?.DeclaringType;
                if (declaringType == null)
                {
                    return method?.Name ?? "__";
                }
                skipFrames++;
                fullName = declaringType.FullName;
            }
            while (declaringType.Module.Name.Equals("mscorlib.dll", StringComparison.OrdinalIgnoreCase));

            return fullName;
        }

        public void Write(LogLevel level, object msg)
        {
            foreach (var target in this.Targets)
            {
                try
                {
                    target.Write(level, msg);
                }
                catch
                {
                    // ignore
                }
            }
        }
    }
}