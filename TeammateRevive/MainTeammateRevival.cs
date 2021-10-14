using System;
using BepInEx;
using R2API;
using R2API.Networking;
using R2API.Utils;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RoR2.Orbs;
using TeammateRevival.Logging;
using TeammateRevival.RevivalStrategies;
using TeammateRevive.RevivalStrategies.ReduceMaxHp;
using UnityEngine;
using UnityEngine.Networking;

namespace TeammateRevival
{
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [R2APISubmoduleDependency(nameof(PrefabAPI), nameof(NetworkingAPI), nameof(BuffAPI), nameof(ItemAPI))]
    public class MainTeammateRevival : BaseUnityPlugin
    {
        #region Variables
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "KosmosisDire";
        public const string PluginName = "TeammateRevival";
        public const string PluginVersion = "3.3.8";

        //debugging config
        public static PluginConfig PluginConfig;

        float smallestMax = float.PositiveInfinity;
        float threshold = 0;
        int numPlayersSetup = 0;
        float playerSetupTimer = 3;

        public static bool playersSetup = false;
        public int TotalPlayers { get; private set; }
        
        public List<Player> AlivePlayers = new();
        public List<Player> DeadPlayers = new();
        public List<Player> AllPlayers = new();
        
        public static bool runStarted;

        public GameObject DeathMarker { get; private set; }
        public GameObject DeathMarkerPrefab { get; private set; }
        private static List<Material> materials = new();

        //configurable variables
        public static MainTeammateRevival instance;

        public IRevivalStrategy RevivalStrategy { get; set; }

        #endregion

        #region Setup

        public void Awake()
        {
            instance = this;
            PluginConfig = PluginConfig.Load(Config);
            SetupHooks();
            Log.Init(PluginConfig, Logger);
            this.RevivalStrategy = PluginConfig.ReviveStrategy switch {
                ReviveStrategy.DamageInRange => new DamageInRageRevivalStrategy(this),
                ReviveStrategy.ReduceMaxHp => new ReduceMaxHpRevivalStrategy(this),
                _ => new DamageInRageRevivalStrategy(this),
            };
            Log.Debug("Strategy " + PluginConfig.ReviveStrategy);
            this.RevivalStrategy.Init();
            LoadSkullPrefab();

            Log.Debug("Awake.RegisterMessageType");
            NetworkingAPI.RegisterMessageType<SyncSkull>();
            Log.Debug("Setup Teammate Revival");
        }

        void LoadSkullPrefab()
        {
            Log.Debug("Awake.customprefabs");
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TeammateRevive.customprefabs"))
            {
                var bundle = AssetBundle.LoadFromStream(stream);
                var materialsL = bundle.LoadAllAssets<Material>();
                foreach (Material material in materialsL)
                {
                    if (material.shader.name.StartsWith("StubbedShader"))
                    {
                        material.shader = Resources.Load<Shader>("shaders" + material.shader.name.Substring(13));
                        materials.Add(material);
                    }
                }

                var dm = bundle.LoadAsset<GameObject>("Assets/PlayerDeathPoint.prefab");
                DeathMarkerPrefab = bundle.LoadAsset<GameObject>("Assets/PlayerDeathPoint.prefab");
                dm.AddComponent<DeadPlayerSkull>();
                DeathMarker = PrefabAPI.InstantiateClone(dm, "Death Marker");
                dm.GetComponent<DeadPlayerSkull>().Setup();
                dm.GetComponent<DeadPlayerSkull>().radiusSphere.material = materials[0];

                bundle.Unload(false);
            }
        }
        
        void ResetSetup()
        {
            smallestMax = float.PositiveInfinity;
            threshold = 0;
            playersSetup = false;
            AlivePlayers.Clear();
            DeadPlayers.Clear();
            AllPlayers.Clear();
            numPlayersSetup = 0;
            TotalPlayers = 0;
            playerSetupTimer = 0;
            Log.Info("Reset Data");
        }

        void SetupHooks()
        {
            On.RoR2.Run.OnUserAdded += hook_OnUserAdded;
            On.RoR2.Run.OnUserRemoved += hook_OnUserRemoved;
            On.RoR2.GlobalEventManager.OnPlayerCharacterDeath += hook_OnPlayerCharacterDeath;
            On.RoR2.Run.BeginGameOver += hook_BeginGameOver;
            On.RoR2.Run.AdvanceStage += hook_AdvanceStage;
            On.RoR2.PlayerCharacterMasterController.OnBodyStart += hook_OnBodyStart;
            On.RoR2.NetworkUser.OnStartLocalPlayer += hook_OnStartLocalPlayer;
            On.RoR2.Run.BeginStage += hook_BeginStage;
        }


        #endregion

        #region Hooks

        void hook_BeginStage(On.RoR2.Run.orig_BeginStage orig, Run self)
        {
            orig(self);
            runStarted = true;
        }

        void hook_OnStartLocalPlayer(On.RoR2.NetworkUser.orig_OnStartLocalPlayer orig, NetworkUser self)
        {
            orig(self);

            if (IsClient())
            {
                ClientScene.RegisterPrefab(DeathMarker);
                FindObjectOfType<NetworkManager>().spawnPrefabs.Add(DeathMarker);
                Log.Info("Client Registered Prefabs");
                return;
            }
            

            NetworkManager.singleton.connectionConfig.DisconnectTimeout = 5;
            NetworkManager.singleton.connectionConfig.MaxSentMessageQueueSize = 1024;
        }

        void hook_OnUserAdded(On.RoR2.Run.orig_OnUserAdded orig, Run self, NetworkUser user)
        {
            orig(self, user);
            if (IsClient()) return;
            Log.Info(user.userName + " added.");
        }

        void hook_OnUserRemoved(On.RoR2.Run.orig_OnUserRemoved orig, Run self, NetworkUser user)
        {
            if (IsClient())
            {
                orig(self, user);
                return;
            }

            if (!runStarted) 
            {
                Log.Info(user.userName + " left while run wasn't in session.");
            }

            Player leavingPlayer = FindPlayerFromPlayerCharacterMasterControllerInstanceID(user.masterController.netId);
            if (AllPlayers.Contains(leavingPlayer))
            {
                AllPlayers.Remove(leavingPlayer);
                if (DeadPlayers.Contains(leavingPlayer)) DeadPlayers.Remove(leavingPlayer);
                if (this.AlivePlayers.Contains(leavingPlayer)) this.AlivePlayers.Remove(leavingPlayer);

                Log.Info(user.userName + " Left!");
                return;
            }

            
            Log.Error(user.userName + " Left - but they were not registered as a player!");

            orig(self, user);
        }

        void hook_OnBodyStart(On.RoR2.PlayerCharacterMasterController.orig_OnBodyStart orig, PlayerCharacterMasterController self)
        {
            orig(self);

            if (IsClient()) return;
            
            Player p = new Player(self);
            if (PluginConfig.GodMode)
            {
                p.GetBody().baseDamage = 120;
                p.networkUser.GetCurrentBody().baseMoveSpeed = 30;
                p.GetBody().baseAttackSpeed = 200;
            }

            this.AlivePlayers.Add(p);
            AllPlayers.Add(p);
            p.isDead = false;
            
            numPlayersSetup++;
            Log.Info(self.networkUser.userName + " Setup");
            TotalPlayers = NetworkManager.singleton.numPlayers;
            if (DamageNumberManager.instance == null) TotalPlayers--;
            playerSetupTimer = 0;

            if (numPlayersSetup == TotalPlayers)
            {
                playersSetup = true;
                Log.Info("All " + TotalPlayers + " Players Setup Successfully");
            }
        }

        void hook_BeginGameOver(On.RoR2.Run.orig_BeginGameOver orig, Run self, GameEndingDef gameEndingDef)
        {
            orig(self, gameEndingDef);

            if (IsClient()) return;

            Log.Info("Game Over - reseting data");

            ResetSetup();
            TotalPlayers = 0;
            runStarted = false;
        }

        void hook_AdvanceStage(On.RoR2.Run.orig_AdvanceStage orig, Run self, SceneDef nextScene)
        {
            orig(self, nextScene);

            if (IsClient()) return;

            Log.Info("Advanced a stage - now resetting");
            ResetSetup();
        }

        void hook_OnPlayerCharacterDeath(On.RoR2.GlobalEventManager.orig_OnPlayerCharacterDeath orig, GlobalEventManager self, DamageReport damageReport, NetworkUser victimNetworkUser)
        {
            if (!IsClient())
            {
                Player victim = FindPlayerFromBodyInstanceID(victimNetworkUser.GetCurrentBody().netId);
                if (this.AlivePlayers.Contains(victim))
                {
                    PlayerDead(victim);
                    Log.Info(victimNetworkUser.userName + " Died!");
                    return;
                }

                Log.Error("Player Died but they were not alive to begin with!");
            }

            orig(self, damageReport, victimNetworkUser);
        }

        #endregion

        #region Helpers

        public static bool IsServer => !IsClient();
        
        public static bool IsClient()
        {
            if (RoR2.RoR2Application.isInSinglePlayer || !NetworkServer.active)
            {
                return true;
            }

            return false;
        }

        public void PlayerDead(Player p)
        {
            if (this.AlivePlayers.Contains(p)) this.AlivePlayers.Remove(p);
            if(!DeadPlayers.Contains(p)) DeadPlayers.Add(p);
            p.isDead = true;
            p.rechargedHealth = 0;
            
            if (IsClient()) return;
            this.RevivalStrategy.ServerSpawnSkull(p);
        }

        public void PlayerAlive(Player p)
        {
            if (!this.AlivePlayers.Contains(p)) this.AlivePlayers.Add(p);
            if (DeadPlayers.Contains(p)) DeadPlayers.Remove(p);
            p.isDead = false;
            p.rechargedHealth = 0;
            NetworkServer.Destroy(p.deathMark.gameObject);
        }

        public void RespawnPlayer(Player player)
        {
            if (IsClient()) return;

            if (!DeadPlayers.Contains(player)) return;

            bool playerConnected = player.master.isConnected;
            if (playerConnected)
            {
                player.master.master.RespawnExtraLife();
                PlayerAlive(player);
            }
            Log.Info("Player Respawned");
        }

        void CalculateReviveThreshold()
        {
            //find smallest max health out of all the players
            smallestMax = Mathf.Infinity;
            for (int i = 0; i < this.AlivePlayers.Count; i++)
            {
                Player player = this.AlivePlayers[i];

                if (player.CheckDead()) continue;

                if (this.AlivePlayers[i].GetBody().maxHealth < smallestMax)
                    smallestMax = (int)this.AlivePlayers[i].GetBody().maxHealth;
            }
            //the player must give this much health to revive the other player
            threshold = (smallestMax * 0.9f);
        }

        public static Vector3 GroundPosition(Player player)
        {
            RaycastHit hit;
            if (Physics.Raycast(player.GetBody().transform.position, Vector3.down, out hit, 1000, LayerMask.GetMask(new string[] { "World" })))
            {
                if (Vector3.Dot(hit.normal, Vector3.up) > 0.5f && Vector3.Distance(player.GetBody().transform.position, player.groundPosition) > Vector3.Distance(player.GetBody().transform.position, hit.point))
                {
                    return hit.point;
                }
            }
            
            return player.groundPosition;
        }
        
        public Player FindPlayerFromBodyInstanceID(NetworkInstanceId id) 
        {
            foreach (var p in this.AlivePlayers)
            {
                p.GetBody();
                if (p.bodyID == id) return p;
            }

            return null;
        }

        public Player FindPlayerFromPlayerCharacterMasterControllerInstanceID(NetworkInstanceId id) 
        {
            foreach (var p in AllPlayers) 
            {
                if(p.master && p.master.netId == id)
                {
                    return p;
                }
            }
            return null;
        }

        #endregion

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.F3)) this.AllPlayers[0].GetBody().healthComponent.Networkhealth = 1;
            if (Input.GetKeyDown(KeyCode.F4)) this.AllPlayers[1].GetBody().healthComponent.Networkhealth = 1;
            
            if (Input.GetKeyDown(KeyCode.F6)) NetworkUser.readOnlyInstancesList.ToList().ForEach(u => u.master.inventory.GiveItem(AddedResources.ReduceHpItemIndex));
            if (Input.GetKeyDown(KeyCode.F7)) NetworkUser.readOnlyInstancesList.ToList().ForEach(u => u.master.inventory.RemoveItem(AddedResources.ReduceHpItemIndex));
            
            if (Input.GetKeyDown(KeyCode.F8)) NetworkUser.readOnlyInstancesList.ToList().ForEach(u => ReduceMaxHpRevivalStrategy.SendObol(u));

            if (IsClient()) return;

            if (!playersSetup) 
            {
                if (numPlayersSetup > 0)
                {
                    playerSetupTimer += Time.deltaTime;
                    if (playerSetupTimer >= 3)
                    {
                        playersSetup = true;
                        Log.Error("The " + TotalPlayers + " total players were not all setup, falling back. Consider filing an issue on Github.");
                    }
                }
                return;
            }

            //if (Input.GetKeyDown(KeyCode.F2)) 
            //{
            //    SpawnDeathVisuals(alivePlayers[0]);
            //}

            CalculateReviveThreshold();

            //interactions between dead and alive players
            foreach (var player in this.AlivePlayers)
            {
                if (player.CheckDead()) continue;

                player.groundPosition = GroundPosition(player);

                foreach (var dead in DeadPlayers)
                {
                    //have they been revived by other means?
                    if (dead.CheckAlive()) 
                    {
                        PlayerAlive(dead);
                        continue;
                    }
                    
                    this.RevivalStrategy.Update(player, dead);
                }
            }
        }
    }
}
