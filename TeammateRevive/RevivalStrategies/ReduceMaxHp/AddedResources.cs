using System;
using System.IO;
using System.Reflection;
using R2API;
using RoR2;
using TeammateRevival;
using TeammateRevival.Logging;
using UnityEngine;

namespace TeammateRevive.RevivalStrategies.ReduceMaxHp
{
    public static class AddedResources
    {
        public static class Keys
        {
            public const string ReduceHpBuffName = "ReduceHpBuff";
            public const string ReduceShieldBuffName = "ReduceShieldBuff";

            public const string ReduceHpItem = "ReduceHpItem";
            public const string RespawnItem = "CharonsObol";
        }
        
        private static bool inited = false;
        public static void Init()
        {
            if (inited)
            {
                Log.WarnMethod("called twice");
                return;
            }

            ReadAssets();

            CreateRevivePenaltyItem();
            CreateRespawnItem();
            
            CreateReduceHpBuff();
            CreateReduceShieldBuff();
            
            On.RoR2.BuffCatalog.Init += BuffCatalogOnInit;
            On.RoR2.ItemCatalog.Init += ItemCatalogOnInit;
            inited = true;
        }

        static void ReadAssets()
        {
            Log.Debug("Loading assets...");
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TeammateRevive.Resources.reducehp");
            var bundle = AssetBundle.LoadFromStream(stream);

            ReduceHpItemPrefab = bundle.LoadAsset<GameObject>("Assets/Obol.prefab");
            RespawnItemIcon = bundle.LoadAsset<Sprite>("Assets/Icons/obol.png");
            ReduceHpBuffIcon = bundle.LoadAsset<Sprite>("Assets/Icons/curse.png");

            bundle.Unload(false);
        }

        private static GameObject ReduceHpItemPrefab;
        private static Sprite RespawnItemIcon;
        private static Sprite ReduceHpBuffIcon;

        static void CreateRespawnItem()
        {
            ItemAPI.Add(new CustomItem(Keys.RespawnItem, "Charon's Obol", 
                "Used for revive you or fallen teammate.", 
                "Used for revive you or fallen teammate.",
                "Used for revive you or fallen teammate.", 
                RespawnItemIcon,
                ReduceHpItemPrefab, ItemTier.Tier2, new[]
                {
                    ItemTag.Healing,
                    ItemTag.CannotCopy
                }, 
                false, false));
        }
        
        public static void CreateRevivePenaltyItem()
        {
            Log.DebugMethod();
            // var sprite = CreateSprite(CurseTexture);
            ItemAPI.Add(new CustomItem(Keys.ReduceHpItem, "Death curse", 
                "Reduces your max HP/Shield. Removed on next stage.", "Reduces your max HP/Shield. Removed on next stage.",
                "ITEM_REDUCEHP_PICK", ReduceHpBuffIcon,
                ReduceHpItemPrefab, ItemTier.NoTier, new[]
                {
                    ItemTag.CannotCopy
                }, 
                false, true));
            Log.DebugMethod("done");
        }

        public static void CreateReduceHpBuff()
        {
            BuffAPI.Add(new CustomBuff(Keys.ReduceHpBuffName, ReduceHpBuffIcon, Color.white, true, true));
        }
        
        public static void CreateReduceShieldBuff()
        {
            BuffAPI.Add(new CustomBuff(Keys.ReduceShieldBuffName, ReduceHpBuffIcon, Color.blue, true, true));
        }


        private static void ItemCatalogOnInit(On.RoR2.ItemCatalog.orig_Init orig)
        {
            orig();
            
            ReduceHpItemIndex = ItemCatalog.FindItemIndex(Keys.ReduceHpItem);
            RespawnItemIndex = ItemCatalog.FindItemIndex(Keys.RespawnItem);
        }

        private static void BuffCatalogOnInit(On.RoR2.BuffCatalog.orig_Init orig)
        {
            orig();
            
            ReduceHpBuffIndex = BuffCatalog.FindBuffIndex(Keys.ReduceHpBuffName);
            ReduceShieldBuffIndex = BuffCatalog.FindBuffIndex(Keys.ReduceShieldBuffName);
        }

        public static BuffIndex ReduceHpBuffIndex;
        public static BuffIndex ReduceShieldBuffIndex;
        public static ItemIndex ReduceHpItemIndex;
        public static ItemIndex RespawnItemIndex;
    }
}