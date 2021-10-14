using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using System.Collections.Generic;
using TeammateRevival;
using TeammateRevival.Logging;
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
    public float radius;

    public SyncSkull() 
    {

    }

    public SyncSkull(NetworkInstanceId skull, int insideCount, List<NetworkInstanceId> insideIDs, float amount, Color color, float intensity, float radius)
    {
        this.skull = skull;
        this.insideCount = insideCount;
        this.insideIDs = insideIDs;
        this.amount = amount;
        this.color = color;
        this.intensity = intensity;
        this.radius = radius;
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
        radius = reader.ReadSingle();
    }

    public void OnReceived()
    {
        if (NetworkServer.active) return;
        DeadPlayerSkull skullComp = Util.FindNetworkObject(skull).GetComponent<DeadPlayerSkull>();
        skullComp.gameObject.SetActive(true);
        if (skullComp) skullComp.SetValuesReceive(amount, color, intensity, insideIDs, radius);
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
        writer.Write(radius);
    }
}

public class DeadPlayerSkull : NetworkBehaviour
{
    public float amount = 1;
    public Color color = Color.red;
    public float intensity = 1;
    public List<NetworkInstanceId> insidePlayerIDs = new List<NetworkInstanceId>();
    public float lastSyncTime = 0;
    public MeshRenderer radiusSphere;

    public override void OnStartClient()
    {
        base.OnStartClient();
        MainTeammateRevival.instance.RevivalStrategy.OnClientSkullSpawned(this);
    }

    void Awake()
    {
        Setup();
    }

    public void Setup()
    {
        radiusSphere = transform.Find("Radius Indicator").GetComponent<MeshRenderer>();
        if(!NetworkServer.active)
            gameObject.SetActive(false);
    }

    public void SetValuesReceive(float _amount, Color _color, float _intensity, List<NetworkInstanceId> _insidePlayerIDs, float scale)
    {
        
        amount = _amount;
        color = _color;
        intensity = _intensity;
        insidePlayerIDs = _insidePlayerIDs;
        lastSyncTime = Time.realtimeSinceStartup;
        radiusSphere.transform.localScale = Vector3.one * scale;
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
            Player p = MainTeammateRevival.instance.FindPlayerFromBodyInstanceID(ID);
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
        if (NetworkServer.active && timeSinceLastSync > 0.05f)
        {
            RemoveDeadIDs();
            new SyncSkull(GetComponent<NetworkIdentity>().netId, insidePlayerIDs.Count, insidePlayerIDs, amount, color, intensity, radiusSphere.transform.localScale.x).Send(NetworkDestination.Clients);
            lastSyncTime = Time.realtimeSinceStartup;
            Log.Info("Rad: " + radiusSphere.transform.localScale.x);
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
