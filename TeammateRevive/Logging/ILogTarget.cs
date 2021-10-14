using BepInEx.Logging;

namespace TeammateRevival.Logging
{
    public interface ILogTarget
    {
        void Write(LogLevel level, object msg);
    }
}