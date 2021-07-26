using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using System.Collections.Generic;
using TeammateRevival;
using UnityEngine;
using UnityEngine.Networking;

public class SyncSkull : INetMessage
{
    public NetworkInstanceId skull;
    public int insideCount;
    public List<NetworkInstanceId> insideIDs = new List<NetworkInstanceId>();
    public float amount;
    public Color color;
    public float intensity;

    public SyncSkull() 
    {

    }

    public SyncSkull(NetworkInstanceId skull, int insideCount, List<NetworkInstanceId> insideIDs, float amount, Color color, float intensity)
    {
        this.skull = skull;
        this.insideCount = insideCount;
        this.insideIDs = insideIDs;
        this.amount = amount;
        this.color = color;
        this.intensity = intensity;
    }

    public void Deserialize(NetworkReader reader)
    {
        skull = reader.ReadNetworkId();
        insideCount = reader.ReadInt32();
        insideIDs.Clear();
        for (int i = 0; i < insideCount; i++)
        {
            insideIDs.Add(reader.ReadNetworkId());
        }
        amount = reader.ReadSingle();
        color = reader.ReadColor();
        intensity = reader.ReadSingle();
    }

    public void OnReceived()
    {
        if (NetworkServer.active) return;
        DeadPlayerSkull skullComp = Util.FindNetworkObject(skull).GetComponent<DeadPlayerSkull>();
        if (skullComp) skullComp.SetValuesReceive(amount, color, intensity, insideIDs);
    }

    public void Serialize(NetworkWriter writer)
    {
        writer.Write(skull);
        writer.Write(insideCount);
        for (int i = 0; i < insideCount; i++)
        {
            writer.Write(insideIDs[i]);
        }
        writer.Write(amount);
        writer.Write(color);
        writer.Write(intensity);
    }
}

public class DeadPlayerSkull : MonoBehaviour
{
    public float amount = 1;
    public Color color = Color.red;
    public float intensity = 1;
    public List<NetworkInstanceId> insidePlayerIDs = new List<NetworkInstanceId>();
    public float lastSyncTime = 0;

    public void SetValuesReceive(float _amount, Color _color, float _intensity, List<NetworkInstanceId> _insidePlayerIDs)
    {
        amount = _amount;
        color = _color;
        intensity = _intensity;
        insidePlayerIDs = _insidePlayerIDs;
        lastSyncTime = Time.realtimeSinceStartup;
    }

    public void SetValuesSend(float _amount, Color _color, float _intensity)
    {
        if (amount == _amount && color == _color && intensity == _intensity) return;

        amount = _amount;
        color = _color;
        intensity = _intensity;

        SyncToClients();
    }

    public void RemoveDeadIDs()
    {
        for (int i = 0; i < insidePlayerIDs.Count; i++)
        {
            NetworkInstanceId ID = insidePlayerIDs[i];
            Player p = MainTeammateRevival.FindPlayerFromBodyInstanceID(ID);
            if (p != null)
            {
                if (p.CheckDead()) 
                {
                    insidePlayerIDs.RemoveAt(i);
                    i--;
                }
            }
        }
    }

    public void SyncToClients() 
    {
        float timeSinceLastSync = Time.realtimeSinceStartup - lastSyncTime;
        if (NetworkServer.active && timeSinceLastSync > 0.08f)
        {
            RemoveDeadIDs();
            new SyncSkull(GetComponent<NetworkIdentity>().netId, insidePlayerIDs.Count, insidePlayerIDs, amount, color, intensity).Send(NetworkDestination.Clients);
            lastSyncTime = Time.realtimeSinceStartup;
        }
    }

    void Update()
    {
        if (DamageNumberManager.instance == null) return;
        SetLighting();
        DamageNumbers();
    }

    void SetLighting()
    {
        transform.GetChild(0).GetComponentInChildren<Light>(false).color = color;
        transform.GetChild(0).GetComponentInChildren<Light>(false).intensity = intensity;
    }

    void DamageNumbers()
    {
        if (insidePlayerIDs.Count > 0)
        {
            foreach (var playerID in insidePlayerIDs)
            {
                GameObject player = Util.FindNetworkObject(playerID);
                
                if (!player)
                {
                    continue;
                }

                if (Random.Range(0f, 100f) < 10f)
                    DamageNumberManager.instance.SpawnDamageNumber(amount * 10 + Random.Range(-1, 2), player.transform.position + Vector3.up * 0.7f, false, TeamIndex.Player, DamageColorIndex.Bleed);
            }

            if (Random.Range(0f, 100f) < 10f)
                DamageNumberManager.instance.SpawnDamageNumber(amount * 10 + Random.Range(-1, 2), transform.position, false, TeamIndex.Player, DamageColorIndex.Heal);
        }
    }
}
