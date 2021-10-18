using System.Collections.Generic;
using System.Linq;
using TeammateRevive.Logging;
using TeammateRevive.Skull;
using UnityEngine;
using UnityEngine.Networking;

namespace TeammateRevive.Revive
{
    public class SkullTracker
    {
        private readonly HashSet<DeadPlayerSkull> skulls = new();

        public bool HasAnySkulls => this.skulls.Count > 0;

        public SkullTracker()
        {
            DeadPlayerSkull.GlobalOnDestroy += OnSkullDestroy;
            DeadPlayerSkull.GlobalOnCreated += OnSkullUpdate;
            DeadPlayerSkull.GlobalOnValuesReceived += OnSkullUpdate;
        }

        public void Clear()
        {
            this.skulls.Clear();
        }

        private void OnSkullUpdate(DeadPlayerSkull obj)
        {
            Log.Debug("Skull updated! " + string.Join(", ", obj.insidePlayerIDs.Select(i => i.ToString())));
            this.skulls.Add(obj);
        }

        private void OnSkullDestroy(DeadPlayerSkull obj)
        {
            Log.Debug("Skull destroyed! " + string.Join(", ", obj.insidePlayerIDs.Select(i => i.ToString())));
            this.skulls.Remove(obj);
        }

        public DeadPlayerSkull GetSkullInRange(NetworkInstanceId userBodyId)
        {
            var skull = this.skulls.FirstOrDefault(s => s.insidePlayerIDs.Contains(userBodyId));
            return skull;
        }
    }
}