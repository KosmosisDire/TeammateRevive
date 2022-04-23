using System.Collections;
using System.Collections.Generic;
using R2API.Networking.Interfaces;
using RoR2;
using TeammateRevive.Logging;
using UnityEngine;
using UnityEngine.Networking;

namespace TeammateRevive.DeathTotem
{
    public class SyncDeathTotemMessage : INetMessage
    {
        public NetworkInstanceId totemId;
        public NetworkInstanceId deadPlayerId;
        public int insideCount;
        public readonly List<NetworkInstanceId> insideIDs = new();
        public float radius;
        private float fractionPerSecond;

        public SyncDeathTotemMessage() 
        {

        }

        public SyncDeathTotemMessage(NetworkInstanceId totemId, NetworkInstanceId deadPlayerId, List<NetworkInstanceId> insideIDs, float radius, float fractionPerSecond)
        {
            this.totemId = totemId;
            this.deadPlayerId = deadPlayerId;
            this.insideCount = insideIDs.Count;
            this.insideIDs = insideIDs;
            this.radius = radius;
            this.fractionPerSecond = fractionPerSecond;
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(this.totemId);
            writer.Write(this.deadPlayerId);
            writer.Write(this.insideCount);
            for (int i = 0; i < this.insideCount; i++)
            {
                writer.Write(this.insideIDs[i]);
            }
            writer.Write(this.radius);
            writer.Write(this.fractionPerSecond);
        }

        public void Deserialize(NetworkReader reader)
        {
            this.totemId = reader.ReadNetworkId();
            this.deadPlayerId = reader.ReadNetworkId();
            this.insideCount = reader.ReadInt32();
            this.insideIDs.Clear();
            for (int i = 0; i < this.insideCount; i++)
            {
                this.insideIDs.Add(reader.ReadNetworkId());
            }
            this.radius = reader.ReadSingle();
            this.fractionPerSecond = reader.ReadSingle();
        }

        public void OnReceived()
        {
            if (NetworkServer.active) return;
            DeathTotemBehavior totemComp = Util.FindNetworkObject(this.totemId)?.GetComponent<DeathTotemBehavior>();
            if (totemComp == null)
            {
                Log.Debug("Couldn't find totem " + this.totemId);
                MainTeammateRevival.instance.DoCoroutine(DelayedApply(totemComp));
                return;
            }

            Apply(totemComp);
        }

        private IEnumerator DelayedApply(DeathTotemBehavior totemComp)
        {
            yield return new WaitForSeconds(.3f);
            Apply(totemComp);
        }

        private void Apply(DeathTotemBehavior totemComp)
        {
            totemComp = totemComp ? totemComp : Util.FindNetworkObject(this.totemId)?.GetComponent<DeathTotemBehavior>();
            if (totemComp == null)
            {
                Log.Debug("Couldn't find totem after delay " + this.totemId);
                return;
            }
        
            totemComp.gameObject.SetActive(true);
            Log.DebugMethod($"Fraction: {this.fractionPerSecond}");
            if (totemComp) totemComp.SetValuesReceive(this.deadPlayerId, this.insideIDs, this.radius, this.fractionPerSecond);
        }
    }
}