using System;
using System.IO;
using System.Globalization;
using UnityEngine;
namespace TeammateRevival
{

    public class DebugLogger
    {
        public static void Init()
        {
            using (StreamWriter writer = new StreamWriter(System.Environment.GetEnvironmentVariable("USERPROFILE") + "\\Desktop\\log.txt"))
            {
                writer.WriteLine("Setup Teammate Revival - " + DateTime.Now + '\n');
            }
        }

        public static void LogInfo(object message)
        {
            using (StreamWriter writer = new StreamWriter(System.Environment.GetEnvironmentVariable("USERPROFILE") + "\\Desktop\\log.txt", true))
            {
                writer.WriteLine("[Info] - " + message.ToString() + DateTime.Now + '\n');
            }
        }

        //Yellow Errors Prefixed With *WARNING* in Log File 
        public static void LogWarning(object message)
        {
            using (StreamWriter writer = new StreamWriter(System.Environment.GetEnvironmentVariable("USERPROFILE") + "\\Desktop\\log.txt", true))
            {
                writer.WriteLine("[WARNING] - " + message.ToString() + DateTime.Now + '\n');
            }
        }

        //Red Errors Prefixed With *ERROR* in Log File
        public static void LogError(object message)
        {
            using (StreamWriter writer = new StreamWriter(System.Environment.GetEnvironmentVariable("USERPROFILE") + "\\Desktop\\log.txt", true))
            {
                writer.WriteLine("[ERROR] - " + message.ToString() + DateTime.Now + '\n');
            }
        }
    }
}
