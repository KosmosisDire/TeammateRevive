using System.Collections.Generic;
using On.RoR2;
using TeammateRevive.Artifact;
using TeammateRevive.Content;
using TeammateRevive.Logging;
using TeammateRevive.Resources;
using TeammateRevive.Revive.Rules;
using RoR2Content = RoR2.RoR2Content;

namespace TeammateRevive
{
    public class ContentManager
    {
        private readonly ReviveRules rules;
        private readonly RunTracker run;
        private readonly DeathCurseArtifact deathCurseArtifact;
        private List<ContentBase> addedContent = new();

        public ContentManager(ReviveRules rules, RunTracker run, DeathCurseArtifact deathCurseArtifact)
        {
            this.rules = rules;
            this.run = run;
            this.deathCurseArtifact = deathCurseArtifact;
            Language.SetStringByToken += LanguageOnSetStringByToken;
        }

        private void LanguageOnSetStringByToken(Language.orig_SetStringByToken orig, RoR2.Language self, string token, string localizedstring)
        {
            if (token == RoR2Content.Items.ShieldOnly.descriptionToken)
            {
                localizedstring += " Teammate reviving will damage shield instead of health.";
            }
            orig(self, token, localizedstring);
            if (token == RoR2Content.Items.ShieldOnly.descriptionToken)
            {
                Log.Info($"Transcendence description ({token}) patched!");
            }
        }

        public void Init()
        {
            AddedAssets.Init();
            LoadAddedContent();
            this.deathCurseArtifact.Init();
        }
        
        public void LoadAddedContent()
        {
            this.addedContent = new List<ContentBase>
            {
                new DeathCurse(this.rules, this.run),
                new CharonsObol(),
                new ReviveEverywhereItem(),
                new ReviveLink(),
                new ReviveRegen(this.rules),
                new RevivalToken()
            };
            
            foreach (var content in this.addedContent)
            {
                content.Init();
                content.GetType().GetField("instance")
                    ?.SetValue(null, content);
            }

            ContentBase.ContentInited = true;
        }

    }
}