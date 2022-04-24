using R2API;
using RoR2;
using TeammateRevive.Resources;
using UnityEngine;

namespace TeammateRevive.Content
{
    public class ReviveLink : ContentBase
    {
        public static string Name = "BUFF_ReviveLink";
        public static BuffIndex Index;
        
        public override void Init()
        {
            BuffDef buffDefinition = ScriptableObject.CreateInstance<BuffDef>();
            buffDefinition.name = Name;
            buffDefinition.iconSprite = CustomResources.ReviveLinkBuffIcon;
            buffDefinition.buffColor = Color.white;
            buffDefinition.isDebuff = true;
            buffDefinition.canStack = true;

            ContentAddition.AddBuffDef(buffDefinition);
        }

        protected override void OnBuffsAvailable() => Index = BuffCatalog.FindBuffIndex(Name);
    }
}