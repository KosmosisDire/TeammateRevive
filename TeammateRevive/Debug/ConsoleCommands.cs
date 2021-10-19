using System.Collections.Generic;
using System.Linq;
using RoR2;
using TeammateRevive.Common;
using TeammateRevive.Configuration;
using TeammateRevive.Logging;
using TeammateRevive.Revive.Rules;
using Console = On.RoR2.Console;

namespace TeammateRevive.Debug
{
    public class ConsoleCommands
    {
        private readonly ReviveRulesCalculator rules;
        private readonly PluginConfig config;

        private readonly Dictionary<string, RoR2.Console.ConCommand> conCommands;

        public ConsoleCommands(ReviveRulesCalculator rules, PluginConfig config)
        {
            this.rules = rules;
            this.config = config;
            this.conCommands = InitCommands();
            On.RoR2.Console.Awake += ConsoleOnAwake;
            
            #if DEBUG
            Chat.onChatChanged += OnChatChanged;
            #endif
        }

        private void OnChatChanged()
        {
            const string msgEnd = "</noparse></color>";
            if (NetworkHelper.IsClient()) return;
            
            var msg = Chat.readOnlyLog.Last();
            foreach (var pair in this.conCommands)
            {
                var cmdIdx = msg.IndexOf(pair.Key);
                if (cmdIdx >= 0)
                {
                    var args = msg.Substring(cmdIdx, msg.Length - msgEnd.Length - cmdIdx).Split().Skip(1).ToList();
                    Log.Debug($"CMD: {string.Join("|", args)}; Msg: {msg}");
                    var conArgs = new ConCommandArgs
                    {
                        commandName = pair.Key,
                        userArgs = args
                    };
                    
                    pair.Value.action(conArgs);
                }
            }
        }

        private Dictionary<string, RoR2.Console.ConCommand> InitCommands()
        {
            return new Dictionary<string, RoR2.Console.ConCommand>
            {
                {
                    "trv_set", new RoR2.Console.ConCommand
                    {
                        action = SetRuleVariable,
                        flags = ConVarFlags.SenderMustBeServer,
                        helpText = "trv_set <rule_var> <value>"
                    }
                },
                {
                    "trv_rules", new RoR2.Console.ConCommand
                    {
                        action = PrintRuleValues,
                        flags = ConVarFlags.SenderMustBeServer,
                        helpText = "trv_rules"
                    }
                },
                {
                    "trv_god", new RoR2.Console.ConCommand
                    {
                        action = ToggleGodMode,
                        flags = ConVarFlags.SenderMustBeServer,
                        helpText = "trv_god"
                    }
                }
            };
        }

        private void ConsoleOnAwake(Console.orig_Awake orig, RoR2.Console self)
        {
            foreach (var keyValuePair in this.conCommands)
            {
                self.concommandCatalog[keyValuePair.Key] = keyValuePair.Value;
            }
            orig(self);
        }

        public void ToggleGodMode(ConCommandArgs args)
        {
            this.config.GodMode = !this.config.GodMode;
            var message = $"GodModeEnabled: {this.config.GodMode}";
            UnityEngine.Debug.Log(message);
            Chat.AddMessage(message);
        }
        
        public void PrintRuleValues(ConCommandArgs args)
        {
            foreach (var property in typeof(ReviveRuleValues).GetProperties())
            {
                var message = $"{property.Name}: {property.GetValue(this.rules.Values):F2}";
                UnityEngine.Debug.Log(message);
                Chat.AddMessage(message);
            }
            
            foreach (var property in typeof(ReviveRulesCalculator).GetProperties().Where(p => p.PropertyType == typeof(float)))
            {
                var message = $"[c] {property.Name}: {property.GetValue(this.rules):F2}";
                UnityEngine.Debug.Log(message);
                Chat.AddMessage(message);
            }
        }

        private void SetRuleVariable(ConCommandArgs args)
        {
            var name = args.GetArgString(0);
            var val = args.GetArgFloat(1);

            var propertyInfo = typeof(ReviveRuleValues).GetProperty(name);
            if (propertyInfo == null)
            {
                UnityEngine.Debug.Log($"Cannot find property with name '{name}'");
                return;
            }

            var values = this.rules.Values;
            propertyInfo.SetValue(values, val);
            this.rules.ApplyValues(values);
            UnityEngine.Debug.Log($"Set '{name}' to {val}");
            this.rules.SendValues();
        }
    }
}