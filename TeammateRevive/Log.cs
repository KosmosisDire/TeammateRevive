using BepInEx.Logging;

namespace TeammateRevive
{
    internal static class Log
    {
        internal static ManualLogSource _logSource;

        internal static void Init(ManualLogSource logSource)
        {
            _logSource = logSource;
        }

        internal static void LogDebug(object data) => _logSource.LogDebug(data);
        internal static void LogError(object data)
        {
            _logSource.LogError(data);
            DebugLogger.LogError(data.ToString());
        }
        internal static void LogFatal(object data) => _logSource.LogFatal(data);
        internal static void LogInfo(object data)
        {
            _logSource.LogInfo(data);
            DebugLogger.LogInfo(data.ToString());
        }
        internal static void LogMessage(object data) => _logSource.LogMessage(data);
        internal static void LogWarning(object data)
        {
            _logSource.LogWarning(data);
            DebugLogger.LogWarning(data.ToString());
        }
    }
}