using System.Linq;
using RoR2;
using TeammateRevival;
using TeammateRevival.Logging;
using TeammateRevival.RevivalStrategies;
using TeammateRevive.RevivalStrategies.ReduceMaxHp.ProgressBar;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace TeammateRevive.RevivalStrategies.ReduceMaxHp
{
    class ReduceMaxHpRevivalStrategy : IRevivalStrategy
    {
        public MainTeammateRevival Plugin { get; }
        public PluginConfig Config => MainTeammateRevival.PluginConfig;

        public ReduceMaxHpRevivalStrategy(MainTeammateRevival plugin)
        {
            this.Plugin = plugin;
            reduceReviveProgressSpeed = -(1f / Config.ReviveTimeSeconds * ReduceReviveProgressFactor); 
        }

        private static float ReduceHpFactor = 1.2f;
        private static float ObolReviveFactor = 1.125f;
        private static float ReduceReviveProgressFactor = .33f;
        private static readonly string[] IgnoredStages = { "arena", "bazaar" };

        private readonly float reduceReviveProgressSpeed;

        private ProgressBarController progressBar;
        private ClientResurrectionTracker resurrectionTracker;

        public void Init()
        {
            Log.DebugMethod("start");
            this.resurrectionTracker = new ClientResurrectionTracker();
            this.progressBar = new ProgressBarController();

            AddedResources.Init();

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
                var respawnItemIdx = self.availableTier2DropList.FindIndex(pi => pi.pickupDef.itemIndex == AddedResources.ResurrectItemIndex);
                if (respawnItemIdx >= 0)
                {
                    Log.DebugMethod("Removing respawn item from drop list");
                    self.availableTier2DropList.RemoveAt(respawnItemIdx);
                }
            }
        }

        private void StageOnStart(On.RoR2.Stage.orig_Start orig, Stage self)
        {
            orig(self);
            var sceneName = self.sceneDef.cachedName;
            Log.Debug($"Stage start: {self.sceneDef.cachedName}");

            if (MainTeammateRevival.IsClient() || IgnoredStages.Contains(sceneName))
                return;

            foreach (var networkUser in NetworkUser.readOnlyInstancesList)
            {
                RemoveReduceHpItem(networkUser);
            }
            this.resurrectionTracker.Clear();
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
            inventory.RemoveItem(AddedResources.ReduceHpItemIndex, inventory.GetItemCount(AddedResources.ResurrectItemIndex) + 1);
            Log.Debug(
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


            var hpReduce = self.maxHealth - self.maxHealth / Mathf.Pow(ReduceHpFactor, reducesCount);
            var shieldReduce = self.maxShield - self.maxShield / Mathf.Pow(ReduceHpFactor, reducesCount);

            self.maxHealth -= hpReduce;
            self.maxShield -= shieldReduce;
            // original logic: maxHP = current max HP / cursePenalty
            self.cursePenalty += (ReduceHpFactor / 2) * reducesCount;

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
            var skull = Object.Instantiate(this.Plugin.DeathMarker).GetComponent<DeadPlayerSkull>();

            skull.transform.position = player.groundPosition;
            skull.transform.rotation = Quaternion.identity;
            skull.radiusSphere.transform.localScale = Vector3.one * (this.Config.TotemRange);
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
            if (MainTeammateRevival.IsClient())
            {
                UpdateProgressBar();
                return;
            }
            
            // update ground positions
            foreach (var player in Plugin.AlivePlayers)
            {
                if (player.GetBody() == null)
                {
                    continue;
                }
                player.groundPosition = MainTeammateRevival.GroundPosition(player);
            }

            Player localUserInteractibleSkull = null;

            // interactions between dead and alive players
            // NOTE: using for is necessary, since these collections can be modified during iteration
            for (var deadIdx = 0; deadIdx < this.Plugin.DeadPlayers.Count; deadIdx++)
            {
                var dead = this.Plugin.DeadPlayers[deadIdx];
                var skull = dead.skull;
                
                //have they been revived by other means?
                if (dead.CheckAlive()) 
                {
                    Plugin.PlayerAlive(dead);
                    continue;
                }
                var totalHealSpeed = 0f;
                var totalDmgSpeed = 0f;
                var playersInRange = 0;

                var insidePlayersHash = skull.GetInsidePlayersHash();

                for (var aliveIdx = 0; aliveIdx < this.Plugin.AlivePlayers.Count; aliveIdx++)
                {
                    var player = this.Plugin.AlivePlayers[aliveIdx];
                    if (player.CheckDead()) continue;
                    
                    var inRange = Vector3.Distance(player.groundPosition, dead.skull.transform.position) < this.Config.TotemRange;
                    if (inRange)
                    {
                        playersInRange++;

                        if (Plugin.CurrentPlayer == player)
                        {
                            localUserInteractibleSkull = dead;
                        }
                        
                        // track players inside circle
                        if (!skull.insidePlayerIDs.Contains(player.GetBody().netId))
                            skull.insidePlayerIDs.Add(player.GetBody().netId);
                        
                        // resurrect progress
                        var healSpeed = GetHealSpeed(player, dead, skull.insidePlayerIDs.Count);
                        totalHealSpeed += healSpeed;
                        dead.rechargedHealth += healSpeed * Time.deltaTime;

                        // damage alive player - down to 1 HP
                        var playersCount = dead.skull.insidePlayerIDs.Count;
                        float damageSpeed = (player.GetBody().maxHealth * 0.85f) / Config.ReviveTimeSeconds / playersCount;
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
                
                //if dead player has recharged enough health, respawn
                if (dead.rechargedHealth >= 1)
                {
                    var playersToCurse = Plugin.AllPlayers.Where(p => skull.insidePlayerIDs.Contains(p.bodyID))
                        .Append(dead)
                        .ToArray();
                    Revive(dead);
                    AddCurse(playersToCurse);
                    continue;
                }
                
                if (playersInRange > 0)
                {
                    dead.skull.progress = dead.rechargedHealth;
                    
                    // effectively flooring value to reduce sync packages count
                    var avgDmgAmount = (int)(totalDmgSpeed * Time.deltaTime / playersInRange);
                    
                    var data = skull.Data.Clone();
                    data.Amount = avgDmgAmount;
                    data.FractionPerSecond = totalHealSpeed.Truncate(4);
                    var forceUpdate = skull.GetInsidePlayersHash() != insidePlayersHash;
                    
                    skull.SetValuesSend(data, forceUpdate);
                }
                else
                {
                    // if no characters are in range, reduce revive progress
                    dead.rechargedHealth = Mathf.Clamp01(dead.rechargedHealth - this.reduceReviveProgressSpeed * Time.deltaTime);
                    dead.skull.progress = dead.rechargedHealth;
                    
                    var data = new SkullData
                    {
                        Amount = skull.Amount,
                        Color = new Color(1, 0, 0),
                        FractionPerSecond = reduceReviveProgressSpeed
                    };
                    skull.SetValuesSend(data);
                }
            }
            
            // progress bar
            if (localUserInteractibleSkull != null)
            {
                this.progressBar.SetFraction(localUserInteractibleSkull.rechargedHealth);
                this.progressBar.SetUser(localUserInteractibleSkull.networkUser.userName);
            }
            else if (Plugin.CurrentPlayer.CheckDead())
            {
                UpdateProgressBar();
            }
            else
            {
                this.progressBar.Hide();
            }
        }

        public void Revive(Player dead)
        {
            Log.DebugMethod();
            MainTeammateRevival.instance.RespawnPlayer(dead);
            // removing consumed Dio's Best Friend
            dead.master.master.inventory.RemoveItem(RoR2Content.Items.ExtraLifeConsumed);
        }

        public void AddCurse(params Player[] players)
        {
            foreach (var player in players) player.master.master.inventory.GiveItem(AddedResources.ReduceHpItemIndex);
        }

        private void UpdateProgressBar()
        {
            var trackingBodyId = Plugin.CurrentPlayerBodyId;
            
            // trying to get target player in spectator mode
            if (trackingBodyId == null && MainTeammateRevival.runStarted && this.Plugin.CurrentPlayer != null)
            {
                var cameraRig = CameraRigController.instancesList.FirstOrDefault(cr => cr.viewer == this.Plugin.CurrentPlayer.networkUser);
                if (cameraRig != null)
                {
                    var targetBody = cameraRig.targetBody;
                    if (targetBody != null && targetBody.isPlayerControlled) {
                        trackingBodyId = targetBody.netId;
                    }
                }
            }

            if (trackingBodyId == null)
            {
                return;
            }
            
            var skull = this.resurrectionTracker.GetResurrectingSkull(trackingBodyId.Value);
            if (skull == null)
            {
                this.progressBar.Hide();
            }
            else
            {
                this.progressBar.SetFraction(skull.progress);
                this.progressBar.SetUser(skull.PlayerName);
            }
        }

        public float GetHealSpeed(Player player, Player dead, int playersInRange)
        {
            var obolsCount = player.master.master.inventory.GetItemCount(AddedResources.ResurrectItemIndex);
            var reviveItemMod = Config.ReviveTimeSeconds / (Config.ReviveTimeSeconds / Mathf.Pow(ObolReviveFactor, obolsCount));
            var healPerSecond = (1f / Config.ReviveTimeSeconds / playersInRange) * reviveItemMod;
            return healPerSecond;
        }

        private void CreateInteraction(GameObject gameObject)
        {
            // TODO: for some reason this can be called for skull with EntityLocator already initialized
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