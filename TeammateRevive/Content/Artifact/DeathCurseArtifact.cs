using RoR2;
using TeammateRevive.Common;
using TeammateRevive.Localization;
using TeammateRevive.Logging;
using TeammateRevive.Resources;
using TeammateRevive.Revive.Rules;
using UnityEngine;

namespace TeammateRevive.Artifact
{
    public class DeathCurseArtifact : ArtifactBase
    {
        public override string ArtifactLangTokenName => "DEATH_CURSE";

        public override Sprite ArtifactEnabledIcon => CustomResources.DeathCurseArtifactEnabledIcon;
        public override Sprite ArtifactDisabledIcon => CustomResources.DeathCurseArtifactDisabledIcon;

        public override void Init()
        {
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
                    baseToken = TextFormatter.Yellow(Language.GetString(LanguageConsts.ARTIFACT_DEATH_CURSE_DISABLED))
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
                var message = Language.GetString(LanguageConsts.ARTIFACT_DEATH_CURSE_ENFORCED_BY_SERVER);
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