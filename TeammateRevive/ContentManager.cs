using System;
using System.Collections.Generic;
using RoR2;
using TeammateRevive.Artifact;
using TeammateRevive.Content;
using TeammateRevive.Localization;
using TeammateRevive.Logging;
using TeammateRevive.Revive.Rules;
using BuffCatalog = On.RoR2.BuffCatalog;
using ItemCatalog = On.RoR2.ItemCatalog;
using RoR2Content = RoR2.RoR2Content;

namespace TeammateRevive
{
    public class ContentManager
    {
        private readonly ReviveRules rules;
        private readonly RunTracker run;
        private readonly DeathCurseArtifact deathCurseArtifact;
        private List<ContentBase> addedContent = new();
        
        public static bool ContentInited;

        public ContentManager(ReviveRules rules, RunTracker run, DeathCurseArtifact deathCurseArtifact)
        {
            this.rules = rules;
            this.run = run;
            this.deathCurseArtifact = deathCurseArtifact;
            On.RoR2.Language.SetStringByToken += LanguageOnSetStringByToken;
            On.RoR2.ItemCatalog.Init += ItemsOnInit;
            On.RoR2.BuffCatalog.Init += BuffsOnInit;
        }

        private void LanguageOnSetStringByToken(On.RoR2.Language.orig_SetStringByToken orig, RoR2.Language self, string token, string localizedstring)
        {
            if (RoR2Content.Items.ShieldOnly && token == RoR2Content.Items.ShieldOnly.descriptionToken)
            {
                localizedstring += Language.GetString(LanguageConsts.TEAMMATE_REVIVAL_SHIELD_ONLY_POSTFIX);
            }
            orig(self, token, localizedstring);
            if (RoR2Content.Items.ShieldOnly && token == RoR2Content.Items.ShieldOnly.descriptionToken)
            {
                Log.Info($"Transcendence description ({token}) patched!");
            }
        }

        private void BuffsOnInit(BuffCatalog.orig_Init orig)
        {
            orig();
            if (!ContentInited)
            {
                Log.Error($"{nameof(BuffsOnInit)} called before content was inited!");
            }
            
            foreach (var content in addedContent)
            {
                try
                {
                    content.OnBuffsAvailable();
                }
                catch (Exception ex)
                {
                    Log.Error($"Error on OnBuffsAvailable for {content}: {ex}");
                }
            }
        }

        private void ItemsOnInit(ItemCatalog.orig_Init orig)
        {
            orig();
            if (!ContentInited)
            {
                Log.Error($"{nameof(ItemsOnInit)} called before content was inited!");
            }
            foreach (var content in addedContent)
            {
                try
                {
                    content.OnItemsAvailable();
                }
                catch (Exception ex)
                {
                    Log.Error($"Error on OnBuffsAvailable for {content}: {ex}");
                }
            }
        }

        public void Init()
        {
            LoadAddedContent();
            deathCurseArtifact.Init();
        }
        
        public void LoadAddedContent()
        {
            addedContent = new List<ContentBase>
            {
                new DeathCurse(rules, run),
                new CharonsObol(),
                new DeadMansHandItem(),
                new ReviveLink(),
                new ReviveRegen(rules),
                new RevivalToken()
            };
            
            foreach (var content in addedContent)
            {
                content.Init();
                content.GetType().GetField("instance")
                    ?.SetValue(null, content);
            }

            ContentInited = true;
        }

    }
}
