using System.Collections.Generic;
using System.Reflection;
using R2API;
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
            ReadCurseAssets();
            LoadSkullPrefab();
        }
        
        static void ReadCurseAssets()
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TeammateRevive.Resources.reducehp");
            var bundle = AssetBundle.LoadFromStream(stream);

            CharonsObolItemPrefab = bundle.LoadAsset<GameObject>("Assets/Obol.prefab");
            CharonsObolItemIcon = bundle.LoadAsset<Sprite>("Assets/Icons/obol.png");
            DeathCurseBuffIcon = bundle.LoadAsset<Sprite>("Assets/Icons/curse.png");
            ReviveInvolvementBuffIcon = bundle.LoadAsset<Sprite>("Assets/Icons/timed_curse.png");
            
            DeathCurseArtifactEnabledIcon = bundle.LoadAsset<Sprite>("Assets/Icons/artifactCurseEnabled.png");
            DeathCurseArtifactDisabledIcon = bundle.LoadAsset<Sprite>("Assets/Icons/artifactCurseDisabled.png");

            bundle.Unload(false);
        }
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
                    Materials.Add(material);
                }
            }

            var dm = bundle.LoadAsset<GameObject>("Assets/PlayerDeathPoint.prefab");
            DeathMarkerPrefab = bundle.LoadAsset<GameObject>("Assets/PlayerDeathPoint.prefab");
            dm.AddComponent<DeadPlayerSkull>();
            DeathMarker = dm.InstantiateClone("Death Marker");
            dm.GetComponent<DeadPlayerSkull>().Setup();
            dm.GetComponent<DeadPlayerSkull>().radiusSphere.material = Materials[0];

            bundle.Unload(false);
        }
        
        public static GameObject CharonsObolItemPrefab;
        public static Sprite CharonsObolItemIcon;
        public static Sprite DeathCurseBuffIcon;
        public static Sprite ReviveInvolvementBuffIcon;
        
        public static Sprite DeathCurseArtifactEnabledIcon;
        public static Sprite DeathCurseArtifactDisabledIcon;
        
        public static readonly List<Material> Materials = new();
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