using System;
using System.IO;
using BepInEx.Logging;

namespace TeammateRevival.Logging
{
    public class DebugFileTarget : ILogTarget
    {
        static readonly string DefaultPath = Environment.GetEnvironmentVariable("USERPROFILE") + "\\Desktop\\log.txt";

        private readonly string filePath;

        public bool IsEnabled { get; set; } = true;

        public DebugFileTarget(string path, ManualLogSource consoleLogger)
        {
            if (string.IsNullOrEmpty(path)) path = DefaultPath;
            try
            {
                this.filePath = NormalizeAndVerifyPath(path);
            }
            catch
            {
                consoleLogger.LogError(
                    $"Couldn't write to specified file logging path! Will write to \"{DefaultPath}\" instead --------------------------------------------");
                this.filePath = DefaultPath;
            }

            try
            {
                Write(LogLevel.Debug, "Log Setup");
            }
            catch
            {
                consoleLogger.LogWarning("Log file location unavailable!");
                this.IsEnabled = false;
            }
        }

        private string NormalizeAndVerifyPath(string path)
        {
            if (File.Exists(path))
            {
                return path;
            }

            if (Directory.Exists(path) || path.EndsWith("\\"))
            {
                return NormalizeAndVerifyPath(Path.Combine(path, "log.txt"));
            }

            File.WriteAllText(path, "");
            return path;
        }

        public void Write(LogLevel level, object msg)
        {
            if (!IsEnabled) return;
            
            using var writer = new StreamWriter(this.filePath, true);
            writer.WriteLine($"[{level.ToString("G").ToUpper()}] [{DateTime.Now}] {msg} \n");
        }
    }
}