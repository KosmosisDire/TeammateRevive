using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using On.RoR2.Networking;
using R2API;
using R2API.Networking;
using R2API.Utils;
using RoR2;
using TeammateRevive.Artifact;
using TeammateRevive.Common;
using TeammateRevive.Configuration;
using TeammateRevive.Content;
using TeammateRevive.Debug;
using TeammateRevive.Integrations;
using TeammateRevive.Logging;
using TeammateRevive.Players;
using TeammateRevive.ProgressBar;
using TeammateRevive.Resources;
using TeammateRevive.Revive;
using TeammateRevive.Revive.Rules;
using TeammateRevive.Revive.Shrine;
using TeammateRevive.Skull;
using UnityEngine;
using UnityEngine.Networking;

namespace TeammateRevive
{
    [BepInDependency("com.bepis.r2api")]
    [BepInDependency("dev.ontrigger.itemstats", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.xoxfaby.BetterUI", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [R2APISubmoduleDependency(nameof(PrefabAPI), nameof(NetworkingAPI), nameof(BuffAPI), nameof(ItemAPI), nameof(ItemDropAPI), nameof(ArtifactAPI), nameof(LanguageAPI))]
    public class MainTeammateRevival : BaseUnityPlugin
    {
        #region Plugin variables
        
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "KosmosisDire";
        public const string PluginName = "TeammateRevival";
        public const string PluginVersion = "4.0.0";
        
        #endregion
        
        public static MainTeammateRevival instance;

        private DeathCurseArtifact deathCurseArtifact;

        private PluginConfig pluginConfig;
        private PlayersTracker players;
        private RunTracker run;
        private RevivalTracker revivalTracker;
        private ItemsStatsModIntegration itemsStatsModIntegration;
        private BetterUiModIntegration betterUiModIntegration;
        private ConsoleCommands consoleCommands;
        private ReviveRules rules;
        private ShrineManager shrineMan;
        private ReviveLinkBuffIconManager linkBuffIconManager;
        private SkullLongRangeActivationManager skullLongRangeActivationManager;
        private SkullTracker skullTracker;
        private ReviveProgressBarTracker progressBarTracker;

        private List<ContentBase> AddedContent = new();

        #region Setup

        public void Awake()
        {
            instance = this;
            this.pluginConfig = PluginConfig.Load(this.Config);
            
            NetworkingAPI.RegisterMessageType<SyncSkullMessage>();
            NetworkingAPI.RegisterMessageType<SetRulesMessage>();

            this.deathCurseArtifact = new DeathCurseArtifact();
            this.run = new RunTracker(this.deathCurseArtifact);
            this.players = new PlayersTracker(this.run, this.pluginConfig);
            this.rules = new ReviveRules(this.run);
            this.skullTracker = new SkullTracker();
            this.progressBarTracker = new ReviveProgressBarTracker(new ProgressBarController(), this.players,
                this.skullTracker, this.rules);
            this.revivalTracker = new RevivalTracker(this.players, this.run, this.rules, this.skullTracker, this.progressBarTracker);
            
            this.itemsStatsModIntegration = new ItemsStatsModIntegration(this.rules);
            this.betterUiModIntegration = new BetterUiModIntegration();
            this.consoleCommands = new ConsoleCommands(this.rules, this.pluginConfig);
            this.shrineMan = new ShrineManager(this.run, this.rules);
            this.linkBuffIconManager = new ReviveLinkBuffIconManager();
            this.skullLongRangeActivationManager = new SkullLongRangeActivationManager(this.run, this.skullTracker);
            
            Log.Init(this.pluginConfig, this.Logger);
            AddedAssets.Init();
            LoadAddedContent();
            this.deathCurseArtifact.Init(this.Config);
            this.rules.ApplyConfigValues(this.pluginConfig);
#if DEBUG
            DebugHelper.Init(this.pluginConfig);
#endif
            this.run.RunStarted += OnRunStarted;
            
            SetupHooks();

            Log.Debug("Setup Teammate Revival");
        }

        public void LoadAddedContent()
        {
            this.AddedContent = new List<ContentBase>
            {
                new CharonsObol(),
                new DeathCurse(this.rules, this.run),
                new ReviveEverywhereItem(),
                new ReviveLink(),
                new ReviveRegen(this.rules)
            };
            
            foreach (var content in this.AddedContent)
            {
                content.Init();
                content.GetType().GetField("instance")
                    ?.SetValue(null, content);
            }
        }

        void SetupHooks()
        {
            On.RoR2.Run.BeginGameOver += hook_BeginGameOver;
            On.RoR2.Run.AdvanceStage += hook_AdvanceStage;
            On.RoR2.Networking.GameNetworkManager.OnStartClient += OnStartClient;
            On.RoR2.NetworkUser.OnStartLocalPlayer += hook_OnStartLocalPlayer;
            On.RoR2.Run.BeginStage += hook_BeginStage;
        }

        private void OnStartClient(GameNetworkManager.orig_OnStartClient orig, RoR2.Networking.GameNetworkManager self, NetworkClient newclient)
        {
            ClientScene.RegisterPrefab(AddedAssets.ShrinePrefab);
            FindObjectOfType<NetworkManager>().spawnPrefabs.Add(AddedAssets.ShrinePrefab);
            
            orig(self, newclient);
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

        void OnRunStarted(RunTracker obj)
        {
            // disable artifact if single player
            if (Run.instance.participatingPlayerCount == 1
                && RunArtifactManager.instance.IsArtifactEnabled(this.deathCurseArtifact.ArtifactDef))
            {
                RunArtifactManager.instance.SetArtifactEnabledServer(this.deathCurseArtifact.ArtifactDef, false);
                Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                {
                    baseToken = TextFormatter.Yellow("Artifact of Death Curse is disabled because run started in single player!.")
                });
                return;
            }
            
            // enforce artifact if needed
            if (
                Run.instance.participatingPlayerCount > 1
                && this.rules.Values.ForceDeathCurseRule
                && !this.deathCurseArtifact.ArtifactEnabled
                && NetworkHelper.IsServer
            ) {
                var message = "Artifact of Death Curse is enforced by server.";
                RunArtifactManager.instance.SetArtifactEnabledServer(this.deathCurseArtifact.ArtifactDef, true);
                Log.Info(message);
                Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                {
                    baseToken = TextFormatter.Yellow(message)
                });
            }
        }

        public Func<IEnumerator, Coroutine> DoCoroutine => StartCoroutine;
    }
}
