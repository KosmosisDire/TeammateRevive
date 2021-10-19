using BepInEx.Configuration;

namespace TeammateRevive.Configuration
{
    public class PluginConfig
    {
        public bool ConsoleLogging { get; set; }
        public bool ChatLogging { get; set; }

        public ServerLoggingConfig ServerLogging { get; set; } = new();
        public bool FileLogging { get; set; }
        public string FileLoggingPath { get; set; }
        public bool GodMode { get; set; }

        public float TotemRange { get; set; } = 20;
        public bool IncreaseRangeWithPlayers { get; set; } = true;
        public int ReviveTimeSeconds { get; set; } = 6;


        public static PluginConfig Load(ConfigFile config)
        {
            return new PluginConfig
            {
                ConsoleLogging = config.Bind<bool>(
                    section: "Debugging",
                    key: "Console Logging",
                    description: "Log debugging messages to the console.",
                    defaultValue: true).Value,

                ChatLogging = config.Bind<bool>(
                    section: "Debugging",
                    key: "Chat Logging",
                    description: "Log debugging messages to the in-game chat.",
                    defaultValue: false).Value,

                FileLogging = config.Bind<bool>(
                    section: "Debugging",
                    key: "File Logging",
                    description:
                    "Log debugging messages to log.txt located on the desktop by default (sometimes the path cannot be found, so set a custom path below). If the path cannot be found it will write to \"C:\\log.txt\" instead.",
                    defaultValue: false).Value,

                FileLoggingPath = config.Bind<string>(
                    section: "Debugging",
                    key: "File Logging Path",
                    description:
                    "This sets the location that the logging file will be created. Leave blank to put log.txt on the desktop. If the log file is not showing up set your path manually here.",
                    defaultValue: "").Value,

                GodMode = config.Bind<bool>(
                    section: "Debugging",
                    key: "God Mode",
                    description:
                    "Super massive base damage, and super speed for the host. For testing purposes only, Makes the game incredibly boring.",
                    defaultValue: false).Value,
                
                ServerLogging = ReadServerLoggingConfig(config)
            };
        }
        
        
        /// <summary>
        /// Initialize live-reloading configuration.
        /// </summary>
        private static ServerLoggingConfig ReadServerLoggingConfig(ConfigFile configFile)
        {
            var config = new ServerLoggingConfig();

            configFile.BindCollection("Server logging")
                .BindLive(
                    key: "Use external logging server",
                    description: "Log debugging messages to external logging server.",
                    set: v => config.IsEnabled = v,
                    defaultValue: false)
                .BindLive(
                    key: "Logging server URL",
                    description: "Logging server URL",
                    set: v => config.Url = v,
                    defaultValue: "")
                .BindLive(
                    key: "Logging server user name",
                    description: "User name that will be used for logging server",
                    set: v => config.UserName = v,
                    defaultValue: ""
                )
                .BindLive(
                    key: "Logging server room",
                    description: "Room name that will be used for logging server",
                    set: v => config.RoomName = v,
                    defaultValue: ""
                )
                .BindLive(
                    key: "Log all",
                    description: "Include all application logs, not only for this plugin",
                    set: v => config.LogAll = v,
                    defaultValue: false
                );
            
            return config;
        }
    }
}