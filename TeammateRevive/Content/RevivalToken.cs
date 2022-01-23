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
        
        public static string BuffName = "Revival Token";
        public static BuffIndex BuffIndex;

        public override void Init()
        {
            var description = "Reduces chance to receive curse";
            ItemAPI.Add(new CustomItem(Name, "Revival Token",
                description, description, description,
                AddedAssets.ReviveLinkBuffIcon,
                AddedAssets.CharonsObolItemPrefab, ItemTier.NoTier, new[]
                {
                    ItemTag.AIBlacklist
                }, true, true));
            BuffAPI.Add(new CustomBuff(BuffName, AddedAssets.DeathCurseBuffIcon, Color.green, false, true));
            On.RoR2.CharacterBody.RecalculateStats += OnCharacterBodyRecalculateStats;
        }

        private void OnCharacterBodyRecalculateStats(CharacterBody.orig_RecalculateStats orig, RoR2.CharacterBody self)
        {
            orig(self);
            if (self.inventory == null || self.inventory.GetItemCount(Index) == 0) return;
            var count = self.inventory.GetItemCount(Index);
            self.SetBuffCount(BuffIndex, count);
        }

        protected override void OnItemsAvailable() => Index = ItemCatalog.FindItemIndex(Name);
        protected override void OnBuffsAvailable() => BuffIndex = BuffCatalog.FindBuffIndex(BuffName);
    }
}