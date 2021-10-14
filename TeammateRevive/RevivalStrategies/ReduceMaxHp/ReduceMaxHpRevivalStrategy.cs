using System;
using System.Linq;
using System.Threading.Tasks;
using On.RoR2.UI;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using RoR2.Orbs;
using TeammateRevival;
using TeammateRevival.Logging;
using TeammateRevival.RevivalStrategies;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using SceneExitController = On.RoR2.SceneExitController;
using Stage = On.RoR2.Stage;

namespace TeammateRevive.RevivalStrategies.ReduceMaxHp
{
    class ReduceMaxHpRevivalStrategy : IRevivalStrategy
    {
        public MainTeammateRevival Plugin { get; }
        public PluginConfig Config => MainTeammateRevival.PluginConfig;

        public ReduceMaxHpRevivalStrategy(MainTeammateRevival plugin)
        {
            this.Plugin = plugin;
        }

        private float ReduceHpFactor = 1 + 1 / 3f;
        private static float FactorToGetReviveItem = 1.6f;

        public void Init()
        {
            Log.DebugMethod("start");
            // TODO: debug, remove
            NetworkingAPI.RegisterMessageType<DebugNetworkMessage>();

            AddedResources.Init();

            On.RoR2.UI.BuffIcon.UpdateIcon += BuffIconOnUpdateIcon;
            On.RoR2.CharacterBody.RecalculateStats += CharacterBodyOnRecalculateStats;
            On.RoR2.Stage.Start += StageOnStart;
            On.RoR2.Run.BuildDropTable += OnBuildDropTable;
            // On.RoR2.TeleporterInteraction.Awake += TeleporterInteractionOnAwake;

            // RoR2.TeleporterInteraction.onTeleporterChargedGlobal
            // On.RoR2.SceneExitController.SetState += SceneExitControllerOnSetState;

            Log.DebugMethod("end");
        }

        private void OnBuildDropTable(On.RoR2.Run.orig_BuildDropTable orig, Run self)
        {
            orig(self);

            if (NetworkUser.readOnlyInstancesList.Count < 2)
            {
                var respawnItemIdx = self.availableTier2DropList.FindIndex(pi => pi.pickupDef.itemIndex == AddedResources.RespawnItemIndex);
                if (respawnItemIdx >= 0)
                {
                    Log.DebugMethod("Removing respawn item from drop list");
                    self.availableTier2DropList.RemoveAt(respawnItemIdx);
                }
            }
        }

        private void SceneExitControllerOnSetState(SceneExitController.orig_SetState orig,
            RoR2.SceneExitController self, RoR2.SceneExitController.ExitState newstate)
        {
            orig(self, newstate);
            if (MainTeammateRevival.IsClient()) return;

            // TODO!: client RPC!
            if (newstate == RoR2.SceneExitController.ExitState.ExtractExp)
            {
                NetworkUser.readOnlyInstancesList.ToList().ForEach(SendObol);
            }
        }

        // private void TeleporterInteractionOnAwake(TeleporterInteraction.orig_Awake orig, RoR2.TeleporterInteraction self)
        // {
        //     orig(self);
        //     teleporterPosition = self.modelChildLocator.transform.position;
        //     Debug.Log($"Teleporter position: {teleporterPosition}");
        // }

        private static string[] IgnoredStages = { "arena", "bazaar" };

        private void StageOnStart(Stage.orig_Start orig, RoR2.Stage self)
        {
            orig(self);
            var sceneName = self.sceneDef.cachedName;
            Log.Debug($"Stage start: {self.sceneDef.cachedName}");

            if (MainTeammateRevival.IsClient() || IgnoredStages.Contains(sceneName))
                return;

            foreach (var networkUser in NetworkUser.readOnlyInstancesList)
            {
                if (NetworkUser.readOnlyInstancesList.Count >= 2) AddObol(networkUser);
                RemoveItemInv(networkUser);
            }
        }

        private void AddObol(NetworkUser networkUser)
        {
            if (MainTeammateRevival.IsClient()) return;

            Log.DebugMethod();
            // TODO!: name check
            var userName = networkUser.userName;
            var inventory = networkUser.master?.inventory;

            if (inventory == null)
            {
                Log.Warn($"Player has no inventory! {userName}");
                return;
            }

            if (inventory.GetItemCount(AddedResources.ReduceHpItemIndex) > 0)
            {
                Log.Debug($"Didn't add obol since user {networkUser.name} have curse");
                return;
            }

            var count = inventory.GetItemCount(AddedResources.RespawnItemIndex);

            if (!ShouldGiveObol(count))
            {
                Log.Debug($"Didn't add obol for user user {userName}");
                return;
            }

            inventory.GiveItem(AddedResources.RespawnItemIndex);
            Chat.AddMessage($"{userName} received Charon's Obol!");
            Log.Debug(
                $"Added RespawnItemIndex for ({userName}). Was {count}. Now: {inventory.GetItemCount(AddedResources.RespawnItemIndex)}");
        }

        private bool ShouldGiveObol(int count)
        {
            var threshold = FactorToGetReviveItem / (float)count;
            return Random.Range(0, 1f) <= threshold;
        }

        private void RemoveItemInv(NetworkUser networkUser)
        {
            if (MainTeammateRevival.IsClient()) return;

            Log.DebugMethod();
            var userName = networkUser.userName;
            var inventory = networkUser.master?.inventory;

            if (inventory == null)
            {
                Log.Warn($"Player has no inventory! {userName}");
                return;
            }

            var reduceHpItemCount = inventory.GetItemCount(AddedResources.ReduceHpItemIndex);
            inventory.RemoveItem(AddedResources.ReduceHpItemIndex);
            Log.Debug(
                $"Removed reduce HP item for ({userName}). Was {reduceHpItemCount}. Now: {inventory.GetItemCount(AddedResources.ReduceHpItemIndex)}");
        }

        private void BuffIconOnUpdateIcon(BuffIcon.orig_UpdateIcon orig, RoR2.UI.BuffIcon self)
        {
            orig(self);

            if (self.buffDef == null)
                return;

            return;
            // TODO: remove if custom formatting is no longer needed

            // format buff text into "-<reduced_hp>" form
            var buffIndex = self.buffDef.buffIndex;
            if (buffIndex == AddedResources.ReduceHpBuffIndex || buffIndex == AddedResources.ReduceShieldBuffIndex)
            {
                self.stackCount.SetText(RoR2.UI.BuffIcon.sharedStringBuilder
                    .Clear()
                    .AppendInt(self.buffCount)
                );
            }
        }


        private void CharacterBodyOnRecalculateStats(On.RoR2.CharacterBody.orig_RecalculateStats orig,
            RoR2.CharacterBody self)
        {
            if (self.inventory == null)
            {
                orig(self);
                return;
            }

            // cache previous values of health/shield, since they will be overriden on orig() call
            var health = self.healthComponent.health;
            var shield = self.healthComponent.shield;

            orig(self);

            var reducesCount = self.inventory.GetItemCount(AddedResources.ReduceHpItemIndex);
            self.SetBuffCount(AddedResources.ReduceHpBuffIndex, reducesCount);
            if (reducesCount == 0)
                return;


            var hpReduce = self.maxHealth - self.maxHealth / Mathf.Pow(ReduceHpFactor, reducesCount);
            var shieldReduce = self.maxShield - self.maxShield / Mathf.Pow(ReduceHpFactor, reducesCount);

            Log.DebugMethod(
                $"{self.name} {self.GetDisplayName()} {reducesCount} {self.maxHealth} {self.healthComponent.health} (was {health}) | HP reduce {hpReduce} | curse {self.cursePenalty}");

            self.maxHealth -= hpReduce;
            self.maxShield -= shieldReduce;
            // original logic: maxHP = current max HP / cursePenalty
            self.cursePenalty += (ReduceHpFactor / 2) * reducesCount;

            // this should cut excess health/shield on client
            if (MainTeammateRevival.IsServer)
            {
                self.healthComponent.Networkhealth = Mathf.Min(self.maxHealth, health);
                self.healthComponent.Networkshield = Mathf.Min(self.maxShield, self.healthComponent.shield, shield);
            }
            else
            {
                self.healthComponent.health = Mathf.Min(self.maxHealth, health);
                self.healthComponent.shield = Mathf.Min(self.maxShield, self.healthComponent.shield, shield);
            }
        }

        public DeadPlayerSkull ServerSpawnSkull(Player player)
        {
            var deathMark = Object.Instantiate(this.Plugin.DeathMarker).GetComponent<DeadPlayerSkull>();

            deathMark.transform.position = player.groundPosition;
            deathMark.transform.rotation = Quaternion.identity;
            deathMark.radiusSphere.transform.localScale = Vector3.one * 3;
            CreateInteraction(deathMark.gameObject);

            player.deathMark = deathMark;

            NetworkServer.Spawn(deathMark.gameObject);
            Log.Info("Skull spawned on Server and Client");

            return deathMark;
        }

        public void OnClientSkullSpawned(DeadPlayerSkull skull)
        {
            CreateInteraction(skull.gameObject);
        }

        public void Update(Player player, Player dead)
        {
            // nothing here
        }

        public static void SendObol(NetworkUser networkUser)
        {
            Log.Debug($"Sending Obol to {networkUser.userName}");
            ItemTransferOrb.DispatchItemTransferOrb(teleporterPosition, networkUser.master.inventory,
                AddedResources.RespawnItemIndex, 1);
        }

        private void CreateInteraction(GameObject gameObject)
        {
            // TODO: for some reason this can be called for skull with EntityLocator already initialized
            if (gameObject.GetComponent<EntityLocator>() != null)
            {
                Log.DebugMethod("EntityLocator was null for skull");
                return;
            }

            Log.Debug($"{nameof(ReduceMaxHpRevivalStrategy)}.{nameof(CreateInteraction)}()");
            var collider = gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = CubeMesh;

            gameObject.AddComponent<ReviveInteraction>();
            var locator = gameObject.AddComponent<EntityLocator>();
            locator.entity = gameObject;
            Log.Debug($"{nameof(ReduceMaxHpRevivalStrategy)}.{nameof(CreateInteraction)}() done");
        }

        private static readonly Mesh CubeMesh = new()
        {
            vertices = new Vector3[]
            {
                new(0, 0, 0),
                new(1, 0, 0),
                new(1, 1, 0),
                new(0, 1, 0),
                new(0, 1, 1),
                new(1, 1, 1),
                new(1, 0, 1),
                new(0, 0, 1),
            },
            triangles = new[]
            {
                0, 2, 1, //face front
                0, 3, 2,
                2, 3, 4, //face top
                2, 4, 5,
                1, 2, 5, //face right
                1, 5, 6,
                0, 7, 4, //face left
                0, 4, 3,
                5, 4, 7, //face back
                5, 7, 6,
                0, 6, 7, //face bottom
                0, 1, 6
            }
        };

        private static Vector3 teleporterPosition =>
            RoR2.TeleporterInteraction.instance.modelChildLocator.transform.position;
    }

    public class DebugNetworkMessage : INetMessage
    {
        public void Serialize(NetworkWriter writer)
        {
        }

        public void Deserialize(NetworkReader reader)
        {
        }

        public void OnReceived()
        {
            MainTeammateRevival.instance.RevivalStrategy.ServerSpawnSkull(MainTeammateRevival.instance.AlivePlayers
                .First());
        }
    }
}