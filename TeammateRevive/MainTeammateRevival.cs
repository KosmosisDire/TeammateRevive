using System;
using System.Collections;
using BepInEx;
using R2API;
using R2API.Networking;
using R2API.Utils;
using RoR2;
using TeammateRevive.Artifact;
using TeammateRevive.Common;
using TeammateRevive.Configuration;
using TeammateRevive.Debugging;
using TeammateRevive.Integrations;
using TeammateRevive.Logging;
using TeammateRevive.Players;
using TeammateRevive.ProgressBar;
using TeammateRevive.Resources;
using TeammateRevive.Revive;
using TeammateRevive.Revive.Rules;
using TeammateRevive.DeathTotem;
using UnityEngine;
using UnityEngine.Networking;

namespace TeammateRevive
{
    [BepInDependency("com.bepis.r2api")]
    [BepInDependency("dev.ontrigger.itemstats", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.xoxfaby.BetterUI", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.KingEnderBrine.InLobbyConfig", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [R2APISubmoduleDependency(nameof(PrefabAPI), nameof(NetworkingAPI), nameof(ItemAPI), nameof(LanguageAPI))]
    public class MainTeammateRevival : BaseUnityPlugin
    {
        #region Plugin variables
        
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "KosmosisDire";
        public const string PluginName = "TeammateRevival";
        public const string PluginVersion = "4.1.1";
        
        #endregion
        
        public static MainTeammateRevival instance;

        private DeathCurseArtifact deathCurseArtifact;

        public PluginConfig pluginConfig;
        private PlayersTracker players;
        private RunTracker run;
        private RevivalTracker revivalTracker;
        private ItemsStatsModIntegration itemsStatsModIntegration;
        private BetterUiModIntegration betterUiModIntegration;
        private ConsoleCommands consoleCommands;
        private ReviveRules rules;
        private ReviveLinkBuffIconManager linkBuffIconManager;
        private ReviveLongRangeActivationManager reviveLongRangeActivationManager;
        private InLobbyConfigIntegration inLobbyConfigIntegration;
        private DeathTotemTracker deathTotemTracker;
        private ReviveProgressBarTracker progressBarTracker;
        private ItemDropManager itemDropManager;
        private ContentManager contentManager;

        #region Setup

        public void Awake()
        {
            instance = this;
            this.pluginConfig = PluginConfig.Load(this.Config);
            
            NetworkingAPI.RegisterMessageType<SyncDeathTotemMessage>();
            NetworkingAPI.RegisterMessageType<SetRulesMessage>();

            this.deathCurseArtifact = new DeathCurseArtifact();
            this.run = new RunTracker(this.deathCurseArtifact);
            this.players = new PlayersTracker(this.run, this.pluginConfig);
            this.rules = new ReviveRules(this.run, this.pluginConfig);
            this.deathTotemTracker = new DeathTotemTracker(this.players, this.run, this.rules);
            this.progressBarTracker = new ReviveProgressBarTracker(new ProgressBarController(), this.players,
                this.deathTotemTracker, this.rules);
            this.revivalTracker = new RevivalTracker(this.players, this.run, this.rules, this.deathTotemTracker, this.progressBarTracker);
            
            this.itemsStatsModIntegration = new ItemsStatsModIntegration(this.rules);
            this.betterUiModIntegration = new BetterUiModIntegration();
            this.consoleCommands = new ConsoleCommands(this.rules, this.pluginConfig);
            this.linkBuffIconManager = new ReviveLinkBuffIconManager();
            this.inLobbyConfigIntegration = new InLobbyConfigIntegration(this.pluginConfig);
            this.reviveLongRangeActivationManager = new ReviveLongRangeActivationManager(this.run, this.deathTotemTracker);
            this.itemDropManager = new ItemDropManager(this.run, this.rules);
            this.contentManager = new ContentManager(this.rules, this.run, this.deathCurseArtifact);
            
            Log.Init(this.pluginConfig, this.Logger);
            ReviveHelper.Init();
            HideDeathCurseContent.Init(this.pluginConfig);
            this.contentManager.Init();
            this.rules.ApplyConfigValues();
#if DEBUG
            DebugHelper.Init(this.pluginConfig);
#endif
            this.run.RunStarted += OnRunStarted;
            this.run.RunEnded += OnRunEnded;
            
            SetupHooks();

            Log.Debug("Setup Teammate Revival");
        }

        void SetupHooks()
        {
            On.RoR2.Run.BeginGameOver += hook_BeginGameOver;
            On.RoR2.Run.AdvanceStage += hook_AdvanceStage;
            On.RoR2.NetworkUser.OnStartLocalPlayer += hook_OnStartLocalPlayer;
        }

        #endregion

        #region Hooks

        void hook_OnStartLocalPlayer(On.RoR2.NetworkUser.orig_OnStartLocalPlayer orig, NetworkUser self)
        {
            orig(self);

            if (NetworkHelper.IsClient())
            {
                ClientScene.RegisterPrefab(CustomResources.DeathTotem.gameObject);
                FindObjectOfType<NetworkManager>().spawnPrefabs.Add(CustomResources.DeathTotem.gameObject);
                
                Log.Info("Client registered prefabs");
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

        void OnRunStarted(RunTracker obj)
        {
            this.deathCurseArtifact.EnsureEnabled(this.rules);
        }

        private void OnRunEnded(RunTracker obj)
        {
            this.players.Reset();
        }

        public Func<IEnumerator, Coroutine> DoCoroutine => StartCoroutine;
    }
}
