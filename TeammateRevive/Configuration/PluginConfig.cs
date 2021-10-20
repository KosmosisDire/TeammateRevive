using BepInEx.Configuration;
using TeammateRevive.Revive.Rules;

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

        public ReviveRuleValues RuleValues { get; set; }


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
                
                ServerLogging = ReadServerLoggingConfig(config),
                
                RuleValues = ReadReviveRuleValues(config)
            };
        }

        private static ServerLoggingConfig ReadServerLoggingConfig(ConfigFile configFile)
        {
            var config = new ServerLoggingConfig();

            configFile.BindCollection("Server logging")
                .Bind(
                    key: "Use external logging server",
                    description: "Log debugging messages to external logging server.",
                    set: v => config.IsEnabled = v,
                    defaultValue: false)
                .Bind(
                    key: "Logging server URL",
                    description: "Logging server URL",
                    set: v => config.Url = v,
                    defaultValue: "")
                .Bind(
                    key: "Logging server user name",
                    description: "User name that will be used for logging server",
                    set: v => config.UserName = v,
                    defaultValue: ""
                )
                .Bind(
                    key: "Logging server room",
                    description: "Room name that will be used for logging server",
                    set: v => config.RoomName = v,
                    defaultValue: ""
                )
                .Bind(
                    key: "Log all",
                    description: "Include all application logs, not only for this plugin",
                    set: v => config.LogAll = v,
                    defaultValue: false
                );
            
            return config;
        }

        static ReviveRuleValues ReadReviveRuleValues(ConfigFile configFile)
        {
            var values = new ReviveRuleValues();

            configFile.BindCollection("Revive rules configuration")
                .Bind(
                    key: "Base revive range",
                    description: "How wide revive circle will be",
                    set: v => values.BaseTotemRange = v,
                    defaultValue: values.BaseTotemRange)
                .Bind(
                    key: "Increase range per player factor",
                    description: "How much to increase revive circle range per player. Set to 0 to disable. " +
                                 "\nRangeIncrease = BaseRange * IncreasePerPlayerFactor",
                    set: v => values.BaseTotemRange = v,
                    defaultValue: values.IncreaseRangeWithPlayersFactor)
                .Bind(
                    key: "Increase range per Obol",
                    description: "[Only with Death Curse enabled] How much to increase revive circle range per dead character's Obol. " +
                                 "\nnRangeIncrease = BaseRange * ItemsCount * IncreasePerPlayerFactor",
                    set: v => values.BaseTotemRange = v,
                    defaultValue: values.IncreaseRangeWithPlayersFactor)
                .Bind(
                    key: "Revive time",
                    description: "[Only with Death Curse enabled] How much time one player will need to revive one dead character.",
                    set: v => values.ReviveTimeSeconds = v,
                    defaultValue: values.ReviveTimeSeconds)
                .Bind(
                    key: "Reduce revive progress factor",
                    description: "How fast revive progress will be decreased when no one in revive range. " +
                                 "\nReduceSpeed = -(1 / ReviveTime * ReduceProgressFactor)",
                    set: v => values.ReduceReviveProgressFactor = v,
                    defaultValue: values.ReduceReviveProgressFactor)
                .Bind(
                    key: "Revive involvement buff duration factor",
                    description: "[Only with Death Curse enabled] How long Revive Involvement buff will stay after player leave revive range. " +
                                 "\nTime = ReviveTime / ReduceProgressFactor * ReviveInvolvementBuffTimeFactor",
                    set: v => values.ReviveInvolvementBuffTimeFactor = v,
                    defaultValue: values.ReviveInvolvementBuffTimeFactor)
                .Bind(
                    key: "Obol revive factor",
                    description: "[Only with Death Curse enabled] How much revive speed will increase per dead character's Obol. " +
                                 "\nSpeedIncrease = ReviveTime / (ReviveTime / ObolReviveFactor ^ ObolsCount)",
                    set: v => values.ObolReviveFactor = v,
                    defaultValue: values.ObolReviveFactor)
                .Bind(
                    key: "Obol damage reduce factor",
                    description: "[Only with Death Curse enabled] How much revive damage in revive circle will decrease per dead character's Obol. " +
                                 "\nDamagePercentage = 1 / (ObolDamageReduceFactor ^ DeadPlayerObolsCount)",
                    set: v => values.ObolDamageReduceFactor = v,
                    defaultValue: values.ObolDamageReduceFactor)
                .Bind(
                    key: "Base Death Curse HP reduce factor",
                    description: "[Only with Death Curse enabled] For how much HP will be reduced for Death Curse." +
                                 "\nReduceHP = MaxHP - (MaxHP / (ReduceHpFactor ^ CursesCount + BaseReduceHpFactor))" +
                                 "\nMaxHP = MaxHP - ReduceHP",
                    set: v => values.BaseReduceHpFactor = v,
                    defaultValue: values.BaseReduceHpFactor)
                .Bind(
                    key: "Death Curse HP reduce factor",
                    description: "[Only with Death Curse enabled] For how much HP will be reduced for Death Curse." +
                                 "\nReduceHP = MaxHP - (MaxHP / (ReduceHpFactor ^ CursesCount + BaseReduceHpFactor))" +
                                 "\nMaxHP = MaxHP - ReduceHP",
                    set: v => values.ReduceHpFactor = v,
                    defaultValue: values.ReduceHpFactor)
                .Bind(
                    key: "Force Death Curse rule",
                    description: "Force enable Death Curse logic (e.g. Artifact of Death Curse) even when artifact is disabled.",
                    set: v => values.ForceDeathCurseRule = v,
                    defaultValue: values.ForceDeathCurseRule)
                ;

            return values;
        }
    }
}