using System;
using System.Collections.Generic;
using System.Linq;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using TeammateRevive.Logging;
using TeammateRevive.Players;
using TeammateRevive.Revive;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

namespace TeammateRevive.Skull
{
    public class DeadPlayerSkull : NetworkBehaviour
    {
        // events
        public static Action<DeadPlayerSkull> GlobalOnValuesReceived;
        public static Action<DeadPlayerSkull> GlobalOnCreated;
        public static Action<DeadPlayerSkull> GlobalOnDestroy;

        // syncable state
        public float damageAmount;
        public float progress;
        public float fractionPerSecond;
        public List<NetworkInstanceId> insidePlayerIDs = new();
    
        // state
        public MeshRenderer radiusSphere;
        public NetworkInstanceId deadPlayerId;
        private Light lighting;

        private float cachedRadius;
    
    
        public NetworkInstanceId NetworkInstanceId => GetComponent<NetworkIdentity>().netId;
    
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
    
    
        void Awake()
        {
            Setup();
        }

        void Update()
        {
            if (MainTeammateRevival.IsClient())
                this.progress = Mathf.Clamp01(this.progress + Time.deltaTime * this.fractionPerSecond);

            if (DamageNumberManager.instance == null) return;
            SetLighting();
            DamageNumbers();
        }

        void OnDestroy()
        {
            Log.DebugMethod();
            GlobalOnDestroy?.Invoke(this);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            RevivalTracker.instance.OnClientSkullSpawned(this);
        }

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


        public void Setup()
        {
            // NetworkInstanceId = GetComponent<NetworkIdentity>().netId;
            this.lighting = this.transform.GetChild(0).GetComponentInChildren<Light>(false);
            this.radiusSphere = this.transform.Find("Radius Indicator").GetComponent<MeshRenderer>();
            if(!NetworkServer.active)
                this.gameObject.SetActive(false);
            GlobalOnCreated?.Invoke(this);
        }

        public void SetValuesReceive(NetworkInstanceId deadPlayerId, float amount,
            List<NetworkInstanceId> _insidePlayerIDs, float scale, float fractionPerSecond)
        {
            Log.Debug($"Received skull values. Rad: {scale}");
            this.deadPlayerId = deadPlayerId;
            this.damageAmount = amount;
            this.fractionPerSecond = fractionPerSecond;
            this.insidePlayerIDs = _insidePlayerIDs;
            this.cachedRadius = scale;
            this.radiusSphere.transform.localScale = Vector3.one * scale;
            GlobalOnValuesReceived?.Invoke(this);
        }

        public void SetValuesSend(float speed, float damageAmount, float radius, bool forceUpdate = false)
        {
            if (!forceUpdate
                && Mathf.Approximately(speed, this.fractionPerSecond)
                && Mathf.Approximately(damageAmount, this.damageAmount)
                && Mathf.Approximately(radius, this.cachedRadius)
            )
            {
                return;
            }
        
            this.fractionPerSecond = speed;
            this.damageAmount = damageAmount;
            this.cachedRadius = radius;
            this.radiusSphere.transform.localScale = Vector3.one * radius;
        
            SyncToClients();
        }

        public void RemoveDeadIDs()
        {
            for (int i = 0; i < this.insidePlayerIDs.Count; i++)
            {
                var id = this.insidePlayerIDs[i];
                var p = PlayersTracker.instance.FindByBodyId(id);
                if (p != null)
                {
                    if (p.CheckDead()) 
                    {
                        this.insidePlayerIDs.RemoveAt(i);
                        i--;
                    }
                }
            }
        }

        public void SyncToClients() 
        {
            Log.Debug($"SyncToClients. Rad: {this.cachedRadius}");
            RemoveDeadIDs();
            new SyncSkull(this.NetworkInstanceId, this.deadPlayerId, this.insidePlayerIDs, this.damageAmount, this.cachedRadius, this.fractionPerSecond).Send(NetworkDestination.Clients);
            Log.Debug("Rad: " + this.radiusSphere.transform.localScale.x);
        }

        void SetLighting()
        {
            var p = Mathf.Clamp01(this.progress);
            this.lighting.color = new Color(1 - p, p, 0.6f * p);
            this.lighting.intensity = 4 + 15 * p;
        }

        void DamageNumbers()
        {
            if (this.progress >= 1) return;
        
            if (this.insidePlayerIDs.Count > 0)
            {
                foreach (var playerID in this.insidePlayerIDs)
                {
                    GameObject player = Util.FindNetworkObject(playerID);
                
                    if (!player)
                    {
                        continue;
                    }

                    if (Random.Range(0f, 100f) < 10f)
                        DamageNumberManager.instance.SpawnDamageNumber(this.damageAmount * 10 + Random.Range(-1, 2), player.transform.position + Vector3.up * 0.7f, false, TeamIndex.Player, DamageColorIndex.Bleed);
                }

                if (Random.Range(0f, 100f) < 10f)
                    DamageNumberManager.instance.SpawnDamageNumber(this.damageAmount * 10 + Random.Range(-1, 2), this.transform.position, false, TeamIndex.Player, DamageColorIndex.Heal);
            }
        }
    }
}
