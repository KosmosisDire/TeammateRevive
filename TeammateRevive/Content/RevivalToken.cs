using R2API;
using RoR2;
using TeammateRevive.Localization;
using TeammateRevive.Resources;
using UnityEngine;
using CharacterBody = On.RoR2.CharacterBody;

namespace TeammateRevive.Content
{
    public class RevivalToken : ContentBase
    {
        public static string Name = "ITEM_RevivalToken";
        public static ItemIndex Index;

        public override void Init()
        {
            ItemAPI.Add(DeprecatedCustomItem.Create(Name, 
                LanguageConsts.ITEM_REVIVAL_TOKEN_NAME,
                LanguageConsts.ITEM_REVIVAL_TOKEN_DESCRIPTION, 
                LanguageConsts.ITEM_REVIVAL_TOKEN_DESCRIPTION, 
                LanguageConsts.ITEM_REVIVAL_TOKEN_DESCRIPTION,
                CustomResources.ReviveLinkBuffIcon,
                CustomResources.CharonsObolItemPrefab, ItemTier.NoTier, new[]
                {
                    ItemTag.AIBlacklist
                }, true, true));
        }

        public override void OnItemsAvailable() => Index = ItemCatalog.FindItemIndex(Name);
    }
}