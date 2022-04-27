using R2API;
using RoR2;
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
            var description = "Reduces chance to receive curse";
            ItemAPI.Add(DeprecatedCustomItem.Create(Name, "Revival Token",
                description, description, description,
                CustomResources.ReviveLinkBuffIcon,
                CustomResources.CharonsObolItemPrefab, ItemTier.NoTier, new[]
                {
                    ItemTag.AIBlacklist
                }, true, true));
        }

        public override void OnItemsAvailable() => Index = ItemCatalog.FindItemIndex(Name);
    }
}