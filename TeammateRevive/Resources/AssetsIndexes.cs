using R2API;
using RoR2;
using TeammateRevive.Logging;
using UnityEngine;
using static TeammateRevive.Common.TextFormatter;

namespace TeammateRevive.Resources
{
    public static class AssetsIndexes
    {
        public static class Keys
        {
            public const string DeathCurse = "DeathCurseBuff";
            public const string ReviveLinkBuff = "ReviveLinkBuff";

            public const string DeathCurseHiddenItem = "ReduceHpItem";
            public const string ReviveItem = "CharonsObol";
        }
        
        public static BuffIndex DeathCurseBuffIndex;
        public static BuffIndex ReviveLinkBuffIndex;
        public static ItemIndex DeathCurseItemIndex;
        public static ItemIndex CharonsObolItemIndex;
        
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
            CreateCharonsObolItem();
            
            CreateDeathCurseBuff();
            CreateReviveLinkBuff();
            
            On.RoR2.BuffCatalog.Init += BuffCatalogOnInit;
            On.RoR2.ItemCatalog.Init += ItemCatalogOnInit;
            Loaded = true;
        }

        static void CreateReviveLinkBuff()
        {
            BuffAPI.Add(new CustomBuff(Keys.ReviveLinkBuff, AddedAssets.ReviveLinkBuffIcon, Color.white, true, true));
        }
        

        static void CreateCharonsObolItem()
        {
            var full =
                $"- Reduces {Yellow("revival time")}. " +
                $"\n- Can be consumed for {Yellow("instant")} revival without Death Curse. " +
                $"\n- {Yellow("Removes")} additional {Red("Death Curse")} on stage change." +
                $"\n- Increase {Yellow("range")} and decrease {Yellow("damage")} for your revival.";
            
            var generalScale = new Vector3(0.05f, 0.05f, 0.05f);
            ItemDisplayRuleDict rules = new ItemDisplayRuleDict(new ItemDisplayRule[]
            {
                new()
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = AddedAssets.CharonsObolItemPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(-0.22f, 0f, 0f),
                    localAngles = new Vector3(0f, -0.05f, 0f),
                    localScale = generalScale
                }
            });
            
            ItemAPI.Add(new CustomItem(Keys.ReviveItem, "Charon's Obol", 
                full, 
                full,
                "Makes revival easier. Can be consumed for instant revival.", 
                AddedAssets.CharonsObolItemIcon,
                AddedAssets.CharonsObolItemPrefab, ItemTier.Tier2, new[]
                {
                    ItemTag.Healing,
                    ItemTag.AIBlacklist
                }, 
                true, false, itemDisplayRules: rules));
        }
        
        public static void CreateDeathCurseHiddenItem()
        {
            Log.DebugMethod();
            ItemAPI.Add(new CustomItem(Keys.DeathCurseHiddenItem, "Death curse", 
                "Reduces your max HP/Shield. Removed on next stage.", "Reduces your max HP/Shield. Removed on next stage.",
                "ITEM_REDUCEHP_PICK", 
                AddedAssets.DeathCurseBuffIcon,
                AddedAssets.CharonsObolItemPrefab, ItemTier.NoTier, new[]
                {
                    ItemTag.AIBlacklist
                }, 
                false, true));
            Log.DebugMethod("done");
        }

        public static void CreateDeathCurseBuff()
        {
            BuffAPI.Add(new CustomBuff(Keys.DeathCurse, AddedAssets.DeathCurseBuffIcon, Color.white, true, true));
        }


        private static void ItemCatalogOnInit(On.RoR2.ItemCatalog.orig_Init orig)
        {
            orig();
            
            DeathCurseItemIndex = ItemCatalog.FindItemIndex(Keys.DeathCurseHiddenItem);
            CharonsObolItemIndex = ItemCatalog.FindItemIndex(Keys.ReviveItem);
            InitedIndexes = true;
        }

        private static void BuffCatalogOnInit(On.RoR2.BuffCatalog.orig_Init orig)
        {
            orig();
            
            DeathCurseBuffIndex = BuffCatalog.FindBuffIndex(Keys.DeathCurse);
            ReviveLinkBuffIndex = BuffCatalog.FindBuffIndex(Keys.ReviveLinkBuff);
        }

    }
}