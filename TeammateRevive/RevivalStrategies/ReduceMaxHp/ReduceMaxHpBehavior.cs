using RoR2;
using TeammateRevival.Logging;
using UnityEngine;

namespace TeammateRevive.RevivalStrategies.ReduceMaxHp
{
    public class ReduceMaxHpBehavior : MonoBehaviour
    {
        private CharacterBody body;

        void Awake()
        {
            this.body = this.gameObject.GetComponent<CharacterBody>();
            Log.DebugMethod("created ReduceHpBehavior for " + this.body.netId + " " + this.body.name);
        }

        public void ReduceHalf()
        {
            // TODO: network message
            Log.DebugMethod();
            Log.DebugMethod("[server] Adding item to " + this.body.name);
            this.body.inventory.GiveItem(AddedResources.ReduceHpItemIndex);
            this.body.RecalculateStats();
            // TODO: damage numbers
        }
    }
}