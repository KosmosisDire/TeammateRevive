using R2API;
using RoR2;
using UnityEngine;

namespace TeammateRevive.Artifact
{
    public abstract class ArtifactBase
    {
        public abstract string ArtifactLangTokenName { get; }
        public abstract Sprite ArtifactEnabledIcon { get; }
        public abstract Sprite ArtifactDisabledIcon { get; }
        public ArtifactDef ArtifactDef;
        public bool ArtifactEnabled => RunArtifactManager.instance?.IsArtifactEnabled(ArtifactDef) ?? false;

        public abstract void Init();
        
        protected void CreateArtifact()
        {
            ArtifactDef = ScriptableObject.CreateInstance<ArtifactDef>();
            ArtifactDef.cachedName = "ARTIFACT_" + ArtifactLangTokenName;
            ArtifactDef.nameToken = "ARTIFACT_" + ArtifactLangTokenName + "_NAME";
            ArtifactDef.descriptionToken = "ARTIFACT_" + ArtifactLangTokenName + "_DESCRIPTION";
            ArtifactDef.smallIconSelectedSprite = ArtifactEnabledIcon;
            ArtifactDef.smallIconDeselectedSprite = ArtifactDisabledIcon;
            ContentAddition.AddArtifactDef(ArtifactDef);
        }
    }
}