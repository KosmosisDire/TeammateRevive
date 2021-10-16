using System.Collections.Generic;
using System.Linq;
using TeammateRevival.Logging;
using UnityEngine.Networking;

namespace TeammateRevive.RevivalStrategies.ReduceMaxHp
{
    public class ClientResurrectionTracker
    {
        private readonly HashSet<DeadPlayerSkull> skulls = new();

        public ClientResurrectionTracker()
        {
            DeadPlayerSkull.GlobalOnDestroy += OnSkullDestroy;
            DeadPlayerSkull.GlobalOnCreated += OnSkullDestroy;
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

        public DeadPlayerSkull GetResurrectingSkull(NetworkInstanceId userBodyId)
        {
            return this.skulls.FirstOrDefault(s => s.insidePlayerIDs.Contains(userBodyId));
        }
    }
}