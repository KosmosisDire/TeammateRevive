using System.Collections;
using System.Collections.Generic;
using R2API.Networking.Interfaces;
using RoR2;
using TeammateRevive.Logging;
using UnityEngine;
using UnityEngine.Networking;

namespace TeammateRevive.Skull
{
    public class SyncSkullMessage : INetMessage
    {
        public NetworkInstanceId skullId;
        public NetworkInstanceId deadPlayerId;
        public int insideCount;
        public readonly List<NetworkInstanceId> insideIDs = new();
        public float amount;
        public float radius;
        private float fractionPerSecond;

        public SyncSkullMessage() 
        {

        }

        public SyncSkullMessage(NetworkInstanceId skullId, NetworkInstanceId deadPlayerId, List<NetworkInstanceId> insideIDs, float amount, float radius, float fractionPerSecond)
        {
            this.skullId = skullId;
            this.deadPlayerId = deadPlayerId;
            this.insideCount = insideIDs.Count;
            this.insideIDs = insideIDs;
            this.amount = amount;
            this.radius = radius;
            this.fractionPerSecond = fractionPerSecond;
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(this.skullId);
            writer.Write(this.deadPlayerId);
            writer.Write(this.insideCount);
            for (int i = 0; i < this.insideCount; i++)
            {
                writer.Write(this.insideIDs[i]);
            }
            writer.Write(this.amount);
            writer.Write(this.radius);
            writer.Write(this.fractionPerSecond);
        }

        public void Deserialize(NetworkReader reader)
        {
            this.skullId = reader.ReadNetworkId();
            this.deadPlayerId = reader.ReadNetworkId();
            this.insideCount = reader.ReadInt32();
            this.insideIDs.Clear();
            for (int i = 0; i < this.insideCount; i++)
            {
                this.insideIDs.Add(reader.ReadNetworkId());
            }
            this.amount = reader.ReadSingle();
            this.radius = reader.ReadSingle();
            this.fractionPerSecond = reader.ReadSingle();
        }

        public void OnReceived()
        {
            if (NetworkServer.active) return;
            DeadPlayerSkull skullComp = Util.FindNetworkObject(this.skullId)?.GetComponent<DeadPlayerSkull>();
            if (skullComp == null)
            {
                Log.Debug("Couldn't find skull " + this.skullId);
                MainTeammateRevival.instance.DoCoroutine(DelayedApply(skullComp));
                return;
            }

            Apply(skullComp);
        }

        private IEnumerator DelayedApply(DeadPlayerSkull skullComp)
        {
            yield return new WaitForSeconds(.3f);
            Apply(skullComp);
        }

        private void Apply(DeadPlayerSkull skullComp)
        {
            skullComp = skullComp ? skullComp : Util.FindNetworkObject(this.skullId)?.GetComponent<DeadPlayerSkull>();
            if (skullComp == null)
            {
                Log.Debug("Couldn't find skull after delay " + this.skullId);
                return;
            }
        
            skullComp.gameObject.SetActive(true);
            Log.DebugMethod($"Fraction: {this.fractionPerSecond}");
            if (skullComp) skullComp.SetValuesReceive(this.deadPlayerId, this.amount, this.insideIDs, this.radius, this.fractionPerSecond);
        }
    }
}