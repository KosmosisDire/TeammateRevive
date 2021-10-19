using R2API;
using RoR2;
using TeammateRevive.Logging;
using TeammateRevive.Revive.Rules;
using UnityEngine;
using static TeammateRevive.Common.TextFormatter;

namespace TeammateRevive.Resources
{
    public static class ItemsAndBuffs
    {
        public static class Keys
        {
            public const string DeathCurse = "DeathCurseBuff";
            public const string ReviveInvolvementBuff = "ReviveInvolvementBuff";

            public const string DeathCurseHiddenItem = "ReduceHpItem";
            public const string ReviveItem = "CharonsObol";
        }
        
        public static bool Loaded { get; private set; }
        public static bool InitedIndexes { get; private set; }
        public static void Init()
        {
            if (Loaded)
            {
                Log.WarnMethod("called twice");
                return;
            }

            CreateDeathCurseHiddenItem();
            CreateReviveItem();
            
            CreateReduceHpBuff();
            CreateReviveInvolvementBuff();
            
            On.RoR2.BuffCatalog.Init += BuffCatalogOnInit;
            On.RoR2.ItemCatalog.Init += ItemCatalogOnInit;
            Loaded = true;
        }

        static void CreateReviveInvolvementBuff()
        {
            BuffAPI.Add(new CustomBuff(Keys.ReviveInvolvementBuff, AddedAssets.ReviveInvolvementBuffIcon, Color.white, true, true));
        }
        

        static void CreateReviveItem()
        {
            var perStackIncrease = (new ReviveRuleValues().ObolReviveFactor - 1) * 100;
            var description =
                $"Reduce time needed to resurrect fallen teammate {Yellow($"-{perStackIncrease:F2}% per stack")}. Can be consumed to resurrect instantly.";
            var full = description + $" On stage change, remove {Green("1")} additional {Red("Death Curse")} per stack. Will also increase {Yellow("revive range & damage/sec")} when someone is reviving YOU.";
            
            Vector3 generalScale = new Vector3(0.05f, 0.05f, 0.05f);
            ItemDisplayRuleDict rules = new ItemDisplayRuleDict(new ItemDisplayRule[]
            {
                new()
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = AddedAssets.ReviveItemPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(-0.22f, 0f, 0f),
                    localAngles = new Vector3(0f, -0.05f, 0f),
                    localScale = generalScale
                }
            });
            
            ItemAPI.Add(new CustomItem(Keys.ReviveItem, "Charon's Obol", 
                full, 
                full,
                description, 
                AddedAssets.ReviveItemIcon,
                AddedAssets.ReviveItemPrefab, ItemTier.Tier2, new[]
                {
                    ItemTag.Healing,
                    ItemTag.CannotCopy
                }, 
                false, false, itemDisplayRules: rules));
        }
        
        public static void CreateDeathCurseHiddenItem()
        {
            Log.DebugMethod();
            ItemAPI.Add(new CustomItem(Keys.DeathCurseHiddenItem, "Death curse", 
                "Reduces your max HP/Shield. Removed on next stage.", "Reduces your max HP/Shield. Removed on next stage.",
                "ITEM_REDUCEHP_PICK", 
                AddedAssets.RevivePenaltyBuffIcon,
                AddedAssets.ReviveItemPrefab, ItemTier.NoTier, new[]
                {
                    ItemTag.CannotCopy
                }, 
                false, true));
            Log.DebugMethod("done");
        }

        public static void CreateReduceHpBuff()
        {
            BuffAPI.Add(new CustomBuff(Keys.DeathCurse, AddedAssets.RevivePenaltyBuffIcon, Color.white, true, true));
        }


        private static void ItemCatalogOnInit(On.RoR2.ItemCatalog.orig_Init orig)
        {
            orig();
            
            ReduceHpItemIndex = ItemCatalog.FindItemIndex(Keys.DeathCurseHiddenItem);
            ReviveItemIndex = ItemCatalog.FindItemIndex(Keys.ReviveItem);
            InitedIndexes = true;
        }

        private static void BuffCatalogOnInit(On.RoR2.BuffCatalog.orig_Init orig)
        {
            orig();
            
            DeathCurseBuffIndex = BuffCatalog.FindBuffIndex(Keys.DeathCurse);
            ReviveInvolvementBuffIndex = BuffCatalog.FindBuffIndex(Keys.ReviveInvolvementBuff);
        }

        public static BuffIndex DeathCurseBuffIndex;
        public static BuffIndex ReviveInvolvementBuffIndex;
        public static ItemIndex ReduceHpItemIndex;
        public static ItemIndex ReviveItemIndex;
    }
}