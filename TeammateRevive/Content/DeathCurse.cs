using R2API;
using RoR2;
using TeammateRevive.Common;
using TeammateRevive.Resources;
using TeammateRevive.Revive.Rules;
using UnityEngine;

namespace TeammateRevive.Content
{
    public class DeathCurse : ContentBase
    {
        public static DeathCurse instance;
        
        private readonly ReviveRules rules;
        private readonly RunTracker run;
        
        public static string ItemName = "ITEM_DeathCurse";
        public static string BuffName = "BUFF_DeathCurse";
        public static string NameToken = "Death curse";
        
        public static ItemIndex ItemIndex;
        public static BuffIndex BuffIndex;

        public DeathCurse(ReviveRules rules, RunTracker run)
        {
            this.rules = rules;
            this.run = run;
        }

        public override void Init()
        {
            CreateDeathCurseHiddenItem();

            BuffDef buffDefinition = ScriptableObject.CreateInstance<BuffDef>();
            buffDefinition.name = BuffName;
            buffDefinition.iconSprite = CustomResources.DeathCurseBuffIcon;
            buffDefinition.buffColor = Color.white;
            buffDefinition.isDebuff = true;
            buffDefinition.canStack = true;

            ContentAddition.AddBuffDef(buffDefinition);
            
            On.RoR2.CharacterBody.RecalculateStats += OnCharacterBodyRecalculateStats;
        }

        public override void OnItemsAvailable() => ItemIndex = ItemCatalog.FindItemIndex(ItemName);
        public override void OnBuffsAvailable() => BuffIndex = BuffCatalog.FindBuffIndex(BuffName);

        static void CreateDeathCurseHiddenItem()
        {
            ItemAPI.Add(DeprecatedCustomItem.Create(ItemName, NameToken, 
                "Reduces your max HP/Shield. Removed on next stage.", "Reduces your max HP/Shield. Removed on next stage.",
                "ITEM_REDUCEHP_PICK", 
                CustomResources.DeathCurseBuffIcon,
                CustomResources.CharonsObolItemPrefab, ItemTier.NoTier, new[]
                {
                    ItemTag.AIBlacklist
                }, 
                false, true));
        }
        
        private void OnCharacterBodyRecalculateStats(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            if (self.inventory == null || !this.run.IsDeathCurseEnabled)
            {
                orig(self);
                return;
            }

            // cache previous values of health/shield, since they will be overriden on orig() call
            var health = self.healthComponent.health;
            var shield = self.healthComponent.shield;

            orig(self);

            var reducesCount = self.inventory.GetItemCount(ItemIndex);
            self.SetBuffCount(BuffIndex, reducesCount);
            if (reducesCount == 0)
                return;

            var actualReduceFactor = this.rules.GetCurseReduceHpFactor(reducesCount);
            var hpReduce = self.maxHealth - self.maxHealth / actualReduceFactor;
            var shieldReduce = self.maxShield - self.maxShield / actualReduceFactor;

            self.maxHealth -= hpReduce;
            self.maxShield -= shieldReduce;
            // original logic: maxHP = current max HP / cursePenalty
            self.cursePenalty += actualReduceFactor - 1;

            // this should cut excess health/shield on client
            if (NetworkHelper.IsServer)
            {
                self.healthComponent.Networkhealth = Mathf.Min(self.maxHealth, health);
                self.healthComponent.Networkshield = Mathf.Min(self.maxShield, self.healthComponent.shield, shield);
            }
            else
            {
                self.healthComponent.health = Mathf.Min(self.maxHealth, health);
                self.healthComponent.shield = Mathf.Min(self.maxShield, self.healthComponent.shield, shield);
            }
        }
    }
}