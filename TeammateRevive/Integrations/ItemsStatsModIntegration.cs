using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ItemStats;
using ItemStats.Stat;
using ItemStats.ValueFormatters;
using RoR2;
using TeammateRevive.Configuration;
using TeammateRevive.Logging;
using TeammateRevive.Resources;
using TeammateRevive.Revive;
using UnityEngine;

namespace TeammateRevive.Integrations
{
    public class ItemsStatsModIntegration
    {
        private readonly PluginConfig config;

        public ItemsStatsModIntegration(PluginConfig config)
        {
            this.config = config;
            RoR2Application.onLoad += () =>
            {
                if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("dev.ontrigger.itemstats"))
                {
                    this.AddToItemStats();
                }
            };
        }
        
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public void AddToItemStats()
        {
            if (!AddedResources.InitedIndexes || AddedResources.ReviveItemIndex.ToString() == "None")
            {
                Log.Warn("Cannot add ItemStats integration - items weren't loaded at application start!");
                return;
            }

            ItemStatsMod.AddCustomItemStatDef(AddedResources.ReviveItemIndex, new ItemStatDef
            {
                Stats = new List<ItemStat>
                {
                    new(
                        (itemCount, ctx) => this.config.ReviveTimeSeconds / ( this.config.ReviveTimeSeconds / Mathf.Pow(RevivalTracker.ObolReviveFactor, itemCount)) - 1,
                        (value, ctx) => $"Revive speed increased by {value.FormatPercentage(signed: true, decimalPlaces: 1)}"
                    ),
                    new(
                        (itemCount, ctx) => this.config.ReviveTimeSeconds / Mathf.Pow(RevivalTracker.ObolReviveFactor, itemCount),
                        (value, ctx) => $"Time to revive alone: {value.FormatInt(postfix: "s", decimals: 1)}"
                    )
                }
            });
            
            Log.Info($"Added ItemStats integration! Idx {AddedResources.ReviveItemIndex}");
        }
    }
}