using BepInEx;
using BepInEx.Configuration;
using R2API;
using R2API.Utils;
using RoR2;
using System.Collections.Generic;
using UnityEngine;
using static RoR2.Chat;

namespace TeammateRevive
{
    [BepInDependency("com.bepis.r2api")]
	
	//This attribute is required, and lists metadata for your plugin.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
	
	//We will be using 3 modules from R2API: ItemAPI to add our item, ItemDropAPI to have our item drop ingame, and LanguageAPI to add our language tokens.
    [R2APISubmoduleDependency(nameof(ItemAPI), nameof(ItemDropAPI), nameof(LanguageAPI))]
	
    public class TeammateRevive : BaseUnityPlugin
	{
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "KosmosisDire";
        public const string PluginName = "TeammateRevival";
        public const string PluginVersion = "1.0.4";


        public PlayerCharacterMasterController player;
        private static List<PlayerCharacterMasterController> deadPlayers = new List<PlayerCharacterMasterController>();

        //config entries
        public static ConfigEntry<float> helpDistance { get; set; }
        public static ConfigEntry<float> helpTime { get; set; }

        
        public void Awake() 
        {
            Log.Init(Logger);
            InitConfig();
            SetupHooks();
            Logger.LogInfo(" ------------------- Setup Teammate Revival -------------------");
        }

        void SetupHooks() 
        {
            On.RoR2.UnitySystemConsoleRedirector.Redirect += orig => { };
            On.RoR2.GlobalEventManager.OnPlayerCharacterDeath += GlobalEventManager_OnPlayerCharacterDeath;
            On.RoR2.Run.Start += RunStart;
        }

        public void RunStart(On.RoR2.Run.orig_Start orig, Run self)
        {
            orig(self);
            player = PlayerCharacterMasterController.instances[0];
        }

        public void RespawnChar(PlayerCharacterMasterController player)
        {
            if (!deadPlayers.Contains(player)) return;


            bool playerConnected = player.isConnected;
            bool isDead = !player.master.GetBody()
                        || player.master.IsDeadAndOutOfLivesServer()
                        || !player.master.GetBody().healthComponent.alive;
            if (playerConnected && isDead)
            {
                player.master.RespawnExtraLife();
                deadPlayers.Remove(player);
                Logger.LogInfo("character revived");
            }
            
            return;
        }

        private void GlobalEventManager_OnPlayerCharacterDeath(On.RoR2.GlobalEventManager.orig_OnPlayerCharacterDeath orig, GlobalEventManager self, DamageReport damageReport, NetworkUser victimNetworkUser)
        {
            orig(self, damageReport, victimNetworkUser);
            Logger.LogInfo(damageReport.victim.name + " has died");
            deadPlayers.Add(victimNetworkUser.masterController);
        }

        PlayerCharacterMasterController helping = null;
        float timer = 0;

        //The Update() method is run on every frame of the game.
        private void Update()
        {
            
            if (deadPlayers.Count == 0) return;
            if (helping != null)
            {
                Logger.LogInfo(timer);
                player.bodyInputs = new InputBankTest();
                timer += Time.deltaTime;
                if (timer >= helpTime.Value)
                {
                    Logger.LogInfo("Reviving character");
                    RespawnChar(helping);
                    helping = null;
                    timer = 0;
                }
                return;
            }

            foreach (var dead in deadPlayers)
            {
                if (Vector3.Distance(player.transform.position, dead.transform.position) < helpDistance.Value)
                {
                    Logger.LogInfo("In range");
                    helping = dead;
                }
            }
        }

        private void InitConfig()
        {
            helpDistance = Config.Bind(
                section: "Help distance",
                key: "distance",
                description: "Must be this close to a player to revive them. (meters)",
                defaultValue: 1.2f);

            helpTime = Config.Bind(
                section: "Help Time",
                key: "time",
                description: "Reviving a teammate will take this long. (seconds)",
                defaultValue: 5f);
        }
    }
}
