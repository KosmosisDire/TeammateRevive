using System.Collections.Generic;
using System.Reflection;
using R2API;
using RoR2;
using TeammateRevive.Logging;
using TeammateRevive.DeathTotem;
using UnityEngine;
using UnityEngine.UI;

namespace TeammateRevive.Resources
{
    public static class CustomResources
    {

        static void InitializeDeathTotem(GameObject totemPrefab)
        {
            Log.DebugMethod();
            totemPrefab.AddComponent<DeathTotemBehavior>();
            DeathTotem = totemPrefab.InstantiateClone("Death Totem").GetComponent<DeathTotemBehavior>();
            totemPrefab.GetComponent<DeathTotemBehavior>().Setup();
        }

        public static void LoadCustomResources()
        {
            Log.DebugMethod();
            Log.Debug("Loading custom resources...");

            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TeammateRevive.Resources.customresources");
            var bundle = AssetBundle.LoadFromStream(stream);
            
            ReplaceStubbedShaders(bundle);

            CharonsObolItemIcon = bundle.LoadAsset<Sprite>("Assets/CustomAssets/icons/obol.png");
            DeathCurseBuffIcon = bundle.LoadAsset<Sprite>("Assets/CustomAssets/icons/curse.png");
            ReviveLinkBuffIcon = bundle.LoadAsset<Sprite>("Assets/CustomAssets/icons/timed_curse.png");
            LunarHandIcon = bundle.LoadAsset<Sprite>("Assets/CustomAssets/icons/lunar_hand.png");
            DeathCurseArtifactEnabledIcon = bundle.LoadAsset<Sprite>("Assets/CustomAssets/icons/artifactCurseEnabled.png");
            DeathCurseArtifactDisabledIcon = bundle.LoadAsset<Sprite>("Assets/CustomAssets/icons/artifactCurseDisabled.png");

            CurseOrbPrefab = bundle.LoadAsset<GameObject>("Assets/CustomAssets/curseOrb.prefab");
            progressBarPrefab = bundle.LoadAsset<GameObject>("Assets/CustomAssets/progressBar.prefab");
            CharonsObolItemPrefab = bundle.LoadAsset<GameObject>("Assets/CustomAssets/obol.prefab");
            HandItemPrefab = bundle.LoadAsset<GameObject>("Assets/CustomAssets/handItem.prefab");
            InitializeDeathTotem(bundle.LoadAsset<GameObject>("Assets/CustomAssets/deathTotem.prefab"));

            bundle.Unload(false);
        }

        static void ReplaceStubbedShaders(AssetBundle bundle)
        {
            Log.Debug($"Replacing the stubbed shaders of the {bundle.name} asset bundle");

            var materialsL = bundle.LoadAllAssets<Material>();

            Log.Debug($"Found {materialsL.Length} materials in the asset bundle");

            foreach (Material material in materialsL)
            {
                Log.Debug($"Loading the material {material.name}");

                if (material.shader.name.StartsWith("StubbedShader"))
                {
                    Log.Debug($"Loading the stubbed shared for shader {material.shader.name}");

                    string shaderPath = $"shaders{material.shader.name.Substring(13)}";
                    Shader materialShader = LegacyResourcesAPI.Load<Shader>(shaderPath);

                    if (materialShader is null)
                    {
                        Log.Warn($"Could not find the shader for material {material.name} at the path {shaderPath}");
                    }
                    else
                    {
                        Log.Debug($"Loaded the stubbed shared {materialShader.name} from the path {shaderPath}");
                    }

                    material.shader = materialShader;

                    Materials.Add(material);
                }
                else
                {
                    Log.Debug("The shader was not loaded because it is not a stubbed shader");
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
        public static DeathTotemBehavior DeathTotem { get; private set; }
        public static GameObject CurseOrbPrefab;

        public static GameObject progressBarPrefab;
    }
}