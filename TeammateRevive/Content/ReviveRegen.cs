using R2API;
using RoR2;
using TeammateRevive.Common;
using TeammateRevive.Revive.Rules;
using UnityEngine;

namespace TeammateRevive.Content
{
    public class ReviveRegen : ContentBase
    {
        public static string Name = "BUFF_ReviveRegen";
        public static BuffIndex Index;
        
        private readonly ReviveRules rules;

        public ReviveRegen(ReviveRules rules)
        {
            this.rules = rules;
        }

        public override void Init()
        {
            var sprite = UnityEngine.Resources.Load<Sprite>("textures/bufficons/texBuffRegenBoostIcon");

            BuffDef buffDefinition = ScriptableObject.CreateInstance<BuffDef>();
            buffDefinition.name = Name;
            buffDefinition.iconSprite = sprite;
            buffDefinition.buffColor = new Color(0.4f, 0.792f, 0.38f);
            buffDefinition.isDebuff = false;
            buffDefinition.canStack = false;

            ContentAddition.AddBuffDef(buffDefinition);

            On.RoR2.CharacterBody.Update += OnCharacterBodyUpdate;
        }

        protected override void OnBuffsAvailable() => Index = BuffCatalog.FindBuffIndex(Name);

        void OnCharacterBodyUpdate(On.RoR2.CharacterBody.orig_Update orig, CharacterBody self)
        {
            orig(self);
            var buffs = self.GetBuffCount(Index);
            if (buffs == 0)
                return;
            var hasTranscendence = self.inventory.GetItemCount(RoR2Content.Items.ShieldOnly) > 0;

            var effectiveHp = self.maxHealth + self.maxShield;
            var regenSpeed = (effectiveHp * this.rules.Values.PostReviveRegenFraction) /
                             this.rules.Values.PostReviveRegenDurationSec;
            var regenValue = regenSpeed * Time.deltaTime;
            
            if (hasTranscendence)
            {
                var value = Mathf.Clamp(self.healthComponent.shield + regenValue, 1, self.maxShield);
                
                if (NetworkHelper.IsServer)
                    self.healthComponent.Networkshield = value;
                else
                    self.healthComponent.shield = value;
            }
            else
            {
                var value = Mathf.Clamp(self.healthComponent.health + regenValue, 1, self.maxHealth);
                
                if (NetworkHelper.IsServer)
                    self.healthComponent.Networkhealth = value;
                else
                    self.healthComponent.health = value;
            }
        }
    }
}