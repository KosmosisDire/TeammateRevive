using System.Collections.Generic;
using RoR2;
using TeammateRevive.Common;
using TeammateRevive.Content;
using TeammateRevive.Logging;
using TeammateRevive.Revive.Rules;

namespace TeammateRevive
{
    public class ItemDropManager
    {
        private readonly RunTracker run;
        private readonly ReviveRules rules;

        public ItemDropManager(RunTracker run, ReviveRules rules)
        {
            this.run = run;
            this.rules = rules;

            On.RoR2.Run.BuildDropTable += OnBuildDropTable;
            On.RoR2.ItemCatalog.AllItemsEnumerator.MoveNext += OnNextItemIterator;
        }

        private bool OnNextItemIterator(On.RoR2.ItemCatalog.AllItemsEnumerator.orig_MoveNext orig, ref RoR2.ItemCatalog.AllItemsEnumerator self)
        {
            while (this.run.IsDeathCurseEnabled && (self.position == CharonsObol.Index - 1 || self.position == ReviveEverywhereItem.Index - 1))
            {
                self.position++;
            }

            return orig(ref self);
        }

        private void OnBuildDropTable(On.RoR2.Run.orig_BuildDropTable orig, RoR2.Run self)
        {
            orig(self);
            if (!ContentManager.ContentInited)
            {
                Log.Error($"Content wasn't inited on {nameof(OnBuildDropTable)}!");
                return;
            }
            
            var isDeathCurseEnabled = run.IsDeathCurseEnabled ||
                                      (NetworkHelper.IsServer && rules.Values.ForceDeathCurseRule);

            if (!isDeathCurseEnabled)
            {
                RemoveItem(CharonsObol.Index, self.availableTier2DropList, CharonsObol.Name);
                RemoveItem(DeadMansHandItem.Index, self.availableLunarItemDropList, DeadMansHandItem.Name);
                RemoveItem(DeadMansHandItem.Index, self.availableLunarCombinedDropList, DeadMansHandItem.Name);
            }
        }

        private void RemoveItem(ItemIndex itemIndex, List<PickupIndex> dropList, string name)
        {
            var respawnItemIdx =
                dropList.FindIndex(pi => pi.pickupDef.itemIndex == itemIndex);
            if (respawnItemIdx >= 0)
            {
                Log.Info($"Removing '{name}' from drop list");
                dropList.RemoveAt(respawnItemIdx);
            }
            else
            {
                Log.Info($"Item '{name}' isn't found in drop list!");
            }
        }
    }
}