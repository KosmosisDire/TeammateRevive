using R2API;
using RoR2;
using TeammateRevive.Localization;
using TeammateRevive.Resources;
using UnityEngine;

namespace TeammateRevive.Content
{
    public class CharonsObol : ContentBase
    {
        public static string Name = "ITEM_CharonsObol";
        public static string NameToken = LanguageConsts.ITEM_CHARONS_OBOL_NAME;
        public static ItemIndex Index;

        public override void Init()
        {
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
            
            ItemAPI.Add(DeprecatedCustomItem.Create(Name, 
                NameToken, 
                LanguageConsts.ITEM_CHARONS_OBOL_DESCRIPTION, 
                LanguageConsts.ITEM_CHARONS_OBOL_DESCRIPTION,
                LanguageConsts.ITEM_CHARONS_OBOL_PICKUP, 
                CustomResources.CharonsObolItemIcon,
                CustomResources.CharonsObolItemPrefab, ItemTier.Tier2, new[]
                {
                    ItemTag.Healing,
                    ItemTag.AIBlacklist
                }, 
                true, false, itemDisplayRules: rules));
        }

        public override void OnItemsAvailable() => Index = ItemCatalog.FindItemIndex(Name);
    }
}