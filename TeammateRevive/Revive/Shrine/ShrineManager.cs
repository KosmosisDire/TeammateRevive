
using RoR2;
using TeammateRevive.Logging;
using TeammateRevive.Resources;
using TeammateRevive.Revive.Rules;
using UnityEngine;

namespace TeammateRevive.Revive.Shrine
{
    public class ShrineManager
    {
        private readonly RunTracker runTracker;
        private readonly ReviveRules rules;

        private SpawnCard shrineCard;

        public ShrineManager(RunTracker runTracker, ReviveRules rules)
        {
            this.runTracker = runTracker;
            this.rules = rules;
            On.RoR2.SceneDirector.PopulateScene += hook_PopulateScene;
        }

        private void hook_PopulateScene(On.RoR2.SceneDirector.orig_PopulateScene orig, SceneDirector self)
        {
            // spawns 0, 1 or 3 shrines per stage
            const int threshold = 1;
            
            orig(self);
            
            if (self.name == "bazaar" || !this.runTracker.IsDeathCurseEnabled || !this.rules.Values.ShawnCharonShrine) return;
            
            this.shrineCard ??= CreateSpawnCard();
            var rndValue = self.rng.RangeInt(0, 3);
            Log.DebugMethod(rndValue);

            if (rndValue <= threshold) return;
            
            for (var i = threshold; i < rndValue; i++)
            {
                var placementRule = new DirectorPlacementRule
                {
                    placementMode = DirectorPlacementRule.PlacementMode.Random
                };
                var directorSpawnRequest = new DirectorSpawnRequest(this.shrineCard, placementRule, self.rng);
                DirectorCore.instance.TrySpawnObject(directorSpawnRequest);
                Log.Debug("Charon's shrine spawned!");
            }
        }

        private static InteractableSpawnCard CreateSpawnCard()
        {
            var interactableSpawnCard = ScriptableObject.CreateInstance<InteractableSpawnCard>();
            interactableSpawnCard.prefab = AddedAssets.ShrinePrefab;
            interactableSpawnCard.name = "Charon's shrine";
            interactableSpawnCard.sendOverNetwork = true;
            interactableSpawnCard.hullSize = HullClassification.Golem;
            interactableSpawnCard.orientToFloor = true;
            interactableSpawnCard.occupyPosition = true;
            
            return interactableSpawnCard;
        }
    }
}