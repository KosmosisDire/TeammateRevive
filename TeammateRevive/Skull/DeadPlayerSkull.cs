using System;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using TeammateRevival;
using TeammateRevival.Logging;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

public class DeadPlayerSkull : NetworkBehaviour
{
    public static Action<DeadPlayerSkull> GlobalOnValuesReceived;
    public static Action<DeadPlayerSkull> GlobalOnCreated;
    public static Action<DeadPlayerSkull> GlobalOnDestroy;
    
    public SkullData Data = new()
    {
        Amount = 0,
        Color = Color.red,
        Intensity = 1
    };
    
    public float Amount => this.Data.Amount;
    public Color color => this.Data.Color;
    public float intensity => this.Data.Intensity;
    
    public List<NetworkInstanceId> insidePlayerIDs = new();
    public float lastSyncTime = 0;
    public MeshRenderer radiusSphere;
    public NetworkInstanceId deadPlayerId;

    public int GetInsidePlayersHash()
    {
        unchecked
        {
            int hash = 17;
            foreach (var id in this.insidePlayerIDs)
            {
                hash = hash * 31 + id.GetHashCode();
            }
            return hash;
        }
    }

    private string cachedUserName;

    public string PlayerName
    {
        get
        {
            if (this.cachedUserName != null) return this.cachedUserName;
            this.cachedUserName = NetworkUser.readOnlyInstancesList.FirstOrDefault(p => p.netId == this.deadPlayerId)?.userName;
            return this.cachedUserName;
        }
    }

    public NetworkInstanceId NetworkInstanceId => GetComponent<NetworkIdentity>().netId;
    
    /// <summary>
    /// Used only for clients to estimate revive progress.
    /// </summary>
    public float progress;

    private Light lighting;

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
        // NetworkInstanceId = GetComponent<NetworkIdentity>().netId;
        lighting = this.transform.GetChild(0).GetComponentInChildren<Light>(false);
        radiusSphere = transform.Find("Radius Indicator").GetComponent<MeshRenderer>();
        if(!NetworkServer.active)
            gameObject.SetActive(false);
        GlobalOnCreated?.Invoke(this);
    }

    public void SetValuesReceive(float _amount, Color _color, float _intensity, List<NetworkInstanceId> _insidePlayerIDs, float scale, float fractionPerSecond)
    {
        this.Data.Amount = _amount;
        this.Data.Intensity = _intensity;
        this.Data.Color = _color;
        this.Data.FractionPerSecond = fractionPerSecond;
        insidePlayerIDs = _insidePlayerIDs;
        lastSyncTime = Time.realtimeSinceStartup;
        radiusSphere.transform.localScale = Vector3.one * scale;
        GlobalOnValuesReceived?.Invoke(this);
    }

    // TODO: remove
    public void SetValuesSend(float _amount, Color _color, float _intensity)
    {
        if (this.Amount == _amount && color == _color && intensity == _intensity) return;

        this.Data.Amount = _amount;
        this.Data.Intensity = _intensity;
        this.Data.Color = _color;

        SyncToClients();
    }
    
    public void SetValuesSend(SkullData data, bool forceUpdate = false)
    {
        if (!forceUpdate && this.Data.Equals(data)) return;
        this.Data = data;
        // radiusSphere.transform.localScale = Vector3.one * this.insidePlayerIDs.Count;
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
            new SyncSkull(this.NetworkInstanceId, this.deadPlayerId, insidePlayerIDs.Count, insidePlayerIDs, Amount, color, intensity, radiusSphere.transform.localScale.x, Data.FractionPerSecond).Send(NetworkDestination.Clients);
            lastSyncTime = Time.realtimeSinceStartup;
            Log.Debug("Rad: " + radiusSphere.transform.localScale.x);
        }
    }

    void Update()
    {
        if (MainTeammateRevival.IsClient())
            this.progress += Mathf.Clamp01(Time.deltaTime * this.Data.FractionPerSecond);

        if (DamageNumberManager.instance == null) return;
        SetLighting();
        DamageNumbers();
    }

    void OnDestroy()
    {
        Log.DebugMethod();
        GlobalOnDestroy?.Invoke(this);
    }

    void SetLighting()
    {
        // TODO: cleanup color/intensity
        var p = Mathf.Clamp01(this.progress);
        this.lighting.color = new Color(1 - p, p, 0.6f * p);
        this.lighting.intensity = 4 + 15 * p;
    }

    void DamageNumbers()
    {
        if (this.progress >= 1) return;
        
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
                    DamageNumberManager.instance.SpawnDamageNumber(this.Amount * 10 + Random.Range(-1, 2), player.transform.position + Vector3.up * 0.7f, false, TeamIndex.Player, DamageColorIndex.Bleed);
            }

            if (Random.Range(0f, 100f) < 10f)
                DamageNumberManager.instance.SpawnDamageNumber(this.Amount * 10 + Random.Range(-1, 2), transform.position, false, TeamIndex.Player, DamageColorIndex.Heal);
        }
    }
}
