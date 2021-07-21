using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using R2API;
using R2API.Networking;
using R2API.Utils;
using RoR2;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace TeammateRevival
{
    public class Player
    {
        public NetworkUser networkUser;
        public CharacterMaster master;
        public PlayerCharacterMasterController playerCharacterMaster;
        public CharacterBody body;
        public float rechargedHealth = 0;
        public Vector3 lastPosition = Vector3.zero;
        public GameObject deathMark = null;
        public GameObject nearbyRadiusIndicator = null;
        public bool justRespawned;

        public Player(PlayerCharacterMasterController _player)
        {
            if (_player.networkUser) networkUser = _player.networkUser;
            if (_player.master) master = _player.master;
            if (_player.master) playerCharacterMaster = _player;
            if (_player.master.GetBody()) body = _player.master.GetBody();
            rechargedHealth = 0;
        }
    }


    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [R2APISubmoduleDependency(nameof(PrefabAPI), nameof(NetworkingAPI))]
    public class TeammateRevive : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "KosmosisDire";
        public const string PluginName = "TeammateRevival";
        public const string PluginVersion = "3.2.1";

        //debugging config
        public static ConfigEntry<bool> consoleLoggingConfig;
        public static ConfigEntry<bool> chatLoggingConfig;
        public static ConfigEntry<bool> fileLoggingConfig;
        public static ConfigEntry<string> fileLoggingPath;
        public static ConfigEntry<bool> godModeConfig;
        public static bool consoleLogging = true;
        public static bool chatLogging = true;
        public static bool fileLogging = false;
        public static bool godMode = false;
        

        public bool playersSetup = false;
        public int totalPlayers = 0;
        public List<Player> alivePlayers = new List<Player>();
        public List<Player> deadPlayers = new List<Player>();

        private GameObject deathMarker;
        private GameObject nearbyMarker;
        public bool runStarted;

        #region Setup

        public void LogInit() 
        {
            Log.Init(Logger);
            try
            {
                if (fileLogging)
                    DebugLogger.Init();
            }
            catch
            {
                Logger.LogWarning("Log file location unavailable!");
                fileLogging = false;
            }
        }
        public void LogInfo(object msg) 
        {
            if (consoleLogging)
                Logger.LogInfo(msg);

            if (fileLogging)
                DebugLogger.LogInfo(msg);

            if (chatLogging && runStarted)
                ChatMessage.SendColored(msg.ToString(), Color.blue);
            
        }
        public void LogWarning(object msg)
        {
            if (consoleLogging)
                Logger.LogWarning(msg);

            if (fileLogging)
                DebugLogger.LogWarning(msg);

            if(chatLogging && runStarted) 
                ChatMessage.SendColored(msg.ToString(), Color.yellow);
            
        }
        public void LogError(object msg)
        {
            if (consoleLogging)
                Logger.LogError(msg);

            if (fileLogging)
                DebugLogger.LogError(msg);

            if (chatLogging && runStarted)
                ChatMessage.SendColored(msg.ToString(), Color.red);
            
        }


        public void Awake()
        {
            InitConfig();
            SetupHooks();
            LogInit();
            
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TeammateRevive.customprefabs"))
            {
                var bundle = AssetBundle.LoadFromStream(stream);
                var dm = bundle.LoadAsset<GameObject>("Assets/PlayerDeathPoint.prefab");
                var nm = Resources.Load<GameObject>("prefabs/networkedobjects/NearbyDamageBonusIndicator");
                

                deathMarker = PrefabAPI.InstantiateClone(dm, "Death Marker");
                deathMarker.AddComponent<SkullNetwork>();
                nearbyMarker = PrefabAPI.InstantiateClone(nm, "Nearby Marker");
                nearbyMarker.transform.localScale = (Vector3.one / 26) * 8;
                Destroy(nearbyMarker.GetComponent<NetworkedBodyAttachment>());

                bundle.Unload(false);
            }

            LogInfo("Setup Teammate Revival");
        }

        public static bool IsClient()
        {
            if (RoR2.RoR2Application.isInSinglePlayer || !NetworkServer.active)
            {
                return true;
            }

            return false;
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
            Config.SettingChanged += OnConfigChanged;
        }

        void OnConfigChanged(object sender, System.EventArgs e) 
        {
            InitConfig();
            LogInit();
            LogInfo("Config Changed - Settings have been updated.");
        }

        void hook_OnStartLocalPlayer(On.RoR2.NetworkUser.orig_OnStartLocalPlayer orig, global::RoR2.NetworkUser self)
        {
            runStarted = true;
            orig(self);

            if (IsClient())
            {
                
                ClientScene.RegisterPrefab(deathMarker);
                ClientScene.RegisterPrefab(nearbyMarker);
                FindObjectOfType<NetworkManager>().spawnPrefabs.Add(deathMarker);
                FindObjectOfType<NetworkManager>().spawnPrefabs.Add(nearbyMarker);
                LogInfo("Client Registered Prefabs");
                return;
            }
        }

        void hook_OnUserAdded(On.RoR2.Run.orig_OnUserAdded orig, global::RoR2.Run self, global::RoR2.NetworkUser user) 
        {
            orig(self, user);
            if (IsClient()) return;

            totalPlayers++;
        }

        private void hook_OnUserRemoved(On.RoR2.Run.orig_OnUserRemoved orig, Run self, NetworkUser user)
        {
            if (IsClient())
            {
                orig(self, user);
                return;
            }
            totalPlayers--;
            for (int i = 0; i < deadPlayers.Count; i++)
            {
                Player player = deadPlayers[i];
                if (player.networkUser.userName == user.userName)
                {
                    Destroy(player.nearbyRadiusIndicator);
                    Destroy(player.deathMark);
                    
                    deadPlayers.RemoveAt(i);
                    LogInfo("Dead Player Removed");
                    
                    return;
                }
            }

            for (int i = 0; i < alivePlayers.Count; i++)
            {
                Player player = alivePlayers[i];
                if (player.networkUser.userName == user.userName)
                {
                    deadPlayers.RemoveAt(i);
                    LogInfo("Living Player Removed");
                    
                    return;
                }
            }

            LogInfo("PLayer Left - they were not registed as alive or dead");

            orig(self, user);
        }

        int numPlayersSetup = 0;
        void hook_OnBodyStart(On.RoR2.PlayerCharacterMasterController.orig_OnBodyStart orig, global::RoR2.PlayerCharacterMasterController self)
        {
            orig(self);

            if (IsClient())
            {
                return;
            }

            Player p = new Player(self);
            if (godMode) {
                p.master.ToggleGod();
                p.body.baseDamage = 100;
                p.body.baseMoveSpeed = 30;
            }

            alivePlayers.Add(p);
            numPlayersSetup++;
            LogInfo(self.networkUser.userName + " Setup");

            if (numPlayersSetup == totalPlayers)
            {
                playersSetup = true;
                LogInfo("All Players Setup Succesfully");
                StartCoroutine(SpawnTest());
            }
        }

        void hook_BeginGameOver(On.RoR2.Run.orig_BeginGameOver orig, global::RoR2.Run self, global::RoR2.GameEndingDef gameEndingDef) 
        {
            orig(self, gameEndingDef);

            if (IsClient()) return;

            LogInfo("Game Over - reseting data");
            
            ResetSetup();
            totalPlayers = 0;
        }

        void hook_AdvanceStage(On.RoR2.Run.orig_AdvanceStage orig, global::RoR2.Run self, global::RoR2.SceneDef nextScene) 
        {
            orig(self, nextScene);

            if (IsClient()) return;

            LogInfo("Advanced a stage - now resetting");
           

            ResetSetup();
        }
        
        void hook_OnPlayerCharacterDeath(On.RoR2.GlobalEventManager.orig_OnPlayerCharacterDeath orig, global::RoR2.GlobalEventManager self, global::RoR2.DamageReport damageReport, global::RoR2.NetworkUser victimNetworkUser)
        {
            orig(self, damageReport, victimNetworkUser);

            if (IsClient()) return;

            for (int i = 0; i < alivePlayers.Count; i++)
            {
                Player player = alivePlayers[i];
                if (player.networkUser.userName == victimNetworkUser.userName)
                {
                    alivePlayers.Remove(player);
                    deadPlayers.Add(player);
                    SpawnDeathVisuals(player);

                    LogInfo("Player Died!");

                    return;
                }
            }
            LogError("Player Died but they were not alive to begin with!");
        }

        void ResetSetup() 
        {
            smallestMax = float.PositiveInfinity;
            threshold = 0;
            playersSetup = false;
            alivePlayers = new List<Player>();
            deadPlayers = new List<Player>();
            numPlayersSetup = 0;
            LogInfo("Reset Data");
        }

        #endregion

        public SkullNetwork SpawnDeathVisuals(Player player) 
        {
            if (IsClient()) return null;

            //set the transforms of the prefabs before spawning them in
            player.deathMark = Instantiate(deathMarker);
            player.nearbyRadiusIndicator = Instantiate(nearbyMarker);
            
            player.deathMark.transform.position = player.lastPosition + Vector3.up * 2;
            player.deathMark.transform.rotation = Quaternion.identity;
            
            player.nearbyRadiusIndicator.transform.position = player.lastPosition;
            player.nearbyRadiusIndicator.transform.rotation = Quaternion.identity;

            NetworkServer.Spawn(player.deathMark);
            NetworkServer.Spawn(player.nearbyRadiusIndicator);

            LogInfo("Skull spawned on Server and Client");

            return player.deathMark.GetComponent<SkullNetwork>();
        }
        

        public void RespawnPlayer(Player player)
        {
            if (IsClient()) return;

            if (!deadPlayers.Contains(player)) return;

            bool playerConnected = player.playerCharacterMaster.isConnected;
            if (playerConnected)
            {
                player.master.RespawnExtraLife();
                player.body = player.master.GetBody();
                alivePlayers.Add(player);
                deadPlayers.Remove(player);
                player.justRespawned = true;
            }
            LogInfo("Player Respawned");
         
        }

        public IEnumerator SpawnTest() 
        {
            while (true)
            {
                yield return new WaitForSeconds(2);
                foreach (var player in alivePlayers)
                {
                    SpawnDeathVisuals(player).insidePlayerIDs.Add(player.networkUser._id.value);
                }
            }
        }


        float smallestMax = float.PositiveInfinity;
        float threshold = 0;
        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2))
            {
                //SkullNetwork.RpcDamageNumbers();
                SpawnDeathVisuals(alivePlayers[0]);
            }

            if (IsClient() || !playersSetup) return;

            //find smallest max health out of all the players
            smallestMax = int.MaxValue;
            for (int i = 0; i < alivePlayers.Count; i++)
            { 
                Player player = alivePlayers[i];

                if (!player.master.GetBody() || player.master.IsDeadAndOutOfLivesServer() || !player.master.GetBody().healthComponent.alive) continue;
                

                if (alivePlayers[i].body.maxHealth < smallestMax)
                    smallestMax = (int)alivePlayers[i].body.maxHealth;
            }
            //the player must give this much health to revive the other player
            threshold = (smallestMax * 0.9f);

            //interactions between dead and alive players
            for (int i = 0; i < alivePlayers.Count; i++)
            {
                Player player = alivePlayers[i];

                if (!player.master.GetBody() || player.master.IsDeadAndOutOfLivesServer() || !player.master.GetBody().healthComponent.alive) continue;

                if (player.body.inputBank.jump.justPressed)
                {
                    SpawnDeathVisuals(player);
                }


                if (player.justRespawned) 
                {
                    player.body.healthComponent.Networkhealth = threshold;
                    player.rechargedHealth = 0;
                    if(player.body.healthComponent.health == threshold) 
                    {
                        player.justRespawned = false;
                    }
                }

                RaycastHit hit;
                if (Physics.Raycast(player.body.transform.position, Vector3.down, out hit, 1000, LayerMask.GetMask(new string[] {"World"})))
                {
                    if(Vector3.Dot(hit.normal, Vector3.up) > 0.5f && Vector3.Distance(player.body.transform.position, player.lastPosition) > Vector3.Distance(player.body.transform.position, hit.point)) 
                    {
                        player.lastPosition = hit.point;
                    }
                }


                for (int j = 0; j < deadPlayers.Count; j++)
                {
                    Player dead = deadPlayers[j];
                    SkullNetwork skull = dead.deathMark.GetComponent<SkullNetwork>();
                    //if alive player is within the range of the circle
                    if ((player.lastPosition - dead.lastPosition).magnitude < 4)
                    {
                        //add health to dead player
                        float amount = player.body.level * Time.deltaTime * 5;
                        dead.rechargedHealth += amount;

                        //damage alive player
                        player.body.healthComponent.Networkhealth -= Mathf.Clamp(amount, 0f, player.body.healthComponent.health - 1f);
                        if(!skull.insidePlayerIDs.Contains(player.networkUser._id.value))
                            skull.insidePlayerIDs.Add(player.networkUser._id.value);
                        SkullNetwork.amount = amount;
                        
                        //set light color and intensity based on ratio
                        float ratio = (dead.rechargedHealth / threshold);
                        skull.SetColor(1 - ratio, ratio, 0.6f * ratio, 4 + 15 * ratio);
                        
                    }
                    else
                    {
                        //set light to red if no one is inside the circle
                        skull.SetColor(1, 0, 0, skull.intensity);

                        if (skull.insidePlayerIDs.Contains(player.networkUser._id.value))
                            skull.insidePlayerIDs.Remove(player.networkUser._id.value);
                    }

                    //if dead player has recharged enough health, respawn
                    if (dead.rechargedHealth >= threshold)
                    {
                        NetworkServer.Destroy(dead.nearbyRadiusIndicator);
                        NetworkServer.Destroy(dead.deathMark);

                        RespawnPlayer(dead);
                    }
                }
            }
        }

        private void InitConfig()
        {
            consoleLoggingConfig = Config.Bind<bool>(
                section: "Debugging",
                key: "Console Logging",
                description: "Log debugging messages to the console.",
                defaultValue: false);

            chatLoggingConfig = Config.Bind<bool>(
                section: "Debugging",
                key: "Chat Logging",
                description: "Log debugging messages to the in-game chat.",
                defaultValue: false);

            fileLoggingConfig = Config.Bind<bool>(
                section: "Debugging",
                key: "File Logging",
                description: "Log debugging messages to log.txt located on the desktop by default (sometimes the path cannot be found, so set a custom path below). If the path cannot be found it will write to \"C:\\log.txt\" instead.",
                defaultValue: false);

            fileLoggingPath = Config.Bind<string>(
                section: "Debugging",
                key: "File Logging Path",
                description: "[Please include filename and extention in the path] This setss the location that the logging file will be created. Leave blank to put log.txt on the desktop. If the log file is not showing up set your path manually here.",
                defaultValue: "");

            godModeConfig = Config.Bind<bool>(
                section: "Debugging",
                key: "God Mode",
                description: "Grants Invincibility on every other level (starting on first level, off on second, on for 3rd, etc...). Super massive base damage, and super speed. For testing purposes only, Makes the game incredibly boring.",
                defaultValue: false);

            //set variables
            consoleLogging = consoleLoggingConfig.Value;
            chatLogging = chatLoggingConfig.Value;
            fileLogging = fileLoggingConfig.Value;
            godMode = godModeConfig.Value;
        }
    }
}
