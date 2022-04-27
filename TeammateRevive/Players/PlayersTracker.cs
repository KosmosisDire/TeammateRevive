using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using RoR2;
using TeammateRevive.Common;
using TeammateRevive.Configuration;
using TeammateRevive.Logging;
using UnityEngine;
using UnityEngine.Networking;

namespace TeammateRevive.Players
{
    public class PlayersTracker
    {
        public static PlayersTracker instance;
        
        private readonly RunTracker run;
        private readonly PluginConfig config;
        
        int numPlayersSetup = 0;
        float playerSetupTimer = 3;

        public event Action<Player> OnPlayerDead;
        public event Action<Player> OnPlayerAlive;
        public event Action OnSetupFinished;
        public event Action<Player> OnPlayerRespawned;

        public PlayerCharacterMasterController CurrentUserPlayerCharacterMasterController { get; set; }
        public NetworkInstanceId? CurrentUserBodyId => this.CurrentUserPlayerCharacterMasterController?.master.GetBody()?.netId;

        public List<Player> Alive = new();
        public List<Player> Dead = new();
        public List<Player> All = new();
        
        public int TotalCount { get; set; }
        public bool Setup { get; set; }

        public PlayersTracker(RunTracker run, PluginConfig config)
        {
            instance = this;
            
            this.run = run;
            this.config = config;
            
            On.RoR2.Run.OnUserRemoved += hook_OnUserRemoved;
            On.RoR2.GlobalEventManager.OnPlayerCharacterDeath += hook_OnPlayerCharacterDeath;
            On.RoR2.PlayerCharacterMasterController.OnBodyStart += hook_OnBodyStart;
        }

        public void Respawn(Player player)
        {
            if (NetworkHelper.IsClient()) return;

            if (!this.Dead.Contains(player)) return;

            bool playerConnected = player.master.isConnected;
            if (playerConnected)
            {
                if (!player.CheckAlive())
                {
                    ReviveHelper.RespawnExtraLife(player.master.master);
                }
                else
                {
                    Log.Warn("Respawn was called for alive player!");
                }

                PlayerAlive(player);
            }
            OnPlayerRespawned?.Invoke(player);
            Log.Info("Player Respawned");
        }

        public void Reset()
        {
            this.Setup = false;
            this.Alive.Clear();
            this.Dead.Clear();
            this.All.Clear();
            this.TotalCount = 0;
            this.numPlayersSetup = 0;
            this.playerSetupTimer = 0;
        }
        
        public Player FindByCharacterMasterControllerId(NetworkInstanceId id) 
        {
            foreach (var p in this.All) 
            {
                if(p.master && p.master.netId == id)
                {
                    return p;
                }
            }
            return null;
        }
        
        public Player FindByBodyId(NetworkInstanceId id) 
        {
            foreach (var p in this.Alive)
            {
                p.GetBody();
                if (p.BodyId == id) return p;
            }

            return null;
        }
        
        void hook_OnUserRemoved(On.RoR2.Run.orig_OnUserRemoved orig, Run self, NetworkUser user)
        {
            if (NetworkHelper.IsClient())
            {
                orig(self, user);
                return;
            }

            if (!this.run.IsStarted) 
            {
                Log.Info(user.userName + " left while run wasn't in session.");
            }

            Player leavingPlayer = FindByCharacterMasterControllerId(user.masterController.netId);
            if (this.All.Contains(leavingPlayer))
            {
                this.All.Remove(leavingPlayer);
                if (this.Dead.Contains(leavingPlayer)) this.Dead.Remove(leavingPlayer);
                if (this.Alive.Contains(leavingPlayer)) this.Alive.Remove(leavingPlayer);

                Log.Info(user.userName + " Left!");
                return;
            }

            
            Log.Error(user.userName + " Left - but they were not registered as a player!");

            orig(self, user);
        }
        
        void hook_OnPlayerCharacterDeath(On.RoR2.GlobalEventManager.orig_OnPlayerCharacterDeath orig, GlobalEventManager self, DamageReport damageReport, NetworkUser victimNetworkUser)
        {
            if (NetworkHelper.IsServer)
            {
                Player victim = FindByBodyId(victimNetworkUser.GetCurrentBody().netId);
                if (this.Alive.Contains(victim))
                {
                    PlayerDead(victim);
                    Log.Info(victimNetworkUser.userName + " Died!");
                }
                else
                {
                    Log.Error("Player Died but they were not alive to begin with!");
                }
            }

            orig(self, damageReport, victimNetworkUser);
        }
        
        void hook_OnBodyStart(On.RoR2.PlayerCharacterMasterController.orig_OnBodyStart orig, PlayerCharacterMasterController self)
        {
            orig(self);

            if (self.networkUser.isLocalPlayer)
            {
                this.CurrentUserPlayerCharacterMasterController = self;
                Log.Debug($"Set local player body to {this.CurrentUserBodyId} ({this.CurrentUserBodyId})");
            }

            if (NetworkHelper.IsClient())
                return;

            if (this.All.Any(p => p.master == self))
            {
                Log.Info($"BodyStart: {self.networkUser.userName} player already exists.");
                return;
            }

            Player p = new Player(self);
            if (this.config.GodMode)
            {
                p.GetBody().baseDamage = 120;
                p.networkUser.GetCurrentBody().baseMoveSpeed = 30;
                p.GetBody().baseAttackSpeed = 200;
            }

            this.Alive.Add(p);
            this.All.Add(p);
            p.isDead = false;
            
            this.numPlayersSetup++;
            Log.Info(self.networkUser.userName + " Setup");
            this.TotalCount = NetworkManager.singleton.numPlayers;
            this.playerSetupTimer = 0;

            if (this.numPlayersSetup == this.TotalCount)
            {
                this.Setup = true;
                Log.Info("All " + this.TotalCount + " Players Setup Successfully");
                OnSetupFinished?.Invoke();
            }
        }

        private void PlayerDead(Player p)
        {
            RemoveDuplicates();
            LogPlayers($"Player dead: {p.networkUser.userName}");
            if (this.Alive.Contains(p)) this.Alive.Remove(p);
            if(!this.Dead.Contains(p)) this.Dead.Add(p);
            p.isDead = true;
            p.reviveProgress = 0;
            
            if (NetworkHelper.IsClient()) return;
            this.OnPlayerDead?.Invoke(p);
        }
        
        public void PlayerAlive(Player p)
        {
            RemoveDuplicates();
            LogPlayers($"Player alive: {p.networkUser.userName}");
            if (!this.Alive.Contains(p)) this.Alive.Add(p);
            if (this.Dead.Contains(p)) this.Dead.Remove(p);
            p.isDead = false;
            p.reviveProgress = 0;
            NetworkServer.Destroy(p.deathTotem.gameObject);
            this.OnPlayerAlive?.Invoke(p);
        }

        // this is needed only during debug when reviving self
        [Conditional("DEBUG")]
        void RemoveDuplicates()
        {
            this.Alive = this.Alive.Distinct().ToList();
            this.Dead = this.Dead.Distinct().ToList();
        }

        void LogPlayers(string prefix)
        {
            var alives = string.Join(", ", this.Alive.Select(p => p.networkUser.userName));
            var deads = string.Join(", ", this.Dead.Select(p => p.networkUser.userName));
            Log.Info($"{prefix}; Alive: {alives} | Dead: {deads}");
        }

        public void Update()
        {
            if (NetworkHelper.IsServer && !this.Setup)
            {
                if (this.numPlayersSetup > 0)
                {
                    this.playerSetupTimer += Time.deltaTime;
                    if (this.playerSetupTimer >= 3)
                    {
                        this.Setup = true;
                        Log.Error("The " + this.TotalCount + " total players were not all setup, falling back. Consider filing an issue on Github.");
                    }
                }
            }
        }
    }
}