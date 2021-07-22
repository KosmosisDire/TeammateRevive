using System;
using System.IO;
namespace TeammateRevival
{
    public class DebugLogger
    {
        public static string path = System.Environment.GetEnvironmentVariable("USERPROFILE") + "\\Desktop\\log.txt";
        public static void Init()
        {
            if (MainTeammateRevival.fileLoggingPath.Value != "")
            {
                path = MainTeammateRevival.fileLoggingPath.Value;
            }

            FileAttributes checkPath = File.GetAttributes(path);
            if ((checkPath & FileAttributes.Directory) == FileAttributes.Directory)
            {
                if (path.EndsWith("/") || path.EndsWith("\\"))
                {
                    path = path + "log.txt";
                }
                else
                {
                    path = path + "\\log.txt";
                }
            }

            try
            {
                using (StreamWriter writer = new StreamWriter(path))
                {
                    writer.WriteLine("Setup Log - " + DateTime.Now + '\n');
                }
            }
            catch
            {
                Log.LogError("Couldn't write to specified file logging path! Will write to \"C:\\log.txt\" instead --------------------------------------------");

                path = "C:\\log.txt";
                using (StreamWriter writer = new StreamWriter(path))
                {
                    writer.WriteLine("Setup Log - " + DateTime.Now + '\n');
                }
            }
        }

        public static void LogInfo(object message)
        {
            using (StreamWriter writer = new StreamWriter(path, true))
            {
                writer.WriteLine("[Info] -  " + message.ToString() + "  - " + DateTime.Now + '\n');
            }
        }

        //Yellow Errors Prefixed With *WARNING* in Log File 
        public static void LogWarning(object message)
        {
            using (StreamWriter writer = new StreamWriter(path, true))
            {
                writer.WriteLine("[WARNING] -  " + message.ToString() + "  - " + DateTime.Now + '\n');
            }
        }

        //Red Errors Prefixed With *ERROR* in Log File
        public static void LogError(object message)
        {
            using (StreamWriter writer = new StreamWriter(path, true))
            {
                writer.WriteLine("[ERROR] -  " + message.ToString() + "  - " + DateTime.Now + '\n');
            }
        }
    }
}
