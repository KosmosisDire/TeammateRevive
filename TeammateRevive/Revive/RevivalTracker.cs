using System.Linq;
using RoR2;
using TeammateRevive.Common;
using TeammateRevive.Configuration;
using TeammateRevive.Logging;
using TeammateRevive.Players;
using TeammateRevive.ProgressBar;
using TeammateRevive.Resources;
using TeammateRevive.Skull;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace TeammateRevive.Revive
{
    public class RevivalTracker
    {
        public static RevivalTracker instance;

        private readonly PlayersTracker players;
        private readonly RunTracker run;
        private readonly PluginConfig config;

        public RevivalTracker(PluginConfig config, PlayersTracker players, RunTracker run)
        {
            instance = this;
            this.players = players;
            this.run = run;
            this.config = config;
            this.reduceReviveProgressSpeed = -(1f / this.config.ReviveTimeSeconds * ReduceReviveProgressFactor); 
            // TODO: refactor
            this.players.OnPlayerDead += player => ServerSpawnSkull(player);
        }

        public static float ReduceHpFactor = 1 + 1 / 3f;
        public static float BaseReduceHpFactor = .25f;
        public static float ObolReviveFactor = 1.125f;
        public static float ReduceReviveProgressFactor = .15f;
        public static readonly string[] IgnoredStages = { "arena", "bazaar" };

        private readonly float reduceReviveProgressSpeed;

        private ProgressBarController progressBar;
        private SkullTracker skullTracker;
        private ReviveProgressTracker reviveProgressTracker;

        public void Init()
        {
            Log.DebugMethod("start");
            this.skullTracker = new SkullTracker();
            this.progressBar = new ProgressBarController();
            this.reviveProgressTracker = new ReviveProgressTracker(this.progressBar, this.players, this.run, this.skullTracker, this.config);

            On.RoR2.CharacterBody.RecalculateStats += CharacterBodyOnRecalculateStats;
            On.RoR2.Stage.Start += StageOnStart;
            On.RoR2.Run.BuildDropTable += OnBuildDropTable;

            Log.DebugMethod("end");
        }

        private void OnBuildDropTable(On.RoR2.Run.orig_BuildDropTable orig, Run self)
        {
            orig(self);

            // Remove Obol from drop list if single player
            if (NetworkUser.readOnlyInstancesList.Count < 2)
            {
                var respawnItemIdx = self.availableTier2DropList.FindIndex(pi => pi.pickupDef.itemIndex == AddedResources.ReviveItemIndex);
                if (respawnItemIdx >= 0)
                {
                    Log.Info("Only one player in game - removing respawn item from drop list");
                    self.availableTier2DropList.RemoveAt(respawnItemIdx);
                }
            }
        }

        private void StageOnStart(On.RoR2.Stage.orig_Start orig, Stage self)
        {
            orig(self);
            var sceneName = self.sceneDef.cachedName;
            Log.Debug($"Stage start: {self.sceneDef.cachedName}");
            this.skullTracker.Clear();

            if (MainTeammateRevival.IsClient() || IgnoredStages.Contains(sceneName))
                return;

            foreach (var networkUser in NetworkUser.readOnlyInstancesList)
            {
                RemoveReduceHpItem(networkUser);
            }
        }

        private void RemoveReduceHpItem(NetworkUser networkUser)
        {
            if (MainTeammateRevival.IsClient()) return;

            Log.DebugMethod();
            var userName = networkUser.userName;
            var inventory = networkUser.master?.inventory;

            if (inventory == null)
            {
                Log.Warn($"Player has no inventory! {userName}");
                return;
            }

            var reduceHpItemCount = inventory.GetItemCount(AddedResources.ReduceHpItemIndex);
            inventory.RemoveItem(AddedResources.ReduceHpItemIndex, inventory.GetItemCount(AddedResources.ReviveItemIndex) + 1);
            Log.Info(
                $"Removed reduce HP item for ({userName}). Was {reduceHpItemCount}. Now: {inventory.GetItemCount(AddedResources.ReduceHpItemIndex)}");
        }

        private void CharacterBodyOnRecalculateStats(On.RoR2.CharacterBody.orig_RecalculateStats orig,
            RoR2.CharacterBody self)
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

            var reducesCount = self.inventory.GetItemCount(AddedResources.ReduceHpItemIndex);
            self.SetBuffCount(AddedResources.ReduceHpBuffIndex, reducesCount);
            if (reducesCount == 0)
                return;

            var actualReduceFactor = Mathf.Pow(ReduceHpFactor, reducesCount) + BaseReduceHpFactor;
            var hpReduce = self.maxHealth - self.maxHealth / actualReduceFactor;
            var shieldReduce = self.maxShield - self.maxShield / actualReduceFactor;

            self.maxHealth -= hpReduce;
            self.maxShield -= shieldReduce;
            // original logic: maxHP = current max HP / cursePenalty
            self.cursePenalty += actualReduceFactor - 1;

            // this should cut excess health/shield on client
            if (MainTeammateRevival.IsServer)
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

        public DeadPlayerSkull ServerSpawnSkull(Player player)
        {
            var skull = Object.Instantiate(AddedResources.DeathMarker).GetComponent<DeadPlayerSkull>();

            skull.deadPlayerId = player.networkUser.netId;
            skull.transform.position = player.groundPosition;
            skull.transform.rotation = Quaternion.identity;
            skull.radiusSphere.transform.localScale = Vector3.one * (this.config.TotemRange);
            CreateInteraction(skull.gameObject);

            player.skull = skull;

            NetworkServer.Spawn(skull.gameObject);
            Log.Info("Skull spawned on Server and Client");

            return skull;
        }

        public void OnClientSkullSpawned(DeadPlayerSkull skull)
        {
            CreateInteraction(skull.gameObject);
        }

        public void Update()
        {
            if (!this.run.IsStarted) return;
            
            if (MainTeammateRevival.IsClient())
            {
                this.reviveProgressTracker.Update();
                return;
            }

            if (!this.players.Setup) return;
            
            // update ground positions
            foreach (var player in this.players.Alive)
            {
                if (player.GetBody() == null)
                {
                    continue;
                }
                player.groundPosition = MainTeammateRevival.GroundPosition(player);
            }

            // interactions between dead and alive players
            // NOTE: using for is necessary, since these collections can be modified during iteration
            for (var deadIdx = 0; deadIdx < this.players.Dead.Count; deadIdx++)
            {
                var dead = this.players.Dead[deadIdx];
                var skull = dead.skull;
                
                //have they been revived by other means?
                // TODO: for debug, uncomment!
                // if (dead.CheckAlive()) 
                // {
                //     this.players.PlayerAlive(dead);
                //     continue;
                // }
                var totalHealSpeed = 0f;
                var totalDmgSpeed = 0f;
                var playersInRange = 0;

                var insidePlayersHash = skull.GetInsidePlayersHash();
                var actualRange = CalculateSkullRadius(dead);

                for (var aliveIdx = 0; aliveIdx < this.players.Alive.Count; aliveIdx++)
                {
                    var player = this.players.Alive[aliveIdx];
                    if (player.CheckDead()) continue;
                    
                    var inRange = Vector3.Distance(player.GetBody().transform.position, dead.skull.transform.position) < (actualRange * .5);
                    if (inRange)
                    {
                        playersInRange++;
                        
                        // track players inside circle
                        if (!skull.insidePlayerIDs.Contains(player.GetBody().netId))
                            skull.insidePlayerIDs.Add(player.GetBody().netId);
                        
                        // resurrect progress
                        var healSpeed = GetHealPerSecond(player, dead, skull.insidePlayerIDs.Count);
                        totalHealSpeed += healSpeed;
                        dead.rechargedHealth += healSpeed * Time.deltaTime;

                        // damage alive player - down to 1 HP
                        var playersCount = dead.skull.insidePlayerIDs.Count;
                        float damageSpeed = (player.GetBody().maxHealth * 0.85f) / this.config.ReviveTimeSeconds / playersCount;
                        totalDmgSpeed += damageSpeed;
                        float damageAmount = damageSpeed * Time.deltaTime;
                        player.GetBody().healthComponent.Networkhealth -= Mathf.Clamp(damageAmount, 0f, player.GetBody().healthComponent.health - 1f);
                    }
                    else
                    {
                        // track players inside circle
                        if (skull.insidePlayerIDs.Contains(player.GetBody().netId))
                            skull.insidePlayerIDs.Remove(player.GetBody().netId);
                    }
                }
                
                //if dead player has recharged enough health, respawn and give curse for everyone in range
                if (dead.rechargedHealth >= 1)
                {
                    var playersToCurse = this.players.All.Where(p => skull.insidePlayerIDs.Any(id => id.Equals(p.BodyId)))
                        .Append(dead)
                        .ToArray();
                    Revive(dead);
                    AddCurse(playersToCurse);
                    continue;
                }
                
                actualRange = CalculateSkullRadius(dead);
                var forceUpdate = skull.GetInsidePlayersHash() != insidePlayersHash;
                if (playersInRange > 0)
                {
                    dead.skull.progress = dead.rechargedHealth;
                    
                    // effectively flooring value to reduce sync packages count
                    var avgDmgAmount = (int)(totalDmgSpeed * Time.deltaTime / playersInRange);
                    var fractionPerSecond = totalHealSpeed.Truncate(4);
                    
                    skull.SetValuesSend(fractionPerSecond, avgDmgAmount, actualRange, forceUpdate);
                }
                else
                {
                    // if no characters are in range, reduce revive progress
                    dead.rechargedHealth = Mathf.Clamp01(dead.rechargedHealth + this.reduceReviveProgressSpeed * Time.deltaTime);
                    dead.skull.progress = dead.rechargedHealth;

                    skull.SetValuesSend(this.reduceReviveProgressSpeed, 0, actualRange, forceUpdate);
                }
            }
            
            // progress bar
            this.reviveProgressTracker.Update();
        }

        public void Revive(Player dead)
        {
            Log.DebugMethod();
            this.players.Respawn(dead);
            // removing consumed Dio's Best Friend
            dead.master.master.inventory.RemoveItem(RoR2Content.Items.ExtraLifeConsumed);
        }

        public void AddCurse(params Player[] players)
        {
            foreach (var player in players) player.master.master.inventory.GiveItem(AddedResources.ReduceHpItemIndex);
        }

        private float CalculateSkullRadius(Player dead)
        {
            var reviveItemBonus = this.config.TotemRange *
                                  dead.master.master.inventory.GetItemCount(AddedResources.ReviveItemIndex) * 0.5f;
            var playersCountBonus = this.config.IncreaseRangeWithPlayers
                ? this.config.TotemRange * dead.skull.insidePlayerIDs.Count * .4f
                : 0;
            
            return this.config.TotemRange + reviveItemBonus + playersCountBonus;
        }

        public float GetHealPerSecond(Player player, Player dead, int playersInRange)
        {
            var obolsCount = player.master.master.inventory.GetItemCount(AddedResources.ReviveItemIndex);
            var healPerSecond = (1f / this.config.ReviveTimeSeconds / playersInRange) * GetActualReviveItemFactor(obolsCount);
            return healPerSecond;
        }
        
        public float GetActualReviveItemFactor(int obolsCount) => this.config.ReviveTimeSeconds / (this.config.ReviveTimeSeconds / Mathf.Pow(ObolReviveFactor, obolsCount));

        private void CreateInteraction(GameObject gameObject)
        {
            if (gameObject.GetComponent<EntityLocator>() != null)
            {
                Log.DebugMethod("EntityLocator was null for skull");
                return;
            }

            Log.DebugMethod();
            
            // game object need's collider in order to be interactible
            var collider = gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = AddedResources.CubeMesh;

            gameObject.AddComponent<ReviveInteraction>();
            var locator = gameObject.AddComponent<EntityLocator>();
            locator.entity = gameObject;
            Log.DebugMethod("done");
        }
    }
}