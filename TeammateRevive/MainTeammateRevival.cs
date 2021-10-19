using System;
using System.Collections;
using BepInEx;
using R2API;
using R2API.Networking;
using R2API.Utils;
using RoR2;
using TeammateRevive.Common;
using TeammateRevive.Configuration;
using TeammateRevive.Debug;
using TeammateRevive.Integrations;
using TeammateRevive.Logging;
using TeammateRevive.Players;
using TeammateRevive.Resources;
using TeammateRevive.Revive;
using TeammateRevive.Revive.Rules;
using TeammateRevive.Skull;
using UnityEngine;
using UnityEngine.Networking;

namespace TeammateRevive
{
    [BepInDependency("com.bepis.r2api")]
    [BepInDependency("dev.ontrigger.itemstats", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.xoxfaby.BetterUI", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [R2APISubmoduleDependency(nameof(PrefabAPI), nameof(NetworkingAPI), nameof(BuffAPI), nameof(ItemAPI), nameof(ItemDropAPI))]
    public class MainTeammateRevival : BaseUnityPlugin
    {
        #region Plugin variables
        
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "KosmosisDire";
        public const string PluginName = "TeammateRevival";
        public const string PluginVersion = "4.0.0";
        
        #endregion
        
        public static MainTeammateRevival instance;

        private PluginConfig pluginConfig;
        private PlayersTracker players;
        private RunTracker run;
        private RevivalTracker revivalTracker;
        private ItemsStatsModIntegration itemsStatsModIntegration;
        private BetterUiModIntegration betterUiModIntegration;
        private ConsoleCommands consoleCommands;
        private ReviveRules rules;

        #region Setup

        public void Awake()
        {
            instance = this;
            this.pluginConfig = PluginConfig.Load(this.Config);
            
            NetworkingAPI.RegisterMessageType<SyncSkullMessage>();
            NetworkingAPI.RegisterMessageType<SetRulesMessage>();
            
            this.run = new RunTracker();
            this.players = new PlayersTracker(this.run, this.pluginConfig);
            this.rules = new ReviveRules(this.run);
            this.revivalTracker = new RevivalTracker(this.players, this.run, this.rules);
            this.itemsStatsModIntegration = new ItemsStatsModIntegration(this.rules);
            this.betterUiModIntegration = new BetterUiModIntegration();
            this.consoleCommands = new ConsoleCommands(this.rules, this.pluginConfig);
            
            Log.Init(this.pluginConfig, this.Logger);
            AddedAssets.Init();
            ItemsAndBuffs.Init();
            this.revivalTracker.Init();
            this.rules.ApplyConfigValues(this.pluginConfig);
#if DEBUG
            DebugHelper.Init(this.pluginConfig);
#endif
            
            SetupHooks();

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
            this.run.IsStarted = true;
        }

        void hook_OnStartLocalPlayer(On.RoR2.NetworkUser.orig_OnStartLocalPlayer orig, NetworkUser self)
        {
            orig(self);

            if (NetworkHelper.IsClient())
            {
                ClientScene.RegisterPrefab(AddedAssets.DeathMarker);
                FindObjectOfType<NetworkManager>().spawnPrefabs.Add(AddedAssets.DeathMarker);
                Log.Info("Client Registered Prefabs");
                return;
            }

            NetworkManager.singleton.connectionConfig.DisconnectTimeout = 5;
            NetworkManager.singleton.connectionConfig.MaxSentMessageQueueSize = 1024;
        }

        void hook_BeginGameOver(On.RoR2.Run.orig_BeginGameOver orig, Run self, GameEndingDef gameEndingDef)
        {
            orig(self, gameEndingDef);

            if (NetworkHelper.IsClient()) return;

            Log.Info("Game Over - reseting data");

            this.players.Reset();
            this.run.IsStarted = false;
        }

        void hook_AdvanceStage(On.RoR2.Run.orig_AdvanceStage orig, Run self, SceneDef nextScene)
        {
            orig(self, nextScene);

            if (NetworkHelper.IsClient()) return;

            Log.Info("Advanced a stage - now resetting");
            this.players.Reset();
        }
        
        #endregion

        public void Update()
        {
#if DEBUG
            DebugHelper.Update();
#endif
            this.players.Update();
            this.revivalTracker.Update();
        }

        public Func<IEnumerator, Coroutine> DoCoroutine => StartCoroutine;
    }
}
