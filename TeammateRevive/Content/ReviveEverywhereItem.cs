﻿using R2API;
using RoR2;
using TeammateRevive.Resources;
using static TeammateRevive.Common.TextFormatter;

namespace TeammateRevive.Content
{
    public class ReviveEverywhereItem : ContentBase
    {
        public static string Name = "ITEM_ReviveEverywhere";
        public static ItemIndex Index;
        
        public override void Init()
        {
            var full =
                $"- First item {Red("increases")} {Yellow("revival time")} times 2. " +
                $"\n- {Yellow("Revive")} dead teammates {Yellow("everywhere")} on map." +
                $"\n- Reduces {Yellow("revival time")} when you are reviving.";
            
            ItemAPI.Add(new CustomItem(Name, "Dead Man's Hand", 
                full, "Revive death teammates everywhere on map.",
                "Revive death teammates everywhere on map", 
                AddedAssets.LunarHandIcon,
                AddedAssets.HandItemPrefab, ItemTier.Lunar, new[]
                {
                    ItemTag.AIBlacklist
                }, 
                true, false));
        }

        protected override void OnItemsAvailable() => Index = ItemCatalog.FindItemIndex(Name);
    }
}