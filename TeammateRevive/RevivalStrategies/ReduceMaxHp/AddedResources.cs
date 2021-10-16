using System.Reflection;
using System.Text;
using R2API;
using RoR2;
using TeammateRevival.Logging;
using UnityEngine;

namespace TeammateRevive.RevivalStrategies.ReduceMaxHp
{
    public static class PluginColors
    {
        public const string Green = "\"green\"";
        public const string Blue = "\"blue\"";
        public const string Black = "\"black\"";
        public const string Orange = "\"orange\"";
        public const string Purple = "\"purple\"";
        public const string Red = "\"red\"";
        public const string White = "\"white\"";
        public const string Yellow = "\"yellow\"";
        public const string ModifierColor = "#FFB6C1";
    }
    
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
            CreateResurrectItem();
            
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

        public static string WrapColor(object text, string color)
        {
            return $"<color={color}>{text}</color>";
        }

        static void CreateResurrectItem()
        {
            var description =
                $"Reduce time needed to resurrect fallen teammate {WrapColor("-12.5% per stack", PluginColors.Yellow)}. Can be consumed to resurrect instantly.";
            var full = description + $" On stage change, remove {WrapColor(1, PluginColors.Green)} additional {WrapColor("Death Curse", PluginColors.Red)} per stack.";
            ItemAPI.Add(new CustomItem(Keys.RespawnItem, "Charon's Obol", 
                full, 
                full,
                description, 
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
                "ITEM_REDUCEHP_PICK", 
                ReduceHpBuffIcon,
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
            ResurrectItemIndex = ItemCatalog.FindItemIndex(Keys.RespawnItem);
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
        public static ItemIndex ResurrectItemIndex;

        public static readonly Mesh CubeMesh = new()
        {
            vertices = new Vector3[]
            {
                new(0, 0, 0),
                new(1, 0, 0),
                new(1, 1, 0),
                new(0, 1, 0),
                new(0, 1, 1),
                new(1, 1, 1),
                new(1, 0, 1),
                new(0, 0, 1),
            },
            triangles = new[]
            {
                0, 2, 1, //face front
                0, 3, 2,
                2, 3, 4, //face top
                2, 4, 5,
                1, 2, 5, //face right
                1, 5, 6,
                0, 7, 4, //face left
                0, 4, 3,
                5, 4, 7, //face back
                5, 7, 6,
                0, 6, 7, //face bottom
                0, 1, 6
            }
        };
    }
}