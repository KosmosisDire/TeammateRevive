using System;
using System.Collections.Generic;
using System.Linq;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using TeammateRevive.Common;
using TeammateRevive.Content;
using TeammateRevive.Logging;
using TeammateRevive.Players;
using TeammateRevive.Resources;
using TeammateRevive.Revive.Rules;
using UnityEngine;
using UnityEngine.Networking;

namespace TeammateRevive.Skull
{
    public class DeadPlayerSkull : NetworkBehaviour
    {
        // events
        public static Action<DeadPlayerSkull> GlobalOnValuesReceived;
        public static Action<DeadPlayerSkull> GlobalOnCreated;
        public static Action<DeadPlayerSkull> GlobalOnClientCreated;
        public static Action<DeadPlayerSkull> GlobalOnDestroy;

        // dependencies
        private ReviveRules rules;

        // syncable state
        public float progress;
        public float fractionPerSecond;
        public List<NetworkInstanceId> insidePlayerIDs = new();
    
        // state
        public MeshRenderer radiusSphere;
        public NetworkInstanceId deadPlayerId;
        private Light lighting;
        private ScaleAnimation animation;
        public float cachedRadius;

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
            // interpolate revive progress
            if (NetworkHelper.IsClient())
                this.progress = Mathf.Clamp01(this.progress + Time.deltaTime * this.fractionPerSecond);

            if (DamageNumberManager.instance == null) return;
            this.animation.Update();
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
            GlobalOnClientCreated?.Invoke(this);
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
            this.lighting = this.transform.GetChild(0).GetComponentInChildren<Light>(false);
            this.radiusSphere = this.transform.Find("Radius Indicator").GetComponent<MeshRenderer>();
            if(!NetworkServer.active)
                this.gameObject.SetActive(false);
            this.animation = new ScaleAnimation(this.radiusSphere.transform, .2f);
            this.rules = ReviveRules.instance;
            GlobalOnCreated?.Invoke(this);
        }

        public void SetValuesReceive(NetworkInstanceId deadPlayerId,
            List<NetworkInstanceId> _insidePlayerIDs, float scale, float fractionPerSecond)
        {
            Log.Debug($"Received skull values. Rad: {scale}");
            this.deadPlayerId = deadPlayerId;
            this.fractionPerSecond = fractionPerSecond;
            this.insidePlayerIDs = _insidePlayerIDs;
            this.cachedRadius = scale;
            this.animation.AnimateTo(Vector3.one * scale);
            GlobalOnValuesReceived?.Invoke(this);
        }

        public void SetValuesSend(float speed, float radius, bool forceUpdate = false)
        {
            if (!forceUpdate
                && Mathf.Approximately(speed, this.fractionPerSecond)
                && Mathf.Approximately(radius, this.cachedRadius)
            )
            {
                return;
            }
        
            this.fractionPerSecond = speed;
            this.cachedRadius = radius;
            this.animation.AnimateTo(Vector3.one * radius);
        
            SyncToClients();
        }
        
        public void SetValues(float speed, float radius)
        {
            this.fractionPerSecond = speed;
            this.cachedRadius = radius;
            this.animation.AnimateTo(Vector3.one * radius);
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
            Log.DebugMethod($"Rad: {this.cachedRadius}");
            RemoveDeadIDs();
            new SyncSkullMessage(GetComponent<NetworkIdentity>().netId, this.deadPlayerId, this.insidePlayerIDs, this.cachedRadius, this.fractionPerSecond).Send(NetworkDestination.Clients);
            Log.Debug("Rad: " + this.radiusSphere.transform.localScale.x);
        }

        void SetLighting()
        {
            var p = Mathf.Clamp01(this.progress);
            this.lighting.color = new Color(1 - p, p, 0.6f * p);
            this.lighting.intensity = 4 + 15 * p;
        }

        private float damageNumberElapsed = 0;
        private float damageNumberRate = .2f;

        void DamageNumbers()
        {
            if (this.progress >= 1 || this.insidePlayerIDs.Count == 0) return;
            
            this.damageNumberElapsed += Time.deltaTime;
            if (this.damageNumberElapsed < damageNumberRate)
            {
                return;
            }

            foreach (var playerID in this.insidePlayerIDs)
            {
                var body = GetBody(playerID);
                if (!body)
                    continue;
                
                var deadPlayerObolsCount = body.master.inventory.GetItemCount(CharonsObol.Index);
                var reviverReviveEverywhereItemCount = body.master.inventory.GetItemCount(ReviveEverywhereItem.Index);
                var damageSpeed = this.rules.GetDamageSpeed(this.insidePlayerIDs.Count, body.maxHealth, deadPlayerObolsCount, reviverReviveEverywhereItemCount);

                DamageNumberManager.instance.SpawnDamageNumber(damageSpeed * this.damageNumberElapsed, body.transform.position + Vector3.up * 0.7f, false, TeamIndex.Player, DamageColorIndex.Bleed);
            }

            DamageNumberManager.instance.SpawnDamageNumber(this.fractionPerSecond * this.damageNumberElapsed * 100, this.transform.position, false, TeamIndex.Player, DamageColorIndex.Heal);
            this.damageNumberElapsed = 0;
        }

        CharacterBody GetBody(NetworkInstanceId playerID)
        {
            var player = Util.FindNetworkObject(playerID);
            if (!player)
                return null;
            return player.GetComponent<CharacterBody>();
        }
    }
}
