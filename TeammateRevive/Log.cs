using BepInEx.Logging;
using System;
using System.IO;
using System.Text;

namespace TeammateRevive
{
    internal static class Log
    {
        internal static ManualLogSource _logSource;

        internal static void Init(ManualLogSource logSource)
        {
            _logSource = logSource;
        }

        internal static void LogDebug(object data)
        {
            _logSource.LogDebug(data);
            using (StreamWriter writer = new StreamWriter(System.Environment.GetEnvironmentVariable("USERPROFILE") + "\\Desktop\\log.txt", true))
            {
                writer.WriteLine(data);
            }
        }
        internal static void LogError(object data)
        {
            _logSource.LogError(data);
            using (StreamWriter writer = new StreamWriter(System.Environment.GetEnvironmentVariable("USERPROFILE") + "\\Desktop\\log.txt", true))
            {
                writer.WriteLine(data);
            }
        }
        internal static void LogFatal(object data) => _logSource.LogFatal(data);
        internal static void LogInfo(object data)
        {
            _logSource.LogInfo(data);
            FileStream f = File.Open(System.Environment.GetEnvironmentVariable("USERPROFILE") + "\\Desktop\\log.txt", FileMode.Append);
            byte[] d = Encoding.ASCII.GetBytes(data + "\n");
            f.Write(d, 0, d.Length);
            
        }
        internal static void LogMessage(object data) => _logSource.LogMessage(data);
        internal static void LogWarning(object data) => _logSource.LogWarning(data);
    }
}