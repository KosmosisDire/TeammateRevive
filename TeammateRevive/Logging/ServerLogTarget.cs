using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Logging;
using SimpleJSON;
using TeammateRevive.Common;
using TeammateRevive.Configuration;
using LogLevel = BepInEx.Logging.LogLevel;

namespace TeammateRevive.Logging
{
    public class ServerLogTarget : ILogTarget, ILogListener
    {
        private readonly object locker = new();

        private readonly ServerLoggingConfig config;

        private Task shipTask = Task.CompletedTask;

        private readonly ConcurrentQueue<JSONObject> logsQueue = new();

        private readonly HttpClient client = new();

        public ServerLogTarget(ServerLoggingConfig config)
        {
            this.config = config;
            
            // add hook for all console logging
            BepInEx.Logging.Logger.Listeners.Add(this);
        }

        public void Write(LogLevel level, object message)
        {
            InternalWrite(level, message, false);
        }

        private void InternalWrite(LogLevel level, object message, bool fromHook)
        {
            if (!config.IsEnabled) return;
            if (config.LogAll && !fromHook) return;

            logsQueue.Enqueue(new JSONObject
            {
                ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                ["level"] = MapLevel(level),
                // TODO: debug, remove
                ["msg"] =  (NetworkHelper.IsClient() ? "C " : "S ") + message?.ToString(),
                ["user"] = (NetworkHelper.IsClient() ? "C " : "S ") + config.UserName,
                ["room"] = config.RoomName
            });
            ShipLogs();
        }

        private string MapLevel(LogLevel level)
        {
            return level switch
            {
                LogLevel.Fatal or LogLevel.Error => "ERROR",
                LogLevel.Warning => "WARN",
                LogLevel.Debug => "DEBUG",
                _ => "INFO",
            };
        }

        private void ShipLogs()
        {
            if (logsQueue.IsEmpty) return;

            // achieving queue using task scheduling. Performing only one sending at a time
            lock (locker)
            {
                shipTask = shipTask.ContinueWith(async _ =>
                {
                    var array = GroupEntries();
                    if (!config.IsEnabled) return;
                    await SendEntries(array);
                });
            }
        }
        
        private async Task SendEntries(JSONArray entry)
        {
            var content = new StringContent(entry.ToString(), Encoding.UTF8, "application/json");
            try
            {
                await client.PostAsync(config.Url.TrimEnd('/') + "/logs/add", content);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private JSONArray GroupEntries()
        {
            var entries = new JSONArray();
            
            var maxCount = 50;
            
            // group all pending entries but not more that specific amount
            while (logsQueue.TryDequeue(out var entry))
            {
                entries.Add(entry);
                if (entries.Count == maxCount)
                {
                    break;
                }
            }

            return entries;
        }

        public void Dispose()
        {
        }

        void ILogListener.LogEvent(object sender, LogEventArgs eventArgs)
        {
            if (config.LogAll || eventArgs.Level.HasFlag(LogLevel.Error))
            {
                InternalWrite(eventArgs.Level, eventArgs.Data.ToString(), true);
            }
        }
    }
}