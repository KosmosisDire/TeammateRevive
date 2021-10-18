using System;
using System.Collections.Generic;
using RoR2;
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

        public PlayerCharacterMasterController CurrentUserPlayerCharacterMasterController { get; set; }
        public NetworkInstanceId? CurrentUserBodyId => this.CurrentUserPlayerCharacterMasterController?.master.GetBody()?.netId;

        public readonly List<Player> Alive = new();
        public readonly List<Player> Dead = new();
        public readonly List<Player> All = new();
        
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
            if (MainTeammateRevival.IsClient()) return;

            if (!this.Dead.Contains(player)) return;

            bool playerConnected = player.master.isConnected;
            if (playerConnected)
            {
                player.master.master.RespawnExtraLife();
                PlayerAlive(player);
            }
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
            if (MainTeammateRevival.IsClient())
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
            if (MainTeammateRevival.IsServer)
            {
                Player victim = FindByBodyId(victimNetworkUser.GetCurrentBody().netId);
                if (this.Alive.Contains(victim))
                {
                    PlayerDead(victim);
                    Log.Info(victimNetworkUser.userName + " Died!");
                    return;
                }

                Log.Error("Player Died but they were not alive to begin with!");
            }

            orig(self, damageReport, victimNetworkUser);
        }
        
        void hook_OnBodyStart(On.RoR2.PlayerCharacterMasterController.orig_OnBodyStart orig, PlayerCharacterMasterController self)
        {
            orig(self);


            if (self.networkUser.isLocalPlayer)
            {
                this.CurrentUserPlayerCharacterMasterController = self;
                // this.CurrentUserBody = self.master.GetBody();
                Log.Debug($"Set local player body to {this.CurrentUserBodyId} ({this.CurrentUserBodyId})");
            }

            if (MainTeammateRevival.IsClient())
                return;

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
            // if (DamageNumberManager.instance == null) TotalPlayers--;
            this.playerSetupTimer = 0;

            if (this.numPlayersSetup == this.TotalCount)
            {
                this.Setup = true;
                Log.Info("All " + this.TotalCount + " Players Setup Successfully");
            }
        }


        private void PlayerDead(Player p)
        {
            Log.Debug($"Player dead: {p.networkUser.userName}| {this.Alive.Contains(p)} {this.Dead.Contains(p)}");
            if (this.Alive.Contains(p)) this.Alive.Remove(p);
            if(!this.Dead.Contains(p)) this.Dead.Add(p);
            p.isDead = true;
            p.rechargedHealth = 0;
            
            if (MainTeammateRevival.IsClient()) return;
            this.OnPlayerDead?.Invoke(p);
        }
        
        public void PlayerAlive(Player p)
        {
            Log.Debug($"Player alive: {p.networkUser.userName}| {this.Alive.Contains(p)} {this.Dead.Contains(p)}");
            if (!this.Alive.Contains(p)) this.Alive.Add(p);
            if (this.Dead.Contains(p)) this.Dead.Remove(p);
            p.isDead = false;
            p.rechargedHealth = 0;
            NetworkServer.Destroy(p.skull.gameObject);
        }

        public void UpdateSetup()
        {
            if (MainTeammateRevival.IsServer && !this.Setup)
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