using R2API;
using RoR2;
using TeammateRevive.Localization;
using TeammateRevive.Resources;

namespace TeammateRevive.Content
{
    public class DeadMansHandItem : ContentBase
    {
        public static string Name = "ITEM_ReviveEverywhere";
        public static string NameToken = LanguageConsts.ITEM_REVIVE_EVERYWHERE_NAME;
        public static ItemIndex Index;
        
        public override void Init()
        {
            ItemAPI.Add(DeprecatedCustomItem.Create(Name, 
                NameToken, 
                LanguageConsts.ITEM_REVIVE_EVERYWHERE_DESCRIPTION, 
                LanguageConsts.ITEM_REVIVE_EVERYWHERE_LORE,
                LanguageConsts.ITEM_REVIVE_EVERYWHERE_PICKUP, 
                CustomResources.LunarHandIcon,
                CustomResources.HandItemPrefab, ItemTier.Lunar, new[]
                {
                    ItemTag.AIBlacklist
                }, 
                true, false));
        }

        public override void OnItemsAvailable() => Index = ItemCatalog.FindItemIndex(Name);
    }
}