using BepInEx;
using BepInEx.Configuration;
using R2API;
using R2API.Utils;
using RoR2;
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
        public GameObject nearby = null;
    }

    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod)]
    public class TeammateRevive : BaseUnityPlugin
	{
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "KosmosisDire";
        public const string PluginName = "TeammateRevival";
        public const string PluginVersion = "2.0.0";

        public bool playersSetup = false;
        public List<Player> alivePlayers = new List<Player>();
        public List<Player> deadPlayers = new List<Player>();

        //config entries
        public static ConfigEntry<float> helpDistance { get; set; }

        private GameObject deathMarker;
        private GameObject nearbyMarker;
        
        public void Awake()
        {
            Log.Init(Logger);
            InitConfig();
            SetupHooks();

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TeammateRevive.customprefabs"))
            {
                var bundle = AssetBundle.LoadFromStream(stream);
                deathMarker = bundle.LoadAsset<GameObject>("Assets/PlayerDeathPoint.prefab");
                nearbyMarker = Resources.Load<GameObject>("prefabs/networkedobjects/NearbyDamageBonusIndicator");
                Destroy(nearbyMarker.GetComponents<Component>()[2]);
                Destroy(nearbyMarker.GetComponents<Component>()[1]);

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
            if(playersSetup)
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
            if(!playersSetup)
                SetupPlayers();

            playersSetup = true;
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

            if (Input.GetKeyDown(KeyCode.F2)) 
            {
                PlayerCharacterMasterController.instances[0].master.ToggleGod();
            }

            //find average max health
            int smallestMax = int.MaxValue;
            for (int i = 0; i < alivePlayers.Count; i++)
            {
                if(alivePlayers[i].body.healthComponent.health < smallestMax)
                    smallestMax = (int)alivePlayers[i].body.maxHealth;
            }
            float threshold = (smallestMax * 0.9f);


            //do all interactions between players and figure out whether they are dead
            for (int i = 0; i < alivePlayers.Count; i++)
            {
                Player player = alivePlayers[i];

                if (!player.master.GetBody())
                {
                    //set player to dead and add them to the list
                    player.isDead = true;
                    deadPlayers.Add(player);
                    alivePlayers.Remove(player);

                    deathMarker.transform.position = player.lastPosition + Vector3.up * 2;
                    deathMarker.transform.rotation = Quaternion.identity;

                    player.deathMark = Instantiate(deathMarker);
                    continue;
                }

                if (player.master == null || player.body == null || player.networkUser == null || player.playerCharacterMaster == null)
                {
                    Logger.LogError("Player has a null reference!");
                }

                //set position for use after death
                player.lastPosition = player.master.GetBodyObject().transform.position;
                
                //if the player is alive, see if they are reviving someone
                for (int j = 0; j < deadPlayers.Count; j++)
                {
                    Player dead = deadPlayers[j];

                    Vector3 playerPos = player.lastPosition;
                    Vector3 deadPos = dead.lastPosition;

                    if (dead.nearby == null)
                    {
                        nearbyMarker.transform.localScale = (Vector3.one / 26) * 8;
                        nearbyMarker.transform.position = dead.lastPosition;
                        nearbyMarker.transform.rotation = Quaternion.identity;

                        dead.nearby = Instantiate(nearbyMarker);
                    }

                    if ((playerPos - deadPos).magnitude < 4)
                    {

                        if (player.body.healthComponent.health > player.body.maxHealth * 0.1f)
                        {
                            float amount = player.body.level * Time.deltaTime * 3;

                            //add health to dead player
                            dead.rechargedHealth += amount;

                            float ratio = (dead.rechargedHealth / threshold);
                            dead.deathMark.transform.GetChild(0).GetComponentInChildren<Light>(false).color = new Color(1 - ratio, ratio, 0.6f * ratio);
                            dead.deathMark.transform.GetChild(0).GetComponentInChildren<Light>(false).intensity = 4 + 15 * ratio;



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

                    if (dead.rechargedHealth >= threshold)
                    {
                        Destroy(dead.nearby);
                        Destroy(dead.deathMark);

                        RespawnChar(dead);
                        dead.body.healthComponent.Networkhealth = Mathf.Clamp(threshold, 0, dead.body.maxHealth);
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
                defaultValue: 4f);
        }
    }
}
