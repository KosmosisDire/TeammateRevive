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
        public GameObject nearbyRadiusIndicator = null;
    }
    
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class TeammateRevive : BaseUnityPlugin
	{
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "KosmosisDire";
        public const string PluginName = "TeammateRevival";
        public const string PluginVersion = "2.1.0";

        public bool playersSetup = false;
        public List<Player> alivePlayers = new List<Player>();
        public List<Player> deadPlayers = new List<Player>();

        private GameObject deathMarker;
        private GameObject nearbyMarker;

        #region Setup

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
                deathMarker.AddComponent<Highlight>().isOn = true;
                

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

        #endregion

        public void RespawnPlayer(Player player)
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
            }
        }

        public void Update()
        {
            //god mode for testing ;)
            //if (Input.GetKeyDown(KeyCode.F2))
            //{
            //    PlayerCharacterMasterController.instances[0].master.ToggleGod();
            //}

            //find smallest max health out of all the players
            int smallestMax = int.MaxValue;
            for (int i = 0; i < alivePlayers.Count; i++)
            {
                if(alivePlayers[i].body.healthComponent.health < smallestMax)
                    smallestMax = (int)alivePlayers[i].body.maxHealth;
            }

            //the player must give this much health to revive the other player
            float threshold = (smallestMax * 0.9f);


            for (int i = 0; i < alivePlayers.Count; i++)
            {
                Player player = alivePlayers[i];

                //is player dead?
                if (!player.master.GetBody())
                {
                    player.isDead = true;
                    deadPlayers.Add(player);
                    alivePlayers.Remove(player);

                    //set the transforms of the prefabs before spawning them in
                    deathMarker.transform.position = player.lastPosition + Vector3.up * 2;
                    deathMarker.transform.rotation = Quaternion.identity;
                    nearbyMarker.transform.localScale = (Vector3.one / 26) * 8;
                    nearbyMarker.transform.position = player.lastPosition;
                    nearbyMarker.transform.rotation = Quaternion.identity;

                    player.deathMark = Instantiate(deathMarker);
                    player.nearbyRadiusIndicator = Instantiate(nearbyMarker);

                    //spawn another nearby circle to make it more visible
                    var secondMarker = Instantiate(nearbyMarker);
                    secondMarker.transform.SetParent(player.nearbyRadiusIndicator.transform);
                    secondMarker.transform.localScale = Vector3.one;

                    continue;
                }
                //else, then player must be alive

                player.lastPosition = player.master.GetBodyObject().transform.position;
                

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

                        if(Random.Range(0f, 100f) < 10f)
                            DamageNumberManager.instance.SpawnDamageNumber(amount * 10 + Random.Range(-1,2), player.lastPosition + Vector3.up * 0.75f, false, TeamIndex.Player, DamageColorIndex.Bleed);
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
                        Destroy(dead.nearbyRadiusIndicator);
                        Destroy(dead.deathMark);

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
