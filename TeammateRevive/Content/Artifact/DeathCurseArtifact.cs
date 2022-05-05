using RoR2;
using TeammateRevive.Common;
using TeammateRevive.Logging;
using TeammateRevive.Resources;
using TeammateRevive.Revive.Rules;
using UnityEngine;

namespace TeammateRevive.Artifact
{
    public class DeathCurseArtifact : ArtifactBase
    {        
        public override string ArtifactName => "Artifact of Death Curse";
        public override string ArtifactLangTokenName => "DEATH_CURSE";

        public override string ArtifactDescription =>
            "Adds Death Curse on revive, but also adds Charon's Obol item that will make revive easier.";

        public override Sprite ArtifactEnabledIcon => CustomResources.DeathCurseArtifactEnabledIcon;
        public override Sprite ArtifactDisabledIcon => CustomResources.DeathCurseArtifactDisabledIcon;

        public override void Init()
        {
            CreateLang();
            CreateArtifact();
        }

        public void EnsureEnabled(ReviveRules rules)
        {
            // disable artifact if single player
            if (Run.instance.participatingPlayerCount == 1 
                && !rules.Values.ForceEnableDeathCurseForSinglePlayer
                && RunArtifactManager.instance.IsArtifactEnabled(ArtifactDef))
            {
                RunArtifactManager.instance.SetArtifactEnabledServer(ArtifactDef, false);
                Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                {
                    baseToken = TextFormatter.Yellow("Artifact of Death Curse is disabled because run started in single player.")
                });
                return;
            }
            
            // enforce artifact if needed
            if (
                (Run.instance.participatingPlayerCount > 1 || rules.Values.ForceEnableDeathCurseForSinglePlayer)
                && rules.Values.ForceDeathCurseRule
                && !ArtifactEnabled
                && NetworkHelper.IsServer
            ) {
                var message = "Artifact of Death Curse is enforced by server.";
                RunArtifactManager.instance.SetArtifactEnabledServer(ArtifactDef, true);
                Log.Info(message);
                Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                {
                    baseToken = TextFormatter.Yellow(message)
                });
            }            
        }
    }
}