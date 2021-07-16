using System;
using System.IO;
using System.Globalization;
using UnityEngine;
public class DebugLogger
{
    public static void diagInit()
    {
        
        using (StreamWriter writer = new StreamWriter(System.Environment.GetEnvironmentVariable("USERPROFILE") + "\\Desktop\\TeammateRevivalLog.txt"))
        {
            writer.WriteLine("Setup Teammate Revival" + DateTime.Now + '\n');
        }

    }
    public static void LogInfo(string message)
    {
        using (StreamWriter writer = new StreamWriter(System.Environment.GetEnvironmentVariable("USERPROFILE") + "\\Desktop\\log.txt", true))
        {
            writer.WriteLine(message.ToString() + DateTime.Now + '\n');
        }

    }
    //Yellow Errors Prefixed With *WARNING* in Log File 
    public static void LogWarning(string message)
    {
        using (StreamWriter writer = new StreamWriter(System.Environment.GetEnvironmentVariable("USERPROFILE") + "\\Desktop\\log.txt", true))
        {
            writer.WriteLine("*WARNING*" + message.ToString() + DateTime.Now + '\n');
        }
    }
    //Red Errors Prefixed With *ERROR* in Log File
    public static void LogError(string message)
    {
        using (StreamWriter writer = new StreamWriter(System.Environment.GetEnvironmentVariable("USERPROFILE") + "\\Desktop\\log.txt", true))
        {
            writer.WriteLine("*ERROR*" + message.ToString() + DateTime.Now + '\n');
        }
    }
}
