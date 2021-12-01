using System.Collections.Generic;
using System.Reflection;
using R2API;
using RoR2;
using TeammateRevive.Logging;
using TeammateRevive.Skull;
using UnityEngine;

namespace TeammateRevive.Resources
{
    public static class AddedAssets
    {
        public static void Init()
        {
            Log.Debug("Loading assets...");
            LoadSkullPrefab();
            ReadCurseAssets();
        }
        
        static void LoadSkullPrefab()
        {
            Log.DebugMethod();
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TeammateRevive.Resources.customprefabs");
            
            var bundle = AssetBundle.LoadFromStream(stream);
            ReplaceStubbedShaders(bundle);

            var dm = bundle.LoadAsset<GameObject>("Assets/PlayerDeathPoint.prefab");
            DeathMarkerPrefab = bundle.LoadAsset<GameObject>("Assets/PlayerDeathPoint.prefab");
            dm.AddComponent<DeadPlayerSkull>();
            DeathMarker = dm.InstantiateClone("Death Marker");
            dm.GetComponent<DeadPlayerSkull>().Setup();
            dm.GetComponent<DeadPlayerSkull>().radiusSphere.material = Materials[0];

            bundle.Unload(false);
        }
        
        static void ReadCurseAssets()
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TeammateRevive.Resources.reducehp");
            var bundle = AssetBundle.LoadFromStream(stream);

            CharonsObolItemPrefab = bundle.LoadAsset<GameObject>("Assets/models/Obol.prefab");
            HandItemPrefab = bundle.LoadAsset<GameObject>("Assets/models/hand_item.prefab");
            CharonsObolItemIcon = bundle.LoadAsset<Sprite>("Assets/icons/obol.png");
            DeathCurseBuffIcon = bundle.LoadAsset<Sprite>("Assets/icons/curse.png");
            ReviveLinkBuffIcon = bundle.LoadAsset<Sprite>("Assets/icons/timed_curse.png");
            LunarHandIcon = bundle.LoadAsset<Sprite>("Assets/icons/lunar_hand.png");
            
            DeathCurseArtifactEnabledIcon = bundle.LoadAsset<Sprite>("Assets/icons/artifactCurseEnabled.png");
            DeathCurseArtifactDisabledIcon = bundle.LoadAsset<Sprite>("Assets/icons/artifactCurseDisabled.png");

            ReplaceStubbedShaders(bundle);

            bundle.Unload(false);
        }

        static void ReplaceStubbedShaders(AssetBundle bundle)
        {
            var materialsL = bundle.LoadAllAssets<Material>();
            foreach (Material material in materialsL)
            {
                if (material.shader.name.StartsWith("StubbedShader"))
                {
                    material.shader = UnityEngine.Resources.Load<Shader>("shaders" + material.shader.name.Substring(13));
                    Materials.Add(material);
                }
            }
        }

        public static GameObject CharonsObolItemPrefab;
        public static GameObject HandItemPrefab;
        public static Sprite CharonsObolItemIcon;
        public static Sprite DeathCurseBuffIcon;
        public static Sprite ReviveLinkBuffIcon;
        public static Sprite LunarHandIcon;
        
        public static Sprite DeathCurseArtifactEnabledIcon;
        public static Sprite DeathCurseArtifactDisabledIcon;
        
        public static readonly List<Material> Materials = new();
        public static GameObject DeathMarkerPrefab { get; private set; }
        public static GameObject DeathMarker { get; private set; }
    }
}