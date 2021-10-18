using System;
using System.Collections;
using BepInEx;
using R2API;
using R2API.Networking;
using R2API.Utils;
using RoR2;
using TeammateRevive.Configuration;
using TeammateRevive.Debug;
using TeammateRevive.Integrations;
using TeammateRevive.Logging;
using TeammateRevive.Players;
using TeammateRevive.Resources;
using TeammateRevive.Revive;
using TeammateRevive.Skull;
using UnityEngine;
using UnityEngine.Networking;

namespace TeammateRevive
{
    [BepInDependency("com.bepis.r2api")]
    [BepInDependency("dev.ontrigger.itemstats", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [R2APISubmoduleDependency(nameof(PrefabAPI), nameof(NetworkingAPI), nameof(BuffAPI), nameof(ItemAPI))]
    public class MainTeammateRevival : BaseUnityPlugin
    {
        #region Variables
        
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "KosmosisDire";
        public const string PluginName = "TeammateRevival";
        public const string PluginVersion = "3.3.8";
        
        public static MainTeammateRevival instance;

        private PluginConfig pluginConfig;
        private PlayersTracker players;
        private RunTracker runTracker;
        private RevivalTracker revivalTracker { get; set; }
        private ItemsStatsModIntegration itemsStatsModIntegration;

        #endregion

        #region Setup

        public void Awake()
        {
            instance = this;
            this.pluginConfig = PluginConfig.Load(this.Config);
            
            this.runTracker = new RunTracker();
            this.players = new PlayersTracker(this.runTracker, this.pluginConfig);
            this.revivalTracker = new RevivalTracker(this.pluginConfig, this.players, this.runTracker);
            this.itemsStatsModIntegration = new ItemsStatsModIntegration(this.pluginConfig);
            
            Log.Init(this.pluginConfig, this.Logger);
            AddedResources.Init();
            this.revivalTracker.Init();
#if DEBUG
            DebugHelper.Init();
#endif
            
            SetupHooks();

            NetworkingAPI.RegisterMessageType<SyncSkull>();
            Log.Debug("Setup Teammate Revival");
        }

        void SetupHooks()
        {
            On.RoR2.Run.BeginGameOver += hook_BeginGameOver;
            On.RoR2.Run.AdvanceStage += hook_AdvanceStage;
            On.RoR2.NetworkUser.OnStartLocalPlayer += hook_OnStartLocalPlayer;
            On.RoR2.Run.BeginStage += hook_BeginStage;
        }


        #endregion

        #region Hooks

        void hook_BeginStage(On.RoR2.Run.orig_BeginStage orig, Run self)
        {
            orig(self);
            this.runTracker.IsStarted = true;
        }

        void hook_OnStartLocalPlayer(On.RoR2.NetworkUser.orig_OnStartLocalPlayer orig, NetworkUser self)
        {
            orig(self);

            if (IsClient())
            {
                ClientScene.RegisterPrefab(AddedResources.DeathMarker);
                FindObjectOfType<NetworkManager>().spawnPrefabs.Add(AddedResources.DeathMarker);
                Log.Info("Client Registered Prefabs");
                return;
            }
            

            NetworkManager.singleton.connectionConfig.DisconnectTimeout = 5;
            NetworkManager.singleton.connectionConfig.MaxSentMessageQueueSize = 1024;
        }

        void hook_BeginGameOver(On.RoR2.Run.orig_BeginGameOver orig, Run self, GameEndingDef gameEndingDef)
        {
            orig(self, gameEndingDef);

            if (IsClient()) return;

            Log.Info("Game Over - reseting data");

            this.players.Reset();
            this.runTracker.IsStarted = false;
        }

        void hook_AdvanceStage(On.RoR2.Run.orig_AdvanceStage orig, Run self, SceneDef nextScene)
        {
            orig(self, nextScene);

            if (IsClient()) return;

            Log.Info("Advanced a stage - now resetting");
            this.players.Reset();
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

        public static Vector3 GroundPosition(Player player)
        {
            RaycastHit hit;
            if (Physics.Raycast(player.GetBody().transform.position, Vector3.down, out hit, 1000, LayerMask.GetMask("World")))
            {
                if (Vector3.Dot(hit.normal, Vector3.up) > 0.5f && Vector3.Distance(player.GetBody().transform.position, player.groundPosition) > Vector3.Distance(player.GetBody().transform.position, hit.point))
                {
                    return hit.point;
                }
            }
            
            return player.groundPosition;
        }

        #endregion

        public void Update()
        {
#if DEBUG
            DebugHelper.Update();
#endif
            this.players.UpdateSetup();
            this.revivalTracker.Update();
        }

        public Func<IEnumerator, Coroutine> DoCoroutine => StartCoroutine;
    }
}
