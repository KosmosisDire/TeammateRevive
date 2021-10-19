using System.Linq;
using RoR2;
using TeammateRevive.Common;
using TeammateRevive.Configuration;
using TeammateRevive.Logging;
using TeammateRevive.Players;
using TeammateRevive.ProgressBar;
using TeammateRevive.Resources;
using TeammateRevive.Revive.Rules;
using TeammateRevive.Skull;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace TeammateRevive.Revive
{
    public class RevivalTracker
    {
        public static RevivalTracker instance;
        public static readonly string[] IgnoredStages = { "arena", "bazaar" };

        private readonly PlayersTracker players;
        private readonly RunTracker run;
        private readonly ReviveRulesCalculator rules;

        public RevivalTracker(PlayersTracker players, RunTracker run, ReviveRulesCalculator rules)
        {
            instance = this;
            this.players = players;
            this.run = run;
            this.rules = rules;
            
            this.players.OnPlayerDead += OnPlayerDead;
            this.players.OnPlayerAlive += OnPlayerAlive;
            
            DeadPlayerSkull.GlobalOnClientCreated += OnClientSkullSpawned;
        }

        private ProgressBarController progressBar;
        private SkullTracker skullTracker;
        private ReviveProgressBarTracker reviveProgressBarTracker;

        public void Init()
        {
            Log.DebugMethod("start");
            this.skullTracker = new SkullTracker();
            this.progressBar = new ProgressBarController();
            this.reviveProgressBarTracker = new ReviveProgressBarTracker(this.progressBar, this.players, this.skullTracker, this.rules);

            On.RoR2.CharacterBody.RecalculateStats += OnCharacterBodyRecalculateStats;
            On.RoR2.Stage.Start += OnStageStart;
            On.RoR2.Run.BuildDropTable += OnBuildDropTable;

            Log.DebugMethod("end");
        }
        
        #region Event handlers

        void OnPlayerDead(Player player)
        {
            ServerSpawnSkull(player);
            player.ClearReviveInvolvement();
        }
        
        void OnPlayerAlive(Player player)
        {
            foreach (var otherPlayer in this.players.All)
            {
                otherPlayer.RemoveReviveInvolvement(player);
            }
        }

        void OnBuildDropTable(On.RoR2.Run.orig_BuildDropTable orig, Run self)
        {
            orig(self);

            // Remove Obol from drop list if single player
            if (NetworkUser.readOnlyInstancesList.Count < 2)
            {
                var respawnItemIdx = self.availableTier2DropList.FindIndex(pi => pi.pickupDef.itemIndex == ItemsAndBuffs.ReviveItemIndex);
                if (respawnItemIdx >= 0)
                {
                    Log.Info("Only one player in game - removing respawn item from drop list");
                    self.availableTier2DropList.RemoveAt(respawnItemIdx);
                }
            }
        }

        void OnStageStart(On.RoR2.Stage.orig_Start orig, Stage self)
        {
            orig(self);
            var sceneName = self.sceneDef.cachedName;
            Log.Debug($"Stage start: {self.sceneDef.cachedName}");
            this.skullTracker.Clear();

            if (NetworkHelper.IsClient() || IgnoredStages.Contains(sceneName))
                return;

            foreach (var networkUser in NetworkUser.readOnlyInstancesList)
            {
                RemoveReduceHpItem(networkUser);
            }
            foreach (var player in this.players.All)
            {
                player.ClearReviveInvolvement();
            }
        }
        
        void OnClientSkullSpawned(DeadPlayerSkull skull)
        {
            CreateInteraction(skull.gameObject);
        }

        private void OnCharacterBodyRecalculateStats(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            if (self.inventory == null)
            {
                orig(self);
                return;
            }

            // cache previous values of health/shield, since they will be overriden on orig() call
            var health = self.healthComponent.health;
            var shield = self.healthComponent.shield;

            orig(self);

            var reducesCount = self.inventory.GetItemCount(ItemsAndBuffs.ReduceHpItemIndex);
            self.SetBuffCount(ItemsAndBuffs.DeathCurseBuffIndex, reducesCount);
            if (reducesCount == 0)
                return;

            var actualReduceFactor = this.rules.GetCurseReduceHpFactor(reducesCount);
            var hpReduce = self.maxHealth - self.maxHealth / actualReduceFactor;
            var shieldReduce = self.maxShield - self.maxShield / actualReduceFactor;

            self.maxHealth -= hpReduce;
            self.maxShield -= shieldReduce;
            // original logic: maxHP = current max HP / cursePenalty
            self.cursePenalty += actualReduceFactor - 1;

            // this should cut excess health/shield on client
            if (NetworkHelper.IsServer)
            {
                self.healthComponent.Networkhealth = Mathf.Min(self.maxHealth, health);
                self.healthComponent.Networkshield = Mathf.Min(self.maxShield, self.healthComponent.shield, shield);
            }
            else
            {
                self.healthComponent.health = Mathf.Min(self.maxHealth, health);
                self.healthComponent.shield = Mathf.Min(self.maxShield, self.healthComponent.shield, shield);
            }
        }
        
        #endregion Event handlers
        
        public void Update()
        {
            // nothing to do if run didn't start yet
            if (!this.run.IsStarted) return;
            
            // for client, we'll need to update progress bar display only
            if (NetworkHelper.IsClient())
            {
                this.reviveProgressBarTracker.Update();
                return;
            }

            // if players didn't finish setup yet, we cannot do any updates
            if (!this.players.Setup) return;
            
            // TODO: do we really need to recalculate ground position on every update?
            UpdatePlayersGroundPosition();

            var time = Time.time;
            // interactions between dead and alive players
            for (var deadIdx = 0; deadIdx < this.players.Dead.Count; deadIdx++)
            {
                var dead = this.players.Dead[deadIdx];
                var skull = dead.skull;
                
                //have they been revived by other means?
                // TODO: uncomment
                // if (dead.CheckAlive()) 
                // {
                //     this.players.PlayerAlive(dead);
                //     continue;
                // }
                var totalReviveSpeed = 0f;
                var totalDmgSpeed = 0f;
                var playersInRange = 0;

                var insidePlayersHash = skull.GetInsidePlayersHash();
                var actualRange = this.rules.CalculateSkullRadius(dead);

                for (var aliveIdx = 0; aliveIdx < this.players.Alive.Count; aliveIdx++)
                {
                    var player = this.players.Alive[aliveIdx];
                    if (player.CheckDead()) continue;

                    var playerBody = player.GetBody();
                    var inRange = Vector3.Distance(playerBody.transform.position, dead.skull.transform.position) < (actualRange * .5);
                    if (inRange)
                    {
                        playersInRange++;
                        player.SetReviveInvolvement(dead, time + this.rules.PostReviveBuffTime);
                        
                        // player entered range
                        if (!skull.insidePlayerIDs.Contains(playerBody.netId))
                            skull.insidePlayerIDs.Add(playerBody.netId);

                        // revive progress
                        var reviveSpeed = this.rules.GetReviveSpeed(player, skull.insidePlayerIDs.Count);
                        totalReviveSpeed += reviveSpeed;
                        dead.reviveProgress += reviveSpeed * Time.deltaTime;
                        dead.reviveProgress = Mathf.Clamp01(dead.reviveProgress);

                        // damage alive player - down to 1 HP
                        float damageSpeed = this.rules.GetDamageSpeed(player, dead);
                        totalDmgSpeed += damageSpeed;
                        float damageAmount = damageSpeed * Time.deltaTime;
                        playerBody.healthComponent.Networkhealth -= Mathf.Clamp(damageAmount, 0f, playerBody.healthComponent.health - 1f);
                    }
                    else
                    {
                        // player left the range
                        if (skull.insidePlayerIDs.Contains(playerBody.netId))
                            skull.insidePlayerIDs.Remove(playerBody.netId);
                    }
                }

                UpdateReviveInvolvementBuffs();
                
                //if dead player has recharged enough health, respawn and give curse for everyone in range
                if (dead.reviveProgress >= 1)
                {
                    // uncomment to use characters in range instead
                    // var playersToCurse = this.players.All.Where(p => skull.insidePlayerIDs.Any(id => id.Equals(p.BodyId)))
                    //     .Append(dead)
                    //     .ToArray();
                    
                    var playersToCurse = this.players.Alive.Where(p => p.IsInvolvedInReviveOf(dead))
                        .Append(dead)
                        .ToArray();
                    Revive(dead);
                    AddCurse(playersToCurse);
                    continue;
                }

                UpdateSkull(dead, insidePlayersHash, playersInRange, totalDmgSpeed, totalReviveSpeed);
            }
            
            // progress bar
            this.reviveProgressBarTracker.Update();
        }

        private void UpdateSkull(Player dead, int insidePlayersBefore, int playersInRange,  float totalDmgSpeed, float totalReviveSpeed)
        {
            var skull = dead.skull;
            
            // recalculating range, since it could have been changed after alive/dead interactions
            var actualRange = this.rules.CalculateSkullRadius(dead);
            
            // if players inside changed, forcing update
            var forceUpdate = skull.GetInsidePlayersHash() != insidePlayersBefore;
            if (playersInRange > 0)
            {
                skull.progress = dead.reviveProgress;

                var avgDmgAmountPerSecond = (totalDmgSpeed / playersInRange).Truncate(1);
                var fractionPerSecond = totalReviveSpeed.Truncate(4);

                skull.SetValuesSend(fractionPerSecond, avgDmgAmountPerSecond, actualRange, forceUpdate);
            }
            else
            {
                var prevReviveProgress = dead.reviveProgress;

                // if no characters are in range, reduce revive progress
                dead.reviveProgress =
                    Mathf.Clamp01(dead.reviveProgress + this.rules.ReduceReviveProgressSpeed * Time.deltaTime);

                // if reviving progress become 0, remove involvement from all players
                if (prevReviveProgress != 0 && dead.reviveProgress == 0)
                {
                    foreach (var player in this.players.All)
                    {
                        player.RemoveReviveInvolvement(dead);
                    }
                }

                skull.progress = dead.reviveProgress;
                skull.SetValuesSend(this.rules.ReduceReviveProgressSpeed, 0, actualRange, forceUpdate);
            }
        }

        void UpdatePlayersGroundPosition()
        {
            foreach (var player in this.players.Alive)
            {
                if (player.GetBody() == null)
                {
                    continue;
                }
                player.UpdateGroundPosition();
            }
        }
        
        void RemoveReduceHpItem(NetworkUser networkUser)
        {
            if (NetworkHelper.IsClient()) return;

            Log.DebugMethod();
            var userName = networkUser.userName;
            var inventory = networkUser.master?.inventory;

            if (inventory == null)
            {
                Log.Warn($"Player has no inventory! {userName}");
                return;
            }

            var reduceHpItemCount = inventory.GetItemCount(ItemsAndBuffs.ReduceHpItemIndex);
            inventory.RemoveItem(ItemsAndBuffs.ReduceHpItemIndex, inventory.GetItemCount(ItemsAndBuffs.ReviveItemIndex) + 1);
            Log.Info(
                $"Removed reduce HP item for ({userName}). Was {reduceHpItemCount}. Now: {inventory.GetItemCount(ItemsAndBuffs.ReduceHpItemIndex)}");
        }

        public DeadPlayerSkull ServerSpawnSkull(Player player)
        {
            var skull = Object.Instantiate(AddedAssets.DeathMarker).GetComponent<DeadPlayerSkull>();

            skull.deadPlayerId = player.networkUser.netId;
            skull.transform.position = player.groundPosition;
            skull.transform.rotation = Quaternion.identity;
            skull.radiusSphere.transform.localScale = Vector3.one * (this.rules.Values.BaseTotemRange);
            CreateInteraction(skull.gameObject);

            player.skull = skull;

            NetworkServer.Spawn(skull.gameObject);
            Log.Info("Skull spawned on Server and Client");

            return skull;
        }

        void UpdateReviveInvolvementBuffs()
        {
            foreach (var player in this.players.Alive)
            {
                var characterBody = player.GetBody();
                if (characterBody == null) continue;
                characterBody.SetBuffCount(ItemsAndBuffs.ReviveInvolvementBuffIndex, player.GetRevivingPlayersInvolvement());
            }
        }

        public void Revive(Player dead)
        {
            Log.DebugMethod();
            this.players.Respawn(dead);
            // removing consumed Dio's Best Friend
            dead.master.master.inventory.RemoveItem(RoR2Content.Items.ExtraLifeConsumed);
        }

        void AddCurse(params Player[] players)
        {
            foreach (var player in players) player.master.master.inventory.GiveItem(ItemsAndBuffs.ReduceHpItemIndex);
        }
        
        void CreateInteraction(GameObject gameObject)
        {
            if (gameObject.GetComponent<EntityLocator>() != null)
            {
                Log.DebugMethod("EntityLocator was null for skull");
                return;
            }

            Log.DebugMethod();
            
            // game object need's collider in order to be interactible
            var collider = gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = AddedAssets.CubeMesh;

            gameObject.AddComponent<ReviveInteraction>();
            var locator = gameObject.AddComponent<EntityLocator>();
            locator.entity = gameObject;
            Log.DebugMethod("done");
        }
    }
}