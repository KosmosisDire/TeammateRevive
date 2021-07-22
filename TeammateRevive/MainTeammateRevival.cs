using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using R2API;
using R2API.Networking;
using R2API.Utils;
using RoR2;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace TeammateRevival
{
    public class Player
    {
        public NetworkUser networkUser;
        public PlayerCharacterMasterController master;

        public GameObject deathMark = null;
        public GameObject nearbyRadiusIndicator = null;

        public Vector3 groundPosition = Vector3.zero;
        public float rechargedHealth = 0;
        
        public bool isDead = false;
        public NetworkInstanceId bodyID;

        public Player(PlayerCharacterMasterController _player)
        {
            if (_player.networkUser) networkUser = _player.networkUser;
            master = _player;
            if(master.master.GetBody())
                bodyID = master.master.GetBody().netId;

            rechargedHealth = 0;
        }

        public CharacterBody GetBody()
        {
            if(!master.master.GetBody()) Log.LogError("PLAYER HAS NO BODY!!!!!!!!!!!!!!!!!");

            bodyID = master.master.GetBody().netId;
            return master.master.GetBody();
        }

        public bool CheckAlive() 
        {
            return (GetBody() && !master.master.IsDeadAndOutOfLivesServer() && GetBody().healthComponent.alive);
        }

        public bool CheckDead()
        {
            return (!GetBody() || master.master.IsDeadAndOutOfLivesServer() || !GetBody().healthComponent.alive);
        }
    }

    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [R2APISubmoduleDependency(nameof(PrefabAPI), nameof(NetworkingAPI))]
    public class MainTeammateRevival : BaseUnityPlugin
    {
        #region Variables
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "KosmosisDire";
        public const string PluginName = "TeammateRevival";
        public const string PluginVersion = "3.3.2";

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

        float smallestMax = float.PositiveInfinity;
        float threshold = 0;
        int numPlayersSetup = 0;

        public static bool playersSetup = false;
        public static int totalPlayers = 0;
        public static List<Player> alivePlayers = new List<Player>();
        public static List<Player> deadPlayers = new List<Player>();
        public static List<Player> allPlayers = new List<Player>();
        public static bool runStarted;

        private static GameObject deathMarker;
        private static GameObject nearbyMarker;
        
        public static ManualLogSource log;


        

        //configurable variables
        public float totemRange = 4;
        public float revivalSpeed = 5;


        #endregion

        #region Setup

        public void LogInit()
        {
            Log.Init(Logger);
            log = Logger;

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
        public static void LogInfo(object msg)
        {
            if (consoleLogging)
                log.LogInfo(msg);

            if (fileLogging)
                DebugLogger.LogInfo(msg);

            if (chatLogging && runStarted)
                ChatMessage.SendColored(msg.ToString(), Color.blue);

        }
        public static void LogWarning(object msg)
        {
            if (consoleLogging)
                log.LogWarning(msg);

            if (fileLogging)
                DebugLogger.LogWarning(msg);

            if (chatLogging && runStarted)
                ChatMessage.SendColored(msg.ToString(), Color.yellow);

        }
        public static void LogError(object msg)
        {
            if (consoleLogging)
                log.LogError(msg);

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
                dm.AddComponent<DeadPlayerSkull>();

                deathMarker = PrefabAPI.InstantiateClone(dm, "Death Marker");
                nearbyMarker = PrefabAPI.InstantiateClone(nm, "Nearby Marker");
                nearbyMarker.transform.localScale = (Vector3.one / 26) * 8;
                Destroy(nearbyMarker.GetComponent<NetworkedBodyAttachment>());

                bundle.Unload(false);
            }

            NetworkingAPI.RegisterMessageType<SyncSkull>();

            LogInfo("Setup Teammate Revival");
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

        void SetupHooks()
        {
            On.RoR2.Run.OnUserAdded += hook_OnUserAdded;
            On.RoR2.Run.OnUserRemoved += hook_OnUserRemoved;
            On.RoR2.GlobalEventManager.OnPlayerCharacterDeath += hook_OnPlayerCharacterDeath;
            On.RoR2.Run.BeginGameOver += hook_BeginGameOver;
            On.RoR2.Run.AdvanceStage += hook_AdvanceStage;
            On.RoR2.PlayerCharacterMasterController.OnBodyStart += hook_OnBodyStart;
            On.RoR2.NetworkUser.OnStartLocalPlayer += hook_OnStartLocalPlayer;
        }
        #endregion

        #region Hooks

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
            LogInfo(user.userName + " added.");
        }

        void hook_OnUserRemoved(On.RoR2.Run.orig_OnUserRemoved orig, Run self, NetworkUser user)
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

                    deadPlayers.Remove(player);
                    allPlayers.Remove(player);
                    LogInfo("Dead Player Removed");
                    return;
                }
            }
            for (int i = 0; i < alivePlayers.Count; i++)
            {
                Player player = alivePlayers[i];
                if (player.networkUser.userName == user.userName)
                {
                    deadPlayers.Remove(player);
                    allPlayers.Remove(player);
                    LogInfo("Living Player Removed");
                    return;
                }
            }

            LogInfo("PLayer Left - they were not registed as alive or dead");
            orig(self, user);
        }

        void hook_OnBodyStart(On.RoR2.PlayerCharacterMasterController.orig_OnBodyStart orig, global::RoR2.PlayerCharacterMasterController self)
        {
            orig(self);

            if (IsClient())
            {
                return;
            }

            Player p = new Player(self);
            if (godMode)
            {
                p.GetBody().baseDamage = 120;
                p.networkUser.GetCurrentBody().baseMoveSpeed = 30;
                p.GetBody().baseAttackSpeed = 200;
            }

            alivePlayers.Add(p);
            allPlayers.Add(p);
            p.isDead = false;
            
            numPlayersSetup++;
            LogInfo(self.networkUser.userName + " Setup");

            if (numPlayersSetup == totalPlayers)
            {
                playersSetup = true;
                LogInfo("All Players Setup Succesfully");
            }
        }

        void hook_BeginGameOver(On.RoR2.Run.orig_BeginGameOver orig, global::RoR2.Run self, global::RoR2.GameEndingDef gameEndingDef)
        {
            orig(self, gameEndingDef);

            if (IsClient()) return;

            LogInfo("Game Over - reseting data");

            ResetSetup();
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

            Player victim = FindPlayerFromBodyInstanceID(victimNetworkUser.GetCurrentBody().netId);
            if (alivePlayers.Contains(victim))
            {
                PlayerDead(victim);
                LogInfo(victimNetworkUser.userName + " Died!");
                return;
            }
            
            LogError("Player Died but they were not alive to begin with!");
        }

        #endregion

        #region Helpers
        
        public static bool IsClient()
        {
            if (RoR2.RoR2Application.isInSinglePlayer || !NetworkServer.active)
            {
                return true;
            }

            return false;
        }

        public static DeadPlayerSkull SpawnDeathVisuals(Player player)
        {
            if (IsClient()) return null;

            //set the transforms of the prefabs before spawning them in
            player.deathMark = Instantiate(deathMarker);
            player.nearbyRadiusIndicator = Instantiate(nearbyMarker);

            player.deathMark.transform.position = player.groundPosition + Vector3.up * 2;
            player.deathMark.transform.rotation = Quaternion.identity;

            player.nearbyRadiusIndicator.transform.position = player.groundPosition;
            player.nearbyRadiusIndicator.transform.rotation = Quaternion.identity;

            NetworkServer.Spawn(player.deathMark);
            NetworkServer.Spawn(player.nearbyRadiusIndicator);

            LogInfo("Skull spawned on Server and Client");

            return player.deathMark.GetComponent<DeadPlayerSkull>();
        }

        public static void PlayerDead(Player p)
        {
            alivePlayers.Remove(p);
            deadPlayers.Add(p);
            p.isDead = true;
            SpawnDeathVisuals(p);
        }

        public static void PlayerAlive(Player p)
        {
            alivePlayers.Add(p);
            deadPlayers.Remove(p);
            p.isDead = false;
            NetworkServer.Destroy(p.nearbyRadiusIndicator);
            NetworkServer.Destroy(p.deathMark);
        }

        public static void RespawnPlayer(Player player)
        {
            if (IsClient()) return;

            if (!deadPlayers.Contains(player)) return;

            bool playerConnected = player.master.isConnected;
            if (playerConnected)
            {
                player.master.master.RespawnExtraLife();
                PlayerAlive(player);
            }
            LogInfo("Player Respawned");
        }

        void CalculateReviveThreshold()
        {
            //find smallest max health out of all the players
            smallestMax = int.MaxValue;
            for (int i = 0; i < alivePlayers.Count; i++)
            {
                Player player = alivePlayers[i];

                if (player.CheckDead()) continue;

                if (alivePlayers[i].GetBody().maxHealth < smallestMax)
                    smallestMax = (int)alivePlayers[i].GetBody().maxHealth;
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
        
        public static Player FindPlayerFromBodyInstanceID(NetworkInstanceId id) 
        {
            foreach (var p in allPlayers)
            {
                p.GetBody();
                if (p.bodyID == id) return p;
            }

            return null;
        }

        #endregion

        float timer = 0;

        public void Update()
        {
            if (IsClient() || !playersSetup) return;

            if (Input.GetKeyDown(KeyCode.F2))
            {
                //PlayerDead(alivePlayers[0]);
                //SpawnDeathVisuals(alivePlayers[0]);
            }

            timer += Time.deltaTime;
            if(timer > 15) 
            {
                deadPlayers.Add(alivePlayers[0]);
                SpawnDeathVisuals(alivePlayers[0]);
                timer = -100000;
            }

            //interactions between dead and alive players
            for (int p = 0; p < alivePlayers.Count; p++)
            {
                Player player = alivePlayers[p];
                if (player.CheckDead()) continue;

                player.groundPosition = GroundPosition(player);

                for (int d = 0; d < deadPlayers.Count; d++)
                {
                    Player dead = deadPlayers[d];
                    DeadPlayerSkull skull = dead.deathMark.GetComponent<DeadPlayerSkull>();

                    //have they been revived by other means?
                    if (dead.CheckAlive()) 
                    {
                        //PlayerAlive(dead);
                        //continue;
                    }


                    //if alive player is within the range of the circle
                    if (Vector3.Distance(player.groundPosition, dead.groundPosition) < totemRange)
                    {
                        //add health to dead player
                        float amount = player.GetBody().level * Time.deltaTime * revivalSpeed;
                        dead.rechargedHealth += amount;

                        //damage alive player - down to 1 HP
                        player.GetBody().healthComponent.Networkhealth -= Mathf.Clamp(amount, 0f, player.GetBody().healthComponent.health - 1f);
                        

                        //set light color and intensity based on ratio
                        float ratio = (dead.rechargedHealth / threshold);
                        skull.SetValues(amount, new Color(1 - ratio, ratio, 0.6f * ratio), 4 + 15 * ratio);
                        if (!skull.insidePlayerIDs.Contains(player.GetBody().netId))
                            skull.insidePlayerIDs.Add(player.GetBody().netId);
                    }
                    else
                    {
                        //set light to red if no one is inside the circle
                        skull.SetValues(skull.amount, new Color(1, 0, 0), skull.intensity);

                        if (skull.insidePlayerIDs.Contains(player.GetBody().netId))
                            skull.insidePlayerIDs.Remove(player.GetBody().netId);
                    }

                    //if dead player has recharged enough health, respawn
                    if (dead.rechargedHealth >= threshold)
                    {
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
