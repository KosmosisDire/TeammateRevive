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
        Util.FindNetworkObject(skull).GetComponent<DeadPlayerSkull>().SetValues(amount, color, intensity, insideIDs);
        TeammateRevival.MainTeammateRevival.LogInfo("Received Message!");
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


    public void SetValues(float _amount, Color _color, float _intensity, List<NetworkInstanceId> _insidePlayerIDs)
    {
        amount = _amount;
        color = _color;
        intensity = _intensity;
        insidePlayerIDs = _insidePlayerIDs;
    }

    public void RemoveDeadIDs() 
    {
        for (int i = 0; i < insidePlayerIDs.Count; i++)
        {
            NetworkInstanceId ID = insidePlayerIDs[i];
            Player p = TeammateRevival.MainTeammateRevival.FindPlayerFromBodyInstanceID(ID);
            if (p != null)
            {
                if (p.isDead) 
                {
                    insidePlayerIDs.RemoveAt(i);
                    i--;
                }
            }
        }
    }

    public void SyncToClients() 
    {
        if (NetworkServer.active)
        {
            RemoveDeadIDs();
            new SyncSkull(GetComponent<NetworkIdentity>().netId, insidePlayerIDs.Count, insidePlayerIDs, amount, color, intensity).Send(NetworkDestination.Clients);
            TeammateRevival.MainTeammateRevival.LogInfo("Sent Message to Client");
        }
    }

    public void SetValues(float _amount, Color _color, float _intensity)
    {
        amount = _amount;
        color = _color;
        intensity = _intensity;

        SyncToClients();
    }

    void Update()
    {
        //if (NetworkServer.active)
        //{
        //    foreach (var player in PlayerCharacterMasterController.instances)
        //    {
        //        if (!player.master.GetBody()) continue;
        //        if (Vector3.Distance(player.master.GetBody().transform.position, transform.position) < 4)
        //        {
        //            player.body = player.master.GetBody();
        //            color = Color.green;
        //            amount = 8;
        //            intensity += Time.deltaTime * 1;
        //            if (!insidePlayerIDs.Contains(player.body.netId))
        //            {
        //                insidePlayerIDs.Add(player.body.netId);
        //                SyncToClients();
        //            }
        //        }
        //        else
        //        {
        //            color = Color.red;
        //            intensity -= Time.deltaTime * 0.3f;
        //            if (insidePlayerIDs.Contains(player.body.netId))
        //            {
        //                insidePlayerIDs.Remove(player.body.netId);
        //                SyncToClients();
        //            }
        //        }
        //    }
        //}

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
                    TeammateRevival.MainTeammateRevival.LogWarning("Inside Player is NULL! Skipping");
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
