﻿using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ItemStats;
using ItemStats.Stat;
using ItemStats.ValueFormatters;
using RoR2;
using TeammateRevive.Content;
using TeammateRevive.Logging;
using TeammateRevive.Revive.Rules;
using static TeammateRevive.Localization.LanguageConsts;

namespace TeammateRevive.Integrations
{
    /// <summary>
    /// Adds description for added items.
    /// </summary>
    public class ItemsStatsModIntegration
    {
        private readonly ReviveRules rules;

        public ItemsStatsModIntegration(ReviveRules rules)
        {
            this.rules = rules;
            RoR2Application.onLoad += () =>
            {
                if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("dev.ontrigger.itemstats"))
                {
                    AddToItemStats();
                }
            };
        }

        string Format(string key, string value) => string.Format(Language.GetString(key), value);

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        void AddToItemStats()
        {
            if (CharonsObol.Index.ToString() == "None")
            {
                Log.Warn("ItemStats integration: Cannot add - items weren't loaded at application start!");
                return;
            }

            ItemStatsMod.AddCustomItemStatDef(CharonsObol.Index, new ItemStatDef
            {
                Stats = new List<ItemStat>
                {
                    new(
                        (itemCount, ctx) => rules.GetReviveIncrease((int)itemCount),
                        (value, ctx) => 
                            Format(ITEM_STAT_CHARON_OBOL_REVIVE_SPEED_INCREASE, value.FormatPercentage(signed: true, decimalPlaces: 1))
                    ),
                    new(
                        (itemCount, ctx) => rules.GetReviveTime((int)itemCount, ctx.CountItems(DeadMansHandItem.Index)),
                        (value, ctx) => 
                            Format(ITEM_STAT_CHARON_OBOL_REVIVE_TIME_ALONE, value.FormatInt(postfix: "s", decimals: 1))
                    ),
                    new(
                        (itemCount, ctx) => rules.CalculateDeathTotemRadius((int)itemCount, 1),
                        (value, ctx) =>
                            Format(ITEM_STAT_CHARON_OBOL_REVIVE_CIRCLE_RANGE, value.FormatInt(postfix: "m", decimals: 1))
                    ),
                    new(
                        (itemCount, ctx) => rules.GetReviveReduceDamageFactor((int)itemCount, 0) - 1,
                        (value, ctx) =>
                            Format(ITEM_STAT_CHARON_OBOL_REVIVE_CIRCLE_DAMAGE, value.FormatPercentage(decimalPlaces: 1, signed: true))
                    )
                }
            });

            ItemStatsMod.AddCustomItemStatDef(DeadMansHandItem.Index, new ItemStatDef
            {
                Stats = new List<ItemStat>
                {
                    new(
                        (itemCount, ctx) => rules.GetReviveTimeIncrease(ctx.CountItems(CharonsObol.Index), (int)itemCount),
                        (value, ctx) => 
                            Format(ITEM_STAT_REVIVE_EVERYWHERE_REVIVE_CIRCLE_DAMAGE,value.FormatPercentage(decimalPlaces: 1, signed: true))
                    )
                }
            });
            
            Log.Info($"ItemStats integration: OK!");
        }
    }
}