using System.Linq;
using R2API.Utils;
using RoR2;
using TeammateRevive.Logging;
using TeammateRevive.Players;
using TeammateRevive.Resources;
using TeammateRevive.Skull;
using UnityEngine;
using UnityEngine.Networking;
using static TeammateRevive.Common.TextFormatter;

namespace TeammateRevive.Revive
{
    public class ReviveInteraction : MonoBehaviour, IInteractable, IDisplayNameProvider
    {
        private static readonly string DisplayString = 
            $"Use {Green("Charon's Obol")} to revive ({Red("Reduces Max Hp/Shield")})";

        public string GetContextString(Interactor activator) => DisplayString;

        public Interactability GetInteractability(Interactor activator)
        {
            var networkUser = Util.LookUpBodyNetworkUser(activator.gameObject);
            if (networkUser && networkUser.master.inventory.GetItemCount(AssetsIndexes.CharonsObolItemIndex) > 0)
            {
                return Interactability.Available;
            }

            return Interactability.ConditionsNotMet;
        }

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
            var player = PlayersTracker.instance.FindByBodyId(playerNetId);
            Log.DebugMethod($"Player " + player);

            var skullComp = Util.FindNetworkObject(skullId).GetComponent<DeadPlayerSkull>();
            Log.DebugMethod($"Skull component " + skullId);
            var dead = PlayersTracker.instance.All.FirstOrDefault(dp => dp.skull == skullComp);
            Log.DebugMethod($"Dead " + dead);
                
            if (dead == null || player == null)
            {
                Log.Error($"Cannot find player(s): {player} -> {dead}");
                return;
            }

            var playerHasRespawnItem = player.GetBody().inventory.GetItemCount(AssetsIndexes.CharonsObolItemIndex) > 0;

            if (!playerHasRespawnItem)
            {
                ChatMessage.SendColored("Cannot instantly resurrect without Charon's Obol!", Color.red);
                return;
            }

            RevivalTracker.instance.Revive(dead);
            player.master.master.inventory.RemoveItem(AssetsIndexes.CharonsObolItemIndex);
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