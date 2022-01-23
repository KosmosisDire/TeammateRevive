using System.Collections.Generic;
using RoR2;
using TeammateRevive.Common;
using TeammateRevive.Content;
using TeammateRevive.Logging;
using TeammateRevive.Revive.Rules;
using Run = On.RoR2.Run;

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
        }

        private void OnBuildDropTable(Run.orig_BuildDropTable orig, RoR2.Run self)
        {
            orig(self);
            if (!ContentBase.ContentInited)
            {
                Log.Error($"Content wasn't inited on {nameof(OnBuildDropTable)}!");
                return;
            }
            
            var isDeathCurseEnabled = this.run.IsDeathCurseEnabled ||
                                      (NetworkHelper.IsServer && this.rules.Values.ForceDeathCurseRule);

            if (!isDeathCurseEnabled)
            {
                RemoveItem(CharonsObol.Index, self.availableTier2DropList, CharonsObol.Name);
                RemoveItem(ReviveEverywhereItem.Index, self.availableLunarDropList, ReviveEverywhereItem.Name);
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