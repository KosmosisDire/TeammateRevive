using BepInEx.Logging;

namespace TeammateRevive.Logging
{
    public class ConsoleLoggerTarget : ILogTarget
    {
        private readonly ManualLogSource log;

        public ConsoleLoggerTarget(ManualLogSource log)
        {
            this.log = log;
        }

        public void Write(LogLevel level, object msg)
        {
            log.Log(level, msg);
        }
    }
}