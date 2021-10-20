using BepInEx.Configuration;
using R2API;
using RoR2;
using UnityEngine;

namespace TeammateRevive.Artifact
{
    public abstract class ArtifactBase
    {
        public abstract string ArtifactName { get; }
        public abstract string ArtifactLangTokenName { get; }
        public abstract string ArtifactDescription { get; }
        public abstract Sprite ArtifactEnabledIcon { get; }
        public abstract Sprite ArtifactDisabledIcon { get; }
        public ArtifactDef ArtifactDef;
        public bool ArtifactEnabled => RunArtifactManager.instance?.IsArtifactEnabled(this.ArtifactDef) ?? false;
        public abstract void Init(ConfigFile config);
        protected void CreateLang()
        {
            LanguageAPI.Add("ARTIFACT_" + this.ArtifactLangTokenName + "_NAME", this.ArtifactName);
            LanguageAPI.Add("ARTIFACT_" + this.ArtifactLangTokenName + "_DESCRIPTION", this.ArtifactDescription);
        }
        protected void CreateArtifact()
        {
            this.ArtifactDef = ScriptableObject.CreateInstance<ArtifactDef>();
            this.ArtifactDef.cachedName = "ARTIFACT_" + this.ArtifactLangTokenName;
            this.ArtifactDef.nameToken = "ARTIFACT_" + this.ArtifactLangTokenName + "_NAME";
            this.ArtifactDef.descriptionToken = "ARTIFACT_" + this.ArtifactLangTokenName + "_DESCRIPTION";
            this.ArtifactDef.smallIconSelectedSprite = this.ArtifactEnabledIcon;
            this.ArtifactDef.smallIconDeselectedSprite = this.ArtifactDisabledIcon;
            ArtifactAPI.Add(this.ArtifactDef);
        }
        public abstract void Hooks();
    }
}