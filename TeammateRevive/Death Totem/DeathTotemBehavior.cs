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
using TeammateRevive.ProgressBar;
using TeammateRevive.Resources;
using TeammateRevive.Revive.Rules;
using UnityEngine;
using UnityEngine.Networking;

namespace TeammateRevive.DeathTotem
{
    public class DeathTotemBehavior : NetworkBehaviour
    {
        // events
        public static Action<DeathTotemBehavior> GlobalOnValuesReceived;
        public static Action<DeathTotemBehavior> GlobalOnCreated;
        public static Action<DeathTotemBehavior> GlobalOnClientCreated;
        public static Action<DeathTotemBehavior> GlobalOnDestroy;

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
                if (cachedUserName != null) return cachedUserName;
                cachedUserName = NetworkUser.readOnlyInstancesList.FirstOrDefault(p => p.netId == deadPlayerId)?.userName;
                return cachedUserName;
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
                progress = Mathf.Clamp01(progress + Time.deltaTime * fractionPerSecond);

            if (DamageNumberManager.instance == null) return;
            animation.Update();
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
                foreach (var id in insidePlayerIDs)
                {
                    hash = hash * 31 + id.GetHashCode();
                }
                return hash;
            }
        }


        public void Setup()
        {
            lighting = transform.GetChild(0).GetComponentInChildren<Light>(false);
            radiusSphere = transform.Find("Radius Indicator").GetComponent<MeshRenderer>();
            if(!NetworkServer.active)
                gameObject.SetActive(false);
            animation = new ScaleAnimation(radiusSphere.transform, .2f);
            rules = ReviveRules.instance;
            GlobalOnCreated?.Invoke(this);
        }

        public void SetValuesReceive(NetworkInstanceId deadPlayerId,
            List<NetworkInstanceId> _insidePlayerIDs, float scale, float fractionPerSecond)
        {
            Log.Debug($"Received death totem values. Rad: {scale}");
            deadPlayerId = deadPlayerId;
            fractionPerSecond = fractionPerSecond;
            insidePlayerIDs = _insidePlayerIDs;
            cachedRadius = scale;
            animation.AnimateTo(Vector3.one * scale);
            GlobalOnValuesReceived?.Invoke(this);
        }

        public void SetValuesSend(float speed, float radius, bool forceUpdate = false)
        {
            if (!forceUpdate
                && Mathf.Approximately(speed, fractionPerSecond)
                && Mathf.Approximately(radius, cachedRadius)
            )
            {
                return;
            }
        
            fractionPerSecond = speed;
            cachedRadius = radius;
            animation.AnimateTo(Vector3.one * radius);
        
            SyncToClients();
        }
        
        public void SetValues(float speed, float radius)
        {
            fractionPerSecond = speed;
            cachedRadius = radius;
            animation.AnimateTo(Vector3.one * radius);
        }

        public void RemoveDeadIDs()
        {
            for (int i = 0; i < insidePlayerIDs.Count; i++)
            {
                var id = insidePlayerIDs[i];
                var p = PlayersTracker.instance.FindByBodyId(id);
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
            Log.DebugMethod($"Rad: {cachedRadius}");
            RemoveDeadIDs();
            new SyncDeathTotemMessage(GetComponent<NetworkIdentity>().netId, deadPlayerId, insidePlayerIDs, cachedRadius, fractionPerSecond).Send(NetworkDestination.Clients);
            Log.Debug("Rad: " + radiusSphere.transform.localScale.x);
        }

        void SetLighting()
        {
            lighting.color = Color.Lerp(ReviveProgressBarTracker.ZeroProgressColor, ReviveProgressBarTracker.FullProgressColor, progress);
            
            if(progress > 0 && insidePlayerIDs.Count == 0)
            {
                lighting.color = ReviveProgressBarTracker.NegativeProgressColor;
            }

            lighting.intensity = 8 - progress;
            lighting.range = cachedRadius * 2f;
        }

        private float damageNumberElapsed = 0;
        private float damageNumberRate = .15f;

        void DamageNumbers()
        {
            if (progress >= 1 || insidePlayerIDs.Count == 0) return;
            
            damageNumberElapsed += Time.deltaTime;
            if (damageNumberElapsed < damageNumberRate)
            {
                return;
            }

            foreach (var playerID in insidePlayerIDs)
            {
                var body = GetBody(playerID);
                if (!body)
                    continue;
                
                var deadPlayerObolsCount = body.master.inventory.GetItemCount(CharonsObol.Index);
                var reviverReviveEverywhereItemCount = body.master.inventory.GetItemCount(ReviveEverywhereItem.Index);
                var damageSpeed = rules.GetDamageSpeed(insidePlayerIDs.Count, body.maxHealth, deadPlayerObolsCount, reviverReviveEverywhereItemCount);

                DamageNumberManager.instance.SpawnDamageNumber(damageSpeed * damageNumberElapsed, body.transform.position + Vector3.up * 0.7f, false, TeamIndex.Player, DamageColorIndex.Bleed);
            }

            DamageNumberManager.instance.SpawnDamageNumber(fractionPerSecond * damageNumberElapsed * 100, transform.position, false, TeamIndex.Player, DamageColorIndex.Heal);
            damageNumberElapsed = 0;
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
