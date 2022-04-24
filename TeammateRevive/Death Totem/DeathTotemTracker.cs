using System.Collections.Generic;
using System.Linq;
using RoR2;
using TeammateRevive.Common;
using TeammateRevive.Logging;
using TeammateRevive.Players;
using TeammateRevive.Resources;
using TeammateRevive.Revive;
using TeammateRevive.Revive.Rules;
using UnityEngine;
using UnityEngine.Networking;

namespace TeammateRevive.DeathTotem
{
    public class DeathTotemTracker
    {
        private readonly PlayersTracker players;
        private readonly RunTracker run;
        private readonly ReviveRules rules;
        public static DeathTotemTracker instance;
        
        public readonly HashSet<DeathTotemBehavior> totems = new();

        public bool HasAnyTotems => this.totems.Count > 0;

        public DeathTotemTracker(PlayersTracker players, RunTracker run, ReviveRules rules)
        {
            instance = this;
            
            this.players = players;
            this.run = run;
            this.rules = rules;
            
            DeathTotemBehavior.GlobalOnDestroy += OnTotemDestroy;
            DeathTotemBehavior.GlobalOnCreated += OnTotemUpdate;
            DeathTotemBehavior.GlobalOnValuesReceived += OnTotemUpdate;
            DeathTotemBehavior.GlobalOnClientCreated += OnClientTotemSpawned;
            this.players.OnPlayerDead += OnPlayerDead;
        }

        public void Clear()
        {
            this.totems.Clear();
        }
        
        void OnPlayerDead(Player player)
        {
            ServerSpawnTotem(player);
        }
        
        public void UpdateTotem(Player dead, int insidePlayersBefore, int playersInRange, float totalReviveSpeed)
        {
            var totem = dead.deathTotem;
            
            // recalculating range, since it could have been changed after alive/dead interactions
            var actualRange = this.rules.CalculateDeathTotemRadius(dead);
            
            // if players inside changed, forcing update
            var forceUpdate = totem.GetInsidePlayersHash() != insidePlayersBefore;
            if (playersInRange > 0)
            {
                totem.progress = dead.reviveProgress;
                var fractionPerSecond = totalReviveSpeed.Truncate(4);

                totem.SetValuesSend(fractionPerSecond, actualRange, forceUpdate);
            }
            else
            {
                var prevReviveProgress = dead.reviveProgress;

                // if no characters are in range, reduce revive progress
                dead.reviveProgress =
                    Mathf.Clamp01(dead.reviveProgress + this.rules.ReduceReviveProgressSpeed * Time.deltaTime);

                // if reviving progress become 0, remove revive links from all players
                if (prevReviveProgress != 0 && dead.reviveProgress == 0)
                {
                    foreach (var player in this.players.All)
                    {
                        player.RemoveReviveLink(dead);
                    }
                }

                totem.progress = dead.reviveProgress;
                totem.SetValuesSend(this.rules.ReduceReviveProgressSpeed, actualRange, forceUpdate);
            }
        }
        
        public DeathTotemBehavior ServerSpawnTotem(Player player)
        {
            var totem = Object.Instantiate(CustomResources.DeathTotem);

            totem.deadPlayerId = player.networkUser.netId;
            totem.transform.position = player.groundPosition;
            totem.transform.rotation = Quaternion.identity;
            
            if (this.run.IsDeathCurseEnabled)
            {
                CreateInteraction(totem.gameObject);
            }

            player.deathTotem = totem;

            NetworkServer.Spawn(totem.gameObject);
            Log.Info("Totem spawned on Server and Client");

            return totem;
        }
        
        void OnClientTotemSpawned(DeathTotemBehavior totem)
        {
            if (!this.run.IsDeathCurseEnabled) return;
            CreateInteraction(totem.gameObject);
        }
        
        
        void CreateInteraction(GameObject gameObject)
        {
            gameObject.AddIfMissing<EntityLocator>().entity = gameObject;
            gameObject.AddIfMissing<ReviveInteraction>();
            gameObject.layer = LayerIndex.defaultLayer.intVal;

            var meshGo = gameObject.GetComponentInChildren<MeshFilter>().gameObject;
            
            var collider = meshGo.AddIfMissing<MeshCollider>();
            collider.isTrigger = true;
            collider.convex = true;
            meshGo.layer = LayerIndex.defaultLayer.intVal;
            
            meshGo.AddIfMissing<EntityLocator>().entity = gameObject;
            
            // game object need's collider in order to be interactible
            // gameObject.AddIfMissing<MeshCollider>().sharedMesh = AddedAssets.CubeMesh;
            Log.DebugMethod("done");
        }

        private void OnTotemUpdate(DeathTotemBehavior obj)
        {
            Log.Debug("Totem updated! " + string.Join(", ", obj.insidePlayerIDs.Select(i => i.ToString())));
            this.totems.Add(obj);
        }

        private void OnTotemDestroy(DeathTotemBehavior obj)
        {
            Log.Debug("Totem destroyed! " + string.Join(", ", obj.insidePlayerIDs.Select(i => i.ToString())));
            this.totems.Remove(obj);
        }

        public DeathTotemBehavior GetDeathTotemInRange(NetworkInstanceId userBodyId)
        {
            var totem = this.totems.FirstOrDefault(s => s.insidePlayerIDs.Contains(userBodyId));
            return totem;
        }
    }
}