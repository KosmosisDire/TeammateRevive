using R2API;
using RoR2;
using TeammateRevive.Resources;
using UnityEngine;
using static TeammateRevive.Common.TextFormatter;

namespace TeammateRevive.Content
{
    public class CharonsObol : ContentBase
    {
        public static string Name = "ITEM_CharonsObol";
        public static string NameToken = "Charon's Obol";
        public static ItemIndex Index;

        public override void Init()
        {
            var full =
                $"- Reduces {Yellow("revival time")}. " +
                $"\n- Can be consumed for {Yellow("instant")} revival without Death Curse. " +
                $"\n- {Yellow("Removes")} additional {Red("Death Curse")} on stage change." +
                $"\n- Increase {Yellow("range")} and decrease {Yellow("damage")} for your revival.";
            
            var generalScale = new Vector3(0.05f, 0.05f, 0.05f);
            var rules = new ItemDisplayRuleDict(new ItemDisplayRule[]
            {
                new()
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = CustomResources.CharonsObolItemPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(-0.22f, 0f, 0f),
                    localAngles = new Vector3(0f, -0.05f, 0f),
                    localScale = generalScale
                }
            });
            
            ItemAPI.Add(DeprecatedCustomItem.Create(Name, NameToken, 
                full, 
                full,
                "Makes revival easier. Can be consumed for instant revival.", 
                CustomResources.CharonsObolItemIcon,
                CustomResources.CharonsObolItemPrefab, ItemTier.Tier2, new[]
                {
                    ItemTag.Healing,
                    ItemTag.AIBlacklist
                }, 
                true, false, itemDisplayRules: rules));
        }

        protected override void OnItemsAvailable() => Index = ItemCatalog.FindItemIndex(Name);
    }
}