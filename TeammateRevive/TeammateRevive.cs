using BepInEx;
using BepInEx.Configuration;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.Projectile;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace TeammateRevive
{
    public class Player
    {
        public NetworkUser networkUser;
        public CharacterMaster master;
        public PlayerCharacterMasterController playerCharacterMaster;
        public CharacterBody body;
        public GameObject bodyObject;
        public bool isDead = false;
        public float rechargedHealth = 0;
        public Vector3 lastPosition = Vector3.zero;
        public GameObject deathMark = null;
    }

    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync)]
    public class TeammateRevive : BaseUnityPlugin
	{
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "KosmosisDire";
        public const string PluginName = "TeammateRevival";
        public const string PluginVersion = "1.0.6";


        public List<Player> alivePlayers = new List<Player>();
        public List<Player> deadPlayers = new List<Player>();

        //config entries
        public static ConfigEntry<float> helpDistance { get; set; }

        private GameObject deathMarker;
        
        public void Awake()
        {
            Log.Init(Logger);
            InitConfig();
            SetupHooks();

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TeammateRevive.customprefabs"))
            {
                Logger.LogInfo("DEBUG! 1");
                var bundle = AssetBundle.LoadFromStream(stream);
                Logger.LogInfo("DEBUG! 2");
                deathMarker = bundle.LoadAsset<GameObject>("Assets/PlayerDeathPoint.prefab");

                bundle.Unload(false);
            }
            
            Logger.LogInfo(" ------------------- Setup Teammate Revival -------------------");
        }

        void SetupHooks()
        {
            On.RoR2.UnitySystemConsoleRedirector.Redirect += orig => { };
            On.RoR2.SurvivorPodController.OnPassengerExit += hook_OnPassengerExit;
            On.RoR2.Run.OnUserAdded += hook_OnUserAdded;
            On.RoR2.Run.OnUserRemoved += Run_OnUserRemoved;
        }
        void hook_OnUserAdded(On.RoR2.Run.orig_OnUserAdded orig, Run self, NetworkUser user) 
        {
            orig(self, user);
            SetupPlayers();
        }
        private void Run_OnUserRemoved(On.RoR2.Run.orig_OnUserRemoved orig, Run self, NetworkUser user)
        {
            orig(self, user);
            SetupPlayers();
        }
        void hook_OnPassengerExit(On.RoR2.SurvivorPodController.orig_OnPassengerExit orig, RoR2.SurvivorPodController self, GameObject passenger) 
        {
            orig(self, passenger);
            SetupPlayers();
        }

        public void SetupPlayers() 
        {
            alivePlayers.Clear();
            deadPlayers.Clear();

            var instances = PlayerCharacterMasterController.instances;
            foreach (PlayerCharacterMasterController playerCharacterMaster in instances)
            {
                Player newPlayer = new Player();
                if (playerCharacterMaster.networkUser) newPlayer.networkUser = playerCharacterMaster.networkUser;
                if (playerCharacterMaster.master) newPlayer.master = playerCharacterMaster.master;
                if (playerCharacterMaster.master) newPlayer.playerCharacterMaster = playerCharacterMaster;
                if (playerCharacterMaster.master.GetBody()) newPlayer.body = playerCharacterMaster.master.GetBody();
                if (playerCharacterMaster.master.bodyPrefab) newPlayer.bodyObject = playerCharacterMaster.master.bodyPrefab;
                newPlayer.isDead = false;
                newPlayer.rechargedHealth = 0;

                alivePlayers.Add(newPlayer);
            }
            Logger.LogInfo(" ---------------- Setup Players ---------------- ");
        }

        public void RespawnChar(Player player)
        {
            if (!deadPlayers.Contains(player)) return;


            bool playerConnected = player.playerCharacterMaster.isConnected;
            
            if (playerConnected && player.isDead)
            {
                player.master.RespawnExtraLife();
                player.body = player.master.GetBody();
                Logger.LogInfo("player body null?  :  " + player.body == null + "-------------------------------");
                player.bodyObject = player.master.bodyPrefab;
                player.isDead = false;
                alivePlayers.Add(player);
                deadPlayers.Remove(player);
                Logger.LogInfo(" ---------------- Revived ---------------- ");
            }

            return;
        }

        GameObject energyBubble = null;


        private void Update()
        {
            //find average max health
            int smallestMax = int.MaxValue;
            smallestMax = 50;
            for (int i = 0; i < alivePlayers.Count; i++)
            {
                if(alivePlayers[i].body.healthComponent.health < smallestMax)
                    smallestMax = (int)alivePlayers[i].body.maxHealth;
            }


            //do all interactions between players and figure out whether they are dead
            for (int i = 0; i < alivePlayers.Count; i++)
            {
                Player player = alivePlayers[i];


                if (player.master == null || player.body == null || player.networkUser == null || player.playerCharacterMaster == null)
                {
                    Logger.LogError("Player has a null reference!");
                }

                if (!player.master.GetBody())
                {
                    //set player to dead and add them to the list
                    player.isDead = true;
                    deadPlayers.Add(player);
                    alivePlayers.Remove(player);
                    Logger.LogInfo(" ---------------- player died!! ---------------- ");
                    player.deathMark = Instantiate(deathMarker, player.lastPosition + Vector3.up * 0.7f, Quaternion.identity);

                    continue;
                }
                
                //set position for use after death
                player.lastPosition = player.master.GetBodyObject().transform.position;
                
                //if the player is alive, see if they are reviving someone
                for (int j = 0; j < deadPlayers.Count; j++)
                {
                    Player dead = deadPlayers[j];

                    Vector3 playerPos = player.lastPosition;
                    Vector3 deadPos = dead.lastPosition;
                    
                    Logger.LogInfo((playerPos - deadPos).magnitude);


                    if ((playerPos - deadPos).magnitude < helpDistance.Value)
                    {
                        //if (energyBubble == null)
                        //{
                        //    energyBubble = Instantiate(Resources.Load<GameObject>("prefabs/projectiles/EngiBubbleShield"), dead.lastPosition + Vector3.up * 0.7f, Quaternion.identity);
                        //    Destroy(energyBubble.GetComponent<ProjectileStickOnImpact>());
                        //    Destroy(energyBubble.GetComponent<Rigidbody>());
                        //}
                        //else
                        //{
                        //    energyBubble.transform.localScale = Vector3.one * (helpDistance.Value / 2) * (dead.rechargedHealth / smallestMax);
                        //    energyBubble.transform.position = dead.lastPosition + Vector3.up * 0.7f;
                        //    energyBubble.transform.rotation = Quaternion.identity;
                        //}


                        if (player.body.healthComponent.health > player.body.maxHealth * 0.1f)
                        {
                            float amount = player.body.level * Time.deltaTime * 3;

                            //add health to dead player
                            dead.rechargedHealth += amount;
                            Logger.LogInfo(" ---------------- Recharging: " + dead.rechargedHealth + " ---------------- ");



                            DamageInfo DI = new DamageInfo();
                            DI.attacker = dead.master.gameObject;
                            DI.damage = amount;
                            DI.damageColorIndex = DamageColorIndex.Bleed;
                            DI.damageType = DamageType.BypassArmor;
                            DI.force = Vector3.zero;

                            //take health away from player
                            player.body.healthComponent.TakeDamage(DI);
                        }
                    }

                    if (dead.rechargedHealth >= smallestMax)
                    {
                        Destroy(energyBubble);
                        Destroy(dead.deathMark);

                        RespawnChar(dead);
                        dead.body.healthComponent.Networkhealth = Mathf.Clamp(smallestMax, 0, dead.body.maxHealth);
                        dead.rechargedHealth = 0;
                    }
                }
            }
        }

        private void InitConfig()
        {
            helpDistance = Config.Bind(
                section: "Help distance",
                key: "distance",
                description: "Must be this close to a player to revive them. (meters)",
                defaultValue: 2f);
        }
    }
}
