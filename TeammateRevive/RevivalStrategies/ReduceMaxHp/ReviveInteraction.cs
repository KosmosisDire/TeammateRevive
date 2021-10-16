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
        private static readonly string DisplayString = 
            $"Use {AddedResources.WrapColor("Charon's Obol", PluginColors.Green)} to revive ({AddedResources.WrapColor("Reduces Max Hp/Shield", PluginColors.Red)})";

        public string GetContextString(Interactor activator) => DisplayString;

        // TODO: Interactability check
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
            var dead = MainTeammateRevival.instance.AllPlayers.FirstOrDefault(dp => dp.skull == skullComp);
            Log.DebugMethod($"Dead " + dead);
                
            if (dead == null || player == null)
            {
                Log.Error($"Cannot find player(s): {player} -> {dead}");
                return;
            }

            var playerHasRespawnItem = player.GetBody().inventory.GetItemCount(AddedResources.ResurrectItemIndex) > 0;

            if (!playerHasRespawnItem)
            {
                ChatMessage.SendColored("Cannot instantly resurrect without Charon's Obol!", Color.red);
                return;
            }

            MainTeammateRevival.instance.RevivalStrategy.Revive(dead);
            player.master.master.inventory.RemoveItem(AddedResources.ResurrectItemIndex);
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