﻿using System.Linq;
using R2API.Utils;
using RoR2;
using TeammateRevive.Content;
using TeammateRevive.Logging;
using TeammateRevive.Players;
using TeammateRevive.DeathTotem;
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
            if (networkUser && networkUser.master.inventory.GetItemCount(CharonsObol.Index) > 0)
            {
                return Interactability.Available;
            }

            return Interactability.ConditionsNotMet;
        }

        public void OnInteractionBegin(Interactor interactor)
        {
            Log.DebugMethod("Interaction! " + interactor.name);
            var totemId = gameObject.GetComponent<NetworkBehaviour>().netId;
            Log.DebugMethod($"Totem Id " + totemId);
            HandleInteraction(interactor.netId, totemId);
        }

        public static void HandleInteraction(NetworkInstanceId playerNetId, NetworkInstanceId totemId)
        {
            Log.DebugMethod("Server respawn!");
            var player = PlayersTracker.instance.FindByBodyId(playerNetId);
            Log.DebugMethod($"Player " + player);

            var totemComponent = Util.FindNetworkObject(totemId).GetComponent<DeathTotemBehavior>();
            Log.DebugMethod($"Totem component " + totemId);
            var dead = PlayersTracker.instance.All.FirstOrDefault(dp => dp.deathTotem == totemComponent);
            Log.DebugMethod($"Dead " + dead);
                
            if (dead == null || player == null)
            {
                Log.Error($"Cannot find player(s): {player} -> {dead}");
                return;
            }

            var playerHasRespawnItem = player.GetBody().inventory.GetItemCount(CharonsObol.Index) > 0;

            if (!playerHasRespawnItem)
            {
                ChatMessage.SendColored("Cannot instantly resurrect without Charon's Obol!", Color.red);
                return;
            }

            PlayersTracker.instance.Respawn(dead);
            player.master.master.inventory.RemoveItem(CharonsObol.Index);
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