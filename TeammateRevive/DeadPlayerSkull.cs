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
        Util.FindNetworkObject(skull).GetComponent<DeadPlayerSkull>().SetValues(amount, color, intensity, insideIDs);
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
    public float amount;
    public Color color;
    public float intensity;
    public List<NetworkInstanceId> insidePlayerIDs = new List<NetworkInstanceId>();


    public void SetValues(float _amount, Color _color, float _intensity, List<NetworkInstanceId> _insidePlayerIDs)
    {
        amount = _amount;
        color = _color;
        intensity = _intensity;
        insidePlayerIDs = _insidePlayerIDs;
    }
    public void SetValues(float _amount, Color _color, float _intensity)
    {
        amount = _amount;
        color = _color;
        intensity = _intensity;

        if (NetworkServer.active)
        {
            new SyncSkull(GetComponent<NetworkIdentity>().netId, insidePlayerIDs.Count, insidePlayerIDs, amount, color, intensity);
        }
    }

    void Start()
    {
        color = new Color(1, 0, 0, 1);
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
                    TeammateRevive.LogError("Inside Player is NULL!");
                    return;
                }

                if (Random.Range(0f, 100f) < 10f)
                    DamageNumberManager.instance.SpawnDamageNumber(amount * 10 + Random.Range(-1, 2), player.transform.position + Vector3.up * 0.7f, false, TeamIndex.Player, DamageColorIndex.Bleed);
            }

            if (Random.Range(0f, 100f) < 10f)
                DamageNumberManager.instance.SpawnDamageNumber(amount * 10 + Random.Range(-1, 2), transform.position, false, TeamIndex.Player, DamageColorIndex.Heal);
        }
    }
}
