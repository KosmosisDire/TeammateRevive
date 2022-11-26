using System;
using System.Collections.Generic;
using System.Linq;
using RoR2;
using TeammateRevive.Common;
using TeammateRevive.Configuration;
using TeammateRevive.Logging;
using TeammateRevive.Players;
using TeammateRevive.Revive.Rules;
using Console = On.RoR2.Console;

namespace TeammateRevive.Debugging
{
    public class ConsoleCommands
    {
        private readonly ReviveRules rules;
        private readonly PluginConfig config;

        private readonly Dictionary<string, RoR2.Console.ConCommand> conCommands;

        public ConsoleCommands(ReviveRules rules, PluginConfig config)
        {
            this.rules = rules;
            this.config = config;
            conCommands = InitCommands();
            On.RoR2.Console.Awake += ConsoleOnAwake;
            
            #if DEBUG
            Chat.onChatChanged += OnChatChanged;
            #endif
        }

        private void OnChatChanged()
        {
            const string msgEnd = "</noparse></color>";
            if (NetworkHelper.IsClient()) return;

            try
            {
                var msg = Chat.readOnlyLog.LastOrDefault();
                foreach (var pair in conCommands)
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
            catch (Exception e)
            {
                Log.Debug(e);
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
                },
                {
                    "trv_dmgt", new RoR2.Console.ConCommand()
                    {
                        action = args =>
                        {
                            var idx = args.GetArgInt(0);
                            if (idx >= PlayersTracker.instance.All.Count)
                            {
                                AddLog($"Index {idx} is too large. Player count: {PlayersTracker.instance.All.Count}");
                                return;
                            }
                            DebugHelper.DamageTargetIndex = idx;
                            AddLog($"Target set to {PlayersTracker.instance.All[DebugHelper.DamageTargetIndex].networkUser.userName}");
                        },
                        helpText = "Damage player with entered index"
                    }
                },
                {
                    "trv_tglbuff", new RoR2.Console.ConCommand()
                    {
                        action = args => DebugHelper.ToggleRegenBuff(),
                        helpText = "Toggle post-revive regen buff"
                    }
                },
                {
                    "trv_give_item", new RoR2.Console.ConCommand()
                    {
                        action = args => PlayersTracker.instance.All.ForEach(p => p.GiveItem(ItemCatalog.FindItemIndex(args.GetArgString(0)))),
                        helpText = "Give all players item by name" 
                    }
                },
                {
                    "trv_remove_item", new RoR2.Console.ConCommand()
                    {
                        action = args => PlayersTracker.instance.All.ForEach(p => p.master.master.inventory.RemoveItem(ItemCatalog.FindItemIndex(args.GetArgString(0)))),
                        helpText = "Remove item by name from all players" 
                    }
                }
            };
        }

        private void ConsoleOnAwake(Console.orig_Awake orig, RoR2.Console self)
        {
            foreach (var keyValuePair in conCommands)
            {
                self.concommandCatalog[keyValuePair.Key] = keyValuePair.Value;
            }
            orig(self);
        }

        public void ToggleGodMode(ConCommandArgs args)
        {
            if (NetworkHelper.IsClient()) return;
            
            config.GodMode = !config.GodMode;
            AddLog($"GodModeEnabled: {config.GodMode}");
        }
        
        public void PrintRuleValues(ConCommandArgs args)
        {
            if (NetworkHelper.IsClient()) return;
            var messages = new List<string>();
            
            foreach (var property in typeof(ReviveRuleValues).GetProperties())
            {
                messages.Add($"{property.Name}: {property.GetValue(rules.Values):F2}");
            }
            
            foreach (var property in typeof(ReviveRules).GetProperties().Where(p => p.PropertyType == typeof(float)))
            {
                messages.Add($"[c] {property.Name}: {property.GetValue(rules):F2}");
            }
            
            messages.Add($"[c] Death Curse enabled: {RunTracker.instance.IsDeathCurseEnabled}");
            AddLog(string.Join("; ", messages));
        }

        private void SetRuleVariable(ConCommandArgs args)
        {
            if (NetworkHelper.IsClient()) return;
            
            var name = args.GetArgString(0);
            var propertyInfo = typeof(ReviveRuleValues).GetProperty(name);

            if (propertyInfo == null)
            {
                AddLog($"Cannot find property with name '{name}'");
                return;
            }
            
            object value;
            if (propertyInfo.PropertyType == typeof(float))
            {
                value = args.GetArgFloat(1);
            } else if (propertyInfo.PropertyType == typeof(bool))
            {
                value = args.GetArgBool(1);
            }
            else
            {
                AddLog($"Cannot set property of type '{propertyInfo.PropertyType.Name}'");
                return;
            }

            var values = rules.Values.Clone();
            propertyInfo.SetValue(values, value);
            rules.ApplyValues(values);
            AddLog($"Set '{name}' to {value}");
            rules.SendValues();
        }
        
        void AddLog(string message)
        {
            UnityEngine.Debug.Log(message);
            Chat.AddMessage(message);
        }
    }
}