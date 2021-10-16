using System.Collections;
using System.Collections.Generic;
using R2API.Networking.Interfaces;
using RoR2;
using TeammateRevival;
using TeammateRevival.Logging;
using UnityEngine;
using UnityEngine.Networking;

public class SyncSkull : INetMessage
{
    public NetworkInstanceId skull;
    public NetworkInstanceId deadPlayerId;
    public int insideCount;
    public List<NetworkInstanceId> insideIDs = new();
    public float amount;
    public Color color;
    public float intensity;
    public float radius;
    private float fractionPerSecond;
    private float progress;

    public SyncSkull() 
    {

    }

    public SyncSkull(NetworkInstanceId skull, NetworkInstanceId deadPlayerId, int insideCount, List<NetworkInstanceId> insideIDs, float amount, Color color, float intensity, float radius, float fractionPerSecond)
    {
        this.skull = skull;
        this.deadPlayerId = deadPlayerId;
        this.insideCount = insideCount;
        this.insideIDs = insideIDs;
        this.amount = amount;
        this.color = color;
        this.intensity = intensity;
        this.radius = radius;
        this.fractionPerSecond = fractionPerSecond;
    }

    public void Deserialize(NetworkReader reader)
    {
        this.skull = reader.ReadNetworkId();
        this.deadPlayerId = reader.ReadNetworkId();
        this.insideCount = reader.ReadInt32();
        this.insideIDs.Clear();
        for (int i = 0; i < this.insideCount; i++)
        {
            this.insideIDs.Add(reader.ReadNetworkId());
        }
        this.amount = reader.ReadSingle();
        this.color = reader.ReadColor();
        this.intensity = reader.ReadSingle();
        this.radius = reader.ReadSingle();
        this.fractionPerSecond = reader.ReadSingle();
    }

    public void OnReceived()
    {
        if (NetworkServer.active) return;
        DeadPlayerSkull skullComp = Util.FindNetworkObject(this.skull)?.GetComponent<DeadPlayerSkull>();
        if (skullComp == null)
        {
            Log.Debug("Couldn't find skull " + this.skull);
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
        skullComp = skullComp ? skullComp : Util.FindNetworkObject(this.skull)?.GetComponent<DeadPlayerSkull>();
        if (skullComp == null)
        {
            Log.Debug("Couldn't find skull after delay " + this.skull);
            return;
        }
        
        skullComp.gameObject.SetActive(true);
        Log.DebugMethod($"Fraction: {this.fractionPerSecond}");
        if (skullComp) skullComp.SetValuesReceive(this.amount, this.color, this.intensity, this.insideIDs, this.radius, this.fractionPerSecond);
    }

    public void Serialize(NetworkWriter writer)
    {
        writer.Write(this.skull);
        writer.Write(this.deadPlayerId);
        writer.Write(this.insideCount);
        for (int i = 0; i < this.insideCount; i++)
        {
            writer.Write(this.insideIDs[i]);
        }
        writer.Write(this.amount);
        writer.Write(this.color);
        writer.Write(this.intensity);
        writer.Write(this.radius);
        writer.Write(this.fractionPerSecond);
    }
}