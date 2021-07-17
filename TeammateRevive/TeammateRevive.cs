using BepInEx;
using BepInEx.Configuration;
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
        public CharacterMaster master;
        public PlayerCharacterMasterController playerCharacterMaster;
        public CharacterBody body;
        public float rechargedHealth = 0;
        public Vector3 lastPosition = Vector3.zero;
        public GameObject deathMark = null;
        public GameObject nearbyRadiusIndicator = null;

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
        public const string PluginVersion = "3.1.0";
        public static bool logging = true;
        public static bool fileLogging = false;
        public static bool godMode = false;

        public bool playersSetup = false;
        public int totalPlayers = 0;
        public List<Player> alivePlayers = new List<Player>();
        public List<Player> deadPlayers = new List<Player>();

        private GameObject deathMarker;
        private GameObject nearbyMarker;

        #region Setup

        public void LogInfo(object msg) 
        {
            if (logging)
                Logger.LogInfo(msg);
            if (fileLogging)
                DebugLogger.LogInfo(msg);
        }
        public void LogWarning(object msg)
        {
            if (logging)
                Logger.LogWarning(msg);
            if (fileLogging)
                DebugLogger.LogWarning(msg);
        }
        public void LogError(object msg)
        {
            if (logging)
                Logger.LogError(msg);
            if (fileLogging)
                DebugLogger.LogError(msg);
        }


        public void Awake()
        {
            Log.Init(Logger);

            InitConfig();
            SetupHooks();

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TeammateRevive.customprefabs"))
            {
                var bundle = AssetBundle.LoadFromStream(stream);
                var dm = bundle.LoadAsset<GameObject>("Assets/PlayerDeathPoint.prefab");
                var nm = Resources.Load<GameObject>("prefabs/networkedobjects/NearbyDamageBonusIndicator");

                deathMarker = PrefabAPI.InstantiateClone(dm, "Death Marker");
                nearbyMarker = PrefabAPI.InstantiateClone(nm, "Nearby Marker");

                Destroy(nearbyMarker.GetComponent<NetworkedBodyAttachment>());

                bundle.Unload(false);
            }

            LogInfo(" Setup Teammate Revival -");
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

        void hook_OnStartLocalPlayer(On.RoR2.NetworkUser.orig_OnStartLocalPlayer orig, global::RoR2.NetworkUser self)
        {
            orig(self);
            LogWarning("Start Local Player!!!!");
            if (IsClient())
            {
                ClientScene.RegisterPrefab(deathMarker);
                ClientScene.RegisterPrefab(nearbyMarker);
                FindObjectOfType<NetworkManager>().spawnPrefabs.Add(deathMarker);
                FindObjectOfType<NetworkManager>().spawnPrefabs.Add(nearbyMarker);
                LogInfo("Registered Prefabs");
                return;
            }
        }


        bool IsClient() 
        {
            if (RoR2.RoR2Application.isInSinglePlayer || !NetworkServer.active)
            {
                return true;
            }

            return false;
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
                    LogInfo("  Dead Player Removed  ");
                    
                    return;
                }
            }

            for (int i = 0; i < alivePlayers.Count; i++)
            {
                Player player = alivePlayers[i];
                if (player.networkUser.userName == user.userName)
                {
                    deadPlayers.RemoveAt(i);
                    LogInfo("  Living Player Removed  ");
                    
                    return;
                }
            }

            LogInfo("  PLayer Left - they were not registed as alive or dead  ");

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
            }
        }

        void hook_BeginGameOver(On.RoR2.Run.orig_BeginGameOver orig, global::RoR2.Run self, global::RoR2.GameEndingDef gameEndingDef) 
        {
            orig(self, gameEndingDef);

            if (IsClient()) return;

            LogInfo("  Game Over - reseting data  ");
            
            ResetSetup();
            totalPlayers = 0;
        }

        void hook_AdvanceStage(On.RoR2.Run.orig_AdvanceStage orig, global::RoR2.Run self, global::RoR2.SceneDef nextScene) 
        {
            orig(self, nextScene);

            if (IsClient()) return;

            LogInfo("  Advanced a stage - now resetting  ");
           

            ResetSetup();
        }

        void ResetSetup() 
        {
            smallestMax = float.PositiveInfinity;
            threshold = 0;
            playersSetup = false;
            alivePlayers = new List<Player>();
            deadPlayers = new List<Player>();
            numPlayersSetup = 0;
            LogInfo("  Reset Data  ");
        }

        #endregion

        public void SpawnDeathVisuals(Player player) 
        {
            if (IsClient()) return;

            //set the transforms of the prefabs before spawning them in
            player.deathMark = Instantiate(deathMarker);
            player.nearbyRadiusIndicator = Instantiate(nearbyMarker);

            player.deathMark.transform.position = player.lastPosition + Vector3.up * 2;
            player.deathMark.transform.rotation = Quaternion.identity;
            player.nearbyRadiusIndicator.transform.localScale = (Vector3.one / 26) * 8;
            player.nearbyRadiusIndicator.transform.position = player.lastPosition;
            player.nearbyRadiusIndicator.transform.rotation = Quaternion.identity;

            NetworkServer.Spawn(player.deathMark);
            NetworkServer.Spawn(player.nearbyRadiusIndicator);

            LogInfo("  Skull spawned on Server and Client  ");
           
        }


        public void hook_OnPlayerCharacterDeath (On.RoR2.GlobalEventManager.orig_OnPlayerCharacterDeath orig, global::RoR2.GlobalEventManager self, global::RoR2.DamageReport damageReport, global::RoR2.NetworkUser victimNetworkUser)
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
                    
                    LogInfo("  Player Died!  ");
                  
                    return;
                }
            }
            LogError("  Player Died but they were not alive to begin with!  ");
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
            }
            LogInfo("  Player Respawned  ");
         
        }

        float smallestMax = float.PositiveInfinity;
        float threshold = 0;
        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2))
            {
                //Instantiate(deathMarker, PlayerCharacterMasterController.instances[0].body.transform.position + Vector3.up * 2, Quaternion.identity);
                //SpawnDeathVisuals(alivePlayers[0]);
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


                player.lastPosition = player.body.transform.position;
                for (int j = 0; j < deadPlayers.Count; j++)
                {
                    Player dead = deadPlayers[j];
                    //if alive player is within the range of the circle
                    if ((player.lastPosition - dead.lastPosition).magnitude < 4)
                    {
                        //add health to dead player
                        float amount = player.body.level * Time.deltaTime * 5;
                        dead.rechargedHealth += amount;

                        //take health away from alive player
                        DamageInfo DI = new DamageInfo();
                        DI.attacker = dead.master.gameObject;
                        DI.damage = Mathf.Clamp(amount, 0f, player.body.healthComponent.health - 1f);
                        DI.damageColorIndex = DamageColorIndex.Default;
                        DI.damageType = DamageType.BypassArmor | DamageType.NonLethal;
                        DI.force = Vector3.zero;

                        player.body.healthComponent.Networkhealth -= Mathf.Clamp(amount, 0f, player.body.healthComponent.health - 1f);

                        if (Random.Range(0f, 100f) < 10f)
                            DamageNumberManager.instance.SpawnDamageNumber(amount * 10 + Random.Range(-1, 2), player.lastPosition + Vector3.up * 0.75f, false, TeamIndex.Player, DamageColorIndex.Bleed);
                        if (Random.Range(0f, 100f) < 10f)
                            DamageNumberManager.instance.SpawnDamageNumber(amount * 10 + Random.Range(-1, 2), dead.lastPosition + Vector3.up * 2, false, TeamIndex.Player, DamageColorIndex.Heal);

                        //set light color and intensity based on ratio
                        float ratio = (dead.rechargedHealth / threshold);
                        dead.deathMark.transform.GetChild(0).GetComponentInChildren<Light>(false).color = new Color(1 - ratio, ratio, 0.6f * ratio);
                        dead.deathMark.transform.GetChild(0).GetComponentInChildren<Light>(false).intensity = 4 + 15 * ratio;
                    }
                    else
                    {
                        dead.deathMark.transform.GetChild(0).GetComponentInChildren<Light>(false).color = new Color(1, 0, 0);
                    }

                    //if dead player has recharged enough health, respawn
                    if (dead.rechargedHealth >= threshold)
                    {
                        NetworkServer.Destroy(dead.nearbyRadiusIndicator);
                        NetworkServer.Destroy(dead.deathMark);

                        RespawnPlayer(dead);

                        dead.body.healthComponent.Networkhealth = threshold;
                        dead.rechargedHealth = 0;
                    }
                }
            }
        }

        private void InitConfig()
        {
            //config not here yet
            Config.Clear();


        }
    }
}
