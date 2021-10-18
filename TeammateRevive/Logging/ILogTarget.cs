using BepInEx.Logging;

namespace TeammateRevive.Logging
{
    public interface ILogTarget
    {
        void Write(LogLevel level, object msg);
    }
}