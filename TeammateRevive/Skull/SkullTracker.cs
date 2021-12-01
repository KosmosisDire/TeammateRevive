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

namespace TeammateRevive.Skull
{
    public class SkullTracker
    {
        private readonly PlayersTracker players;
        private readonly RunTracker run;
        private readonly ReviveRules rules;
        public static SkullTracker instance;
        
        public readonly HashSet<DeadPlayerSkull> skulls = new();

        public bool HasAnySkulls => this.skulls.Count > 0;

        public SkullTracker(PlayersTracker players, RunTracker run, ReviveRules rules)
        {
            instance = this;
            
            this.players = players;
            this.run = run;
            this.rules = rules;
            
            DeadPlayerSkull.GlobalOnDestroy += OnSkullDestroy;
            DeadPlayerSkull.GlobalOnCreated += OnSkullUpdate;
            DeadPlayerSkull.GlobalOnValuesReceived += OnSkullUpdate;
            DeadPlayerSkull.GlobalOnClientCreated += OnClientSkullSpawned;
            this.players.OnPlayerDead += OnPlayerDead;
        }

        public void Clear()
        {
            this.skulls.Clear();
        }
        
        void OnPlayerDead(Player player)
        {
            ServerSpawnSkull(player);
        }
        
        public void UpdateSkull(Player dead, int insidePlayersBefore, int playersInRange, float totalReviveSpeed)
        {
            var skull = dead.skull;
            
            // recalculating range, since it could have been changed after alive/dead interactions
            var actualRange = this.rules.CalculateSkullRadius(dead);
            
            // if players inside changed, forcing update
            var forceUpdate = skull.GetInsidePlayersHash() != insidePlayersBefore;
            if (playersInRange > 0)
            {
                skull.progress = dead.reviveProgress;
                var fractionPerSecond = totalReviveSpeed.Truncate(4);

                skull.SetValuesSend(fractionPerSecond, actualRange, forceUpdate);
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

                skull.progress = dead.reviveProgress;
                skull.SetValuesSend(this.rules.ReduceReviveProgressSpeed, actualRange, forceUpdate);
            }
        }
        
        public DeadPlayerSkull ServerSpawnSkull(Player player)
        {
            var skull = Object.Instantiate(AddedAssets.DeathMarker).GetComponent<DeadPlayerSkull>();

            skull.deadPlayerId = player.networkUser.netId;
            skull.transform.position = player.master.master.deathFootPosition;
            skull.transform.rotation = Quaternion.identity;
            
            if (this.run.IsDeathCurseEnabled)
            {
                CreateInteraction(skull.gameObject);
            }

            player.skull = skull;

            NetworkServer.Spawn(skull.gameObject);
            Log.Info("Skull spawned on Server and Client");

            return skull;
        }
        
        void OnClientSkullSpawned(DeadPlayerSkull skull)
        {
            if (!this.run.IsDeathCurseEnabled) return;
            CreateInteraction(skull.gameObject);
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

        private void OnSkullUpdate(DeadPlayerSkull obj)
        {
            Log.Debug("Skull updated! " + string.Join(", ", obj.insidePlayerIDs.Select(i => i.ToString())));
            this.skulls.Add(obj);
        }

        private void OnSkullDestroy(DeadPlayerSkull obj)
        {
            Log.Debug("Skull destroyed! " + string.Join(", ", obj.insidePlayerIDs.Select(i => i.ToString())));
            this.skulls.Remove(obj);
        }

        public DeadPlayerSkull GetSkullInRange(NetworkInstanceId userBodyId)
        {
            var skull = this.skulls.FirstOrDefault(s => s.insidePlayerIDs.Contains(userBodyId));
            return skull;
        }
    }
}