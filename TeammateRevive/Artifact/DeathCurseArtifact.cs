using BepInEx.Configuration;
using TeammateRevive.Resources;
using UnityEngine;

namespace TeammateRevive.Artifact
{
    public class DeathCurseArtifact : ArtifactBase
    {
        public override string ArtifactName => "Artifact of Death Curse";
        public override string ArtifactLangTokenName => "DEATH_CURSE";

        public override string ArtifactDescription =>
            "Adds Death Curse on revive, but also adds Charon's Obol item that will make revive easier.";

        public override Sprite ArtifactEnabledIcon => AddedAssets.DeathCurseArtifactEnabledIcon;
        public override Sprite ArtifactDisabledIcon => AddedAssets.DeathCurseArtifactDisabledIcon;
        
        public override void Init(ConfigFile config)
        {
            CreateLang();
            CreateArtifact();
        }

        public override void Hooks()
        {
        }
    }
}