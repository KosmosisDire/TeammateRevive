using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Logging;
using SimpleJSON;
using TeammateRevive.ServerLogging;
using LogLevel = BepInEx.Logging.LogLevel;

namespace TeammateRevival.Logging
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
            if (!this.config.IsEnabled) return;
            if (this.config.LogAll && !fromHook) return;

            this.logsQueue.Enqueue(new JSONObject
            {
                ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                ["level"] = MapLevel(level),
                // TODO: debug, remove
                ["msg"] =  (MainTeammateRevival.IsClient() ? "C " : "S ") + message?.ToString(),
                ["text"] = $"{DateTime.Now:HH:mm:ss:fff} [{level:G}] {message}",
                ["user"] = this.config.UserName,
                ["room"] = this.config.RoomName
            });
            ShipLogs();
        }

        private string MapLevel(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Fatal:
                case LogLevel.Error:
                    return "ERROR";
                
                case LogLevel.Warning:
                    return "WARN";
                
                case LogLevel.Debug:
                    return "DEBUG";

                case LogLevel.All:
                case LogLevel.None:
                case LogLevel.Info:
                case LogLevel.Message:
                default:
                    return "INFO";
            }
        }

        private void ShipLogs()
        {
            if (this.logsQueue.IsEmpty) return;

            // achieving queue using task scheduling. Performing only one sending at a time
            lock (this.locker)
            {
                this.shipTask = this.shipTask.ContinueWith(async _ =>
                {
                    var array = GroupEntries();
                    if (!this.config.IsEnabled) return;
                    await SendEntries(array);
                });
            }
        }
        
        private async Task SendEntries(JSONArray entry)
        {
            var content = new StringContent(entry.ToString(), Encoding.UTF8, "application/json");
            try
            {
                await this.client.PostAsync(this.config.Url.TrimEnd('/') + "/logs/add", content);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private JSONArray GroupEntries()
        {
            var entries = new JSONArray();
            // group all pending entries but not more that 25
            while (this.logsQueue.TryDequeue(out var entry))
            {
                entries.Add(entry);
                if (entries.Count == 25)
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
            if (this.config.LogAll)
            {
                InternalWrite(eventArgs.Level, eventArgs.Data.ToString(), true);
            }
        }
    }
}