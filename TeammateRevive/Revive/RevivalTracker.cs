using System.Linq;
using RoR2;
using TeammateRevive.Common;
using TeammateRevive.Content;
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
        public static readonly string[] IgnoredStages = { "bazaar" };

        private readonly PlayersTracker players;
        private readonly RunTracker run;
        private readonly ReviveRules rules;
        private ProgressBarController progressBar;
        private readonly SkullTracker skullTracker;
        private readonly ReviveProgressBarTracker reviveProgressBarTracker;

        public RevivalTracker(PlayersTracker players, RunTracker run, ReviveRules rules, SkullTracker skullTracker, ReviveProgressBarTracker reviveProgressBarTracker)
        {
            instance = this;
            this.players = players;
            this.run = run;
            this.rules = rules;
            this.skullTracker = skullTracker;
            this.reviveProgressBarTracker = reviveProgressBarTracker;

            this.players.OnPlayerDead += OnPlayerDead;
            this.players.OnPlayerAlive += OnPlayerAlive;
            
            DeadPlayerSkull.GlobalOnClientCreated += OnClientSkullSpawned;
            On.RoR2.Stage.Start += OnStageStart;
            On.RoR2.Run.BuildDropTable += OnBuildDropTable;
        }

        #region Event handlers

        void OnPlayerDead(Player player)
        {
            ServerSpawnSkull(player);
            player.ClearReviveLinks();
        }
        
        void OnPlayerAlive(Player player)
        {
            foreach (var otherPlayer in this.players.All)
            {
                otherPlayer.RemoveReviveLink(player);
            }
        }

        void OnBuildDropTable(On.RoR2.Run.orig_BuildDropTable orig, Run self)
        {
            orig(self);
            Log.DebugMethod();

            // Remove Obol from drop list if single player or if death curse disabled
            
            // NOTE: need to check ForceDeathCurseRule rule explicitly because run starts after drop table is built
            var isDeathCurseEnabled = this.run.IsDeathCurseEnabled || (NetworkHelper.IsServer && this.rules.Values.ForceDeathCurseRule);
            
            if (NetworkUser.readOnlyInstancesList.Count < 2 || !isDeathCurseEnabled)
            {
                var respawnItemIdx = self.availableTier2DropList.FindIndex(pi => pi.pickupDef.itemIndex == CharonsObol.Index);
                if (respawnItemIdx >= 0)
                {
                    Log.Info("Removing Charon's Obol from drop list");
                    self.availableTier2DropList.RemoveAt(respawnItemIdx);
                }
                else
                {
                    Log.Info("Charon's Obol isn't found in drop list!");
                }
            }
        }

        void OnStageStart(On.RoR2.Stage.orig_Start orig, Stage self)
        {
            orig(self);
            var sceneName = self.sceneDef.cachedName;
            Log.Debug($"Stage start: {self.sceneDef.cachedName}");
            this.skullTracker.Clear();
            
            foreach (var player in this.players.All)
            {
                player.ClearReviveLinks();
            }

            if (NetworkHelper.IsClient() || IgnoredStages.Contains(sceneName) || !this.run.IsDeathCurseEnabled)
                return;

            foreach (var networkUser in NetworkUser.readOnlyInstancesList)
            {
                RemoveReduceHpItem(networkUser);
            }
        }
        
        void OnClientSkullSpawned(DeadPlayerSkull skull)
        {
            if (!this.run.IsDeathCurseEnabled) return;
            CreateInteraction(skull.gameObject);
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

            // interactions between dead and alive players
            for (var deadIdx = 0; deadIdx < this.players.Dead.Count; deadIdx++)
            {
                var dead = this.players.Dead[deadIdx];
                var skull = dead.skull;

                if (skull == null)
                {
                    Log.DebugMethod($"Skull is missing {deadIdx}");
                    continue;
                }
                
                //have they been revived by other means? (can be disabled for debugging purposes)
                if (dead.CheckAlive() && !this.rules.Values.DebugKeepSkulls)
                {
                    Log.Debug("Removing skull revived by other means");
                    this.players.PlayerAlive(dead);
                    continue;
                }
                var totalReviveSpeed = 0f;
                var playersInRange = 0;

                var insidePlayersHash = skull.GetInsidePlayersHash();
                var actualRange = this.rules.CalculateSkullRadius(dead);

                for (var aliveIdx = 0; aliveIdx < this.players.Alive.Count; aliveIdx++)
                {
                    var reviver = this.players.Alive[aliveIdx];
                    if (reviver.CheckDead()) continue;

                    var playerBody = reviver.GetBody();
                    var hasReviveEverywhere = playerBody.inventory.GetItemCount(ReviveEverywhereItem.Index) > 0;
                    var inRange = hasReviveEverywhere || Vector3.Distance(playerBody.transform.position, skull.transform.position) < (actualRange * .5);
                    if (inRange)
                    {
                        playersInRange++;
                        
                        // player entered range, update players in range list
                        if (!skull.insidePlayerIDs.Contains(playerBody.netId))
                            skull.insidePlayerIDs.Add(playerBody.netId);
                        
                        // revive progress
                        var reviveSpeed = this.rules.GetReviveSpeed(reviver, skull.insidePlayerIDs.Count);
                        totalReviveSpeed += reviveSpeed;
                        dead.reviveProgress += reviveSpeed * Time.deltaTime;
                        dead.reviveProgress = Mathf.Clamp01(dead.reviveProgress);
                        
                        // if player in range, update revive revive links
                        if (this.run.IsDeathCurseEnabled)
                        {
                            reviver.IncreaseReviveLinkDuration(dead, Time.deltaTime + Time.deltaTime  / this.rules.Values.ReduceReviveProgressFactor * this.rules.Values.ReviveLinkBuffTimeFactor);
                        }

                        DamageReviver(playerBody, dead);
                    }
                    else
                    {
                        // player left the range
                        if (skull.insidePlayerIDs.Contains(playerBody.netId))
                            skull.insidePlayerIDs.Remove(playerBody.netId);
                    }
                }

                //if dead player has recharged enough health, respawn and give curse for everyone in range
                if (dead.reviveProgress >= 1)
                {
                    var linkedPlayers = this.players.Alive
                        .Where(p => p.IsLinkedTo(dead))
                        .ToArray();
                    
                    Revive(dead);

                    // add Death Curse to every linked character
                    if (this.run.IsDeathCurseEnabled)
                    {
                        foreach (var player in linkedPlayers)
                            player.master.master.inventory.GiveItem(DeathCurse.ItemIndex);
                        dead.master.master.inventory.GiveItem(DeathCurse.ItemIndex);
                    }
                    
                    // remove revive links from all players
                    foreach (var player in this.players.All) 
                        player.RemoveReviveLink(dead);
                    
                    // cut revived character HP after revival
                    CutReviveeHp(dead);
                    
                    // add post-revive regeneration to revivers
                    foreach (var player in linkedPlayers)
                        player.GetBody().AddTimedBuff(ReviveRegen.Index, this.rules.Values.PostReviveRegenDurationSec);

                    continue;
                }

                UpdateSkull(dead, insidePlayersHash, playersInRange, totalReviveSpeed);
            }
            
            // update revive links
            if (this.run.IsDeathCurseEnabled) 
                UpdateReviveLinkBuffs();
            
            // progress bar
            this.reviveProgressBarTracker.Update();
        }

        private void DamageReviver(CharacterBody playerBody, Player dead)
        {
            // special case fot Transcendence - damage shield instead of HP
            if (playerBody.inventory.GetItemCount(RoR2Content.Items.ShieldOnly) > 0)
            {
                playerBody.healthComponent.Networkshield = CalcDamageResult(
                    playerBody.maxShield,
                    playerBody.healthComponent.shield,
                    0.1f,
                    dead,
                    playerBody.inventory.GetItemCount(ReviveEverywhereItem.Index)
                );
            }
            else
            {
                playerBody.healthComponent.Networkhealth = CalcDamageResult(
                    playerBody.maxHealth,
                    playerBody.healthComponent.health,
                    0.05f,
                    dead,
                    playerBody.inventory.GetItemCount(ReviveEverywhereItem.Index)
                );
            }

            // prevent recharging shield and other "out of combat" stuff like Red Whip during reviving
            if (playerBody.outOfDangerStopwatch > 3) playerBody.outOfDangerStopwatch = 3;
        }

        float CalcDamageResult(float max, float current, float dmgThreshold, Player dead, int reviverReviveEverywhereCount)
        {
            var damageSpeed = this.rules.GetDamageSpeed(max, dead, reviverReviveEverywhereCount);
            var damageAmount = damageSpeed * Time.deltaTime;
            
            var minValue = max * dmgThreshold;
            if (current < minValue)
                return current;
                
            return Mathf.Clamp(
                current - damageAmount,
                minValue,
                max
            );
        }

        private void UpdateSkull(Player dead, int insidePlayersBefore, int playersInRange, float totalReviveSpeed)
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

            var reduceHpItemCount = inventory.GetItemCount(DeathCurse.ItemIndex);
            inventory.RemoveItem(DeathCurse.ItemIndex, inventory.GetItemCount(CharonsObol.Index) + 1);
            Log.Info(
                $"Removed reduce HP item for ({userName}). Was {reduceHpItemCount}. Now: {inventory.GetItemCount(DeathCurse.ItemIndex)}");
        }

        public DeadPlayerSkull ServerSpawnSkull(Player player)
        {
            var skull = Object.Instantiate(AddedAssets.DeathMarker).GetComponent<DeadPlayerSkull>();

            skull.deadPlayerId = player.networkUser.netId;
            skull.transform.position = player.groundPosition;
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

        void UpdateReviveLinkBuffs()
        {
            foreach (var player in this.players.Alive)
            {
                var characterBody = player.GetBody();
                if (characterBody == null) continue;
                characterBody.SetBuffCount(ReviveLink.Index, player.GetPlayersReviveLinks());
            }
        }

        public void Revive(Player dead)
        {
            Log.DebugMethod();
            this.players.Respawn(dead);
            // removing consumed Dio's Best Friend
            dead.master.master.inventory.RemoveItem(RoR2Content.Items.ExtraLifeConsumed);
        }

        void CutReviveeHp(Player revivee)
        {
            var body = revivee.GetBody();
            body.RecalculateStats();
            var effectiveHp = (body.maxHealth + body.maxShield) * .3f;
            body.healthComponent.Networkhealth = Mathf.Clamp(effectiveHp, 1, body.maxHealth);
            body.healthComponent.Networkshield = Mathf.Clamp(body.maxHealth - effectiveHp, 0, body.maxShield);
        }
        
        void CreateInteraction(GameObject gameObject)
        {
            gameObject.AddIfMissing<EntityLocator>().entity = gameObject;
            gameObject.AddIfMissing<ReviveInteraction>();
            
            var meshGo = gameObject.GetComponentInChildren<MeshFilter>().gameObject;
            var collider = meshGo.AddIfMissing<MeshCollider>();
            collider.isTrigger = true;
            collider.convex = true;
            
            meshGo.AddIfMissing<EntityLocator>().entity = gameObject;
            
            // game object need's collider in order to be interactible
            // gameObject.AddIfMissing<MeshCollider>().sharedMesh = AddedAssets.CubeMesh;
            Log.DebugMethod("done");
        }
    }
}