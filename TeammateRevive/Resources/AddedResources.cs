using System.Collections.Generic;
using System.Reflection;
using R2API;
using RoR2;
using TeammateRevive.Logging;
using TeammateRevive.Revive;
using TeammateRevive.Skull;
using UnityEngine;
using static TeammateRevive.Common.TextFormatter;

namespace TeammateRevive.Resources
{
    public static class AddedResources
    {
        public static class Keys
        {
            public const string DeathCurse = "DeathCurseBuff";

            public const string RevivePenaltyItem = "ReduceHpItem";
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

            ReadCurseAssets();
            LoadSkullPrefab();

            CreateRevivePenaltyItem();
            CreateReviveItem();
            
            CreateReduceHpBuff();
            
            On.RoR2.BuffCatalog.Init += BuffCatalogOnInit;
            On.RoR2.ItemCatalog.Init += ItemCatalogOnInit;
            Loaded = true;
        }

        static void ReadCurseAssets()
        {
            Log.Debug("Loading assets...");
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TeammateRevive.Resources.reducehp");
            var bundle = AssetBundle.LoadFromStream(stream);

            ReviveItemPrefab = bundle.LoadAsset<GameObject>("Assets/Obol.prefab");
            ReviveItemIcon = bundle.LoadAsset<Sprite>("Assets/Icons/obol.png");
            RevivePenaltyBuffIcon = bundle.LoadAsset<Sprite>("Assets/Icons/curse.png");

            bundle.Unload(false);
        }

        private static GameObject ReviveItemPrefab;
        private static Sprite ReviveItemIcon;
        private static Sprite RevivePenaltyBuffIcon;
        private static List<Material> materials = new();
        
        static void LoadSkullPrefab()
        {
            Log.DebugMethod();
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TeammateRevive.Resources.customprefabs");
            
            var bundle = AssetBundle.LoadFromStream(stream);
            var materialsL = bundle.LoadAllAssets<Material>();
            foreach (Material material in materialsL)
            {
                if (material.shader.name.StartsWith("StubbedShader"))
                {
                    material.shader = UnityEngine.Resources.Load<Shader>("shaders" + material.shader.name.Substring(13));
                    materials.Add(material);
                }
            }

            var dm = bundle.LoadAsset<GameObject>("Assets/PlayerDeathPoint.prefab");
            DeathMarkerPrefab = bundle.LoadAsset<GameObject>("Assets/PlayerDeathPoint.prefab");
            dm.AddComponent<DeadPlayerSkull>();
            DeathMarker = dm.InstantiateClone("Death Marker");
            dm.GetComponent<DeadPlayerSkull>().Setup();
            dm.GetComponent<DeadPlayerSkull>().radiusSphere.material = materials[0];

            bundle.Unload(false);
        }
        

        static void CreateReviveItem()
        {
            var perStackIncrease = (RevivalTracker.ObolReviveFactor - 1) * 100;
            var description =
                $"Reduce time needed to resurrect fallen teammate {Yellow($"-{perStackIncrease:F2}% per stack")}. Can be consumed to resurrect instantly.";
            var full = description + $" On stage change, remove {Green("1")} additional {Red("Death Curse")} per stack.";
            
            Vector3 generalScale = new Vector3(0.05f, 0.05f, 0.05f);
            ItemDisplayRuleDict rules = new ItemDisplayRuleDict(new ItemDisplayRule[]
            {
                new()
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ReviveItemPrefab,
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
                ReviveItemIcon,
                ReviveItemPrefab, ItemTier.Tier2, new[]
                {
                    ItemTag.Healing,
                    ItemTag.CannotCopy
                }, 
                false, false, itemDisplayRules: rules));
        }
        
        public static void CreateRevivePenaltyItem()
        {
            Log.DebugMethod();
            // var sprite = CreateSprite(CurseTexture);
            ItemAPI.Add(new CustomItem(Keys.RevivePenaltyItem, "Death curse", 
                "Reduces your max HP/Shield. Removed on next stage.", "Reduces your max HP/Shield. Removed on next stage.",
                "ITEM_REDUCEHP_PICK", 
                RevivePenaltyBuffIcon,
                ReviveItemPrefab, ItemTier.NoTier, new[]
                {
                    ItemTag.CannotCopy
                }, 
                false, true));
            Log.DebugMethod("done");
        }

        public static void CreateReduceHpBuff()
        {
            BuffAPI.Add(new CustomBuff(Keys.DeathCurse, RevivePenaltyBuffIcon, Color.white, true, true));
        }


        private static void ItemCatalogOnInit(On.RoR2.ItemCatalog.orig_Init orig)
        {
            orig();
            
            ReduceHpItemIndex = ItemCatalog.FindItemIndex(Keys.RevivePenaltyItem);
            ReviveItemIndex = ItemCatalog.FindItemIndex(Keys.ReviveItem);
            InitedIndexes = true;
        }

        private static void BuffCatalogOnInit(On.RoR2.BuffCatalog.orig_Init orig)
        {
            orig();
            
            ReduceHpBuffIndex = BuffCatalog.FindBuffIndex(Keys.DeathCurse);
        }

        public static BuffIndex ReduceHpBuffIndex;
        public static ItemIndex ReduceHpItemIndex;
        public static ItemIndex ReviveItemIndex;
        public static GameObject DeathMarkerPrefab { get; private set; }
        public static GameObject DeathMarker { get; private set; }

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