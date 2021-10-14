using System.Linq;
using System.Threading.Tasks;
using R2API.Utils;
using RoR2;
using TeammateRevival;
using TeammateRevival.Logging;
using UnityEngine;
using UnityEngine.Networking;

namespace TeammateRevive.RevivalStrategies.ReduceMaxHp
{
    public class ReviveInteraction : MonoBehaviour, IInteractable, IDisplayNameProvider
    {
        private static string DisplayString = "Revive (Reduce Max Hp/Shield)";

        public string GetContextString(Interactor activator) => DisplayString;

        public Interactability GetInteractability(Interactor activator) => Interactability.Available;

        public void OnInteractionBegin(Interactor interactor)
        {
            Log.DebugMethod("Interaction! " + interactor.name);
            var skullId = this.gameObject.GetComponent<NetworkBehaviour>().netId;
            Log.DebugMethod($"Skull Id " + skullId);
            HandleInteraction(interactor.netId, skullId);
        }

        public static void HandleInteraction(NetworkInstanceId playerNetId, NetworkInstanceId skullId)
        {
            Log.DebugMethod("Server respawn!");
            var player = MainTeammateRevival.instance.FindPlayerFromBodyInstanceID(playerNetId);
            Log.DebugMethod($"Player " + player);

            var skullComp = Util.FindNetworkObject(skullId).GetComponent<DeadPlayerSkull>();
            Log.DebugMethod($"Skull component " + skullId);
            var dead = MainTeammateRevival.instance.AllPlayers.FirstOrDefault(dp => dp.deathMark == skullComp);
            Log.DebugMethod($"Dead " + dead);
                
            if (dead == null || player == null)
            {
                Log.Error($"Cannot find player(s): {player} -> {dead}");
                return;
            }

            var playerHasRespawnItem = player.GetBody().inventory.GetItemCount(AddedResources.RespawnItemIndex) > 0;
            var deadHasRespawnItem = dead.networkUser.master.inventory.GetItemCount(AddedResources.RespawnItemIndex) > 0;

            if (!playerHasRespawnItem && !deadHasRespawnItem)
            {
                ChatMessage.SendColored("One of player's must have Charon's Obol to respawn", Color.red);
                return;
            }

            MainTeammateRevival.instance.RespawnPlayer(dead);
            Task.Delay(1000)
                .ContinueWith(_ =>
                {
                    var deadCharBody = dead.master.master.GetBody();

                    if (deadCharBody == null)
                    {
                        Log.DebugMethod($"deadCharBody is null!");
                    }
                    
                    Log.DebugMethod("Respawn");
                    deadCharBody.inventory.GiveItem(AddedResources.ReduceHpItemIndex);
                    // removing consumed Dio's Best Friend
                    deadCharBody.inventory.RemoveItem(RoR2Content.Items.ExtraLifeConsumed);
                    Log.Debug("Reducing HP for previously dead player done!");
                    
                    Log.DebugMethod("Reducing HP for alive player");
                    if (deadHasRespawnItem)
                    {
                        deadCharBody.inventory.RemoveItem(AddedResources.RespawnItemIndex);
                    }
                    else
                    {
                        player.GetBody().inventory.RemoveItem(AddedResources.RespawnItemIndex);
                    }

                    player.GetBody().inventory.GiveItem(AddedResources.ReduceHpItemIndex);
                });
                
            // Log.DebugMethod("Waiting to reduce...");
            // Task.Delay(1000)
            //     .ContinueWith(_ =>
            //     {
            //         // TODO: find reference for dead player properly
            //         Log.Debug("Reducing HP for previously dead player...");
            //         var deadCharBody = dead.GetBody();
            //         deadCharBody.inventory.GiveItem(AddedResources.ReduceHpItemIndex);
            //         // removing consumed Dio's Best Friend
            //         deadCharBody.inventory.RemoveItem(RoR2Content.Items.ExtraLifeConsumed);
            //         Log.Debug("Reducing HP for previously dead player done!");
            //     });
            
        }

        public bool ShouldIgnoreSpherecastForInteractibility(Interactor activator)
        {
            return false;
        }

        public bool ShouldShowOnScanner()
        {
            return false;
        }

        public string GetDisplayName() => DisplayString;


        public void OnEnable() => InstanceTracker.Add(this);

        public void OnDisable() => InstanceTracker.Remove(this);
    }
}