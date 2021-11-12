using System.Linq;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using RoR2.Orbs;
using TeammateRevive.Common;
using TeammateRevive.Resources;
using UnityEngine;
using UnityEngine.Networking;
using static TeammateRevive.Common.TextFormatter;

namespace TeammateRevive.Revive.Shrine
{
    public class ShrineInteraction : NetworkBehaviour, IInteractable, IDisplayNameProvider
    {
        private static readonly string DisplayString = 
            $"Use {Green("Charon's Obol")} to remove Death Curse.";

        public string GetContextString(Interactor activator)
        {
            return DisplayString;
        }

        public Interactability GetInteractability(Interactor activator)
        {
            var networkUser = Util.LookUpBodyNetworkUser(activator.gameObject);
            var hasObol = networkUser && networkUser.master.inventory.GetItemCount(AssetsIndexes.CharonsObolItemIndex) > 0;
            var anyoneHaveCurse = PlayerCharacterMasterController.instances.Any(c =>
                c.master.inventory.GetItemCount(AssetsIndexes.DeathCurseItemIndex) > 0);

            if (hasObol && anyoneHaveCurse)
            {
                return Interactability.Available;
            }

            return Interactability.ConditionsNotMet;
        }

        public void OnInteractionBegin(Interactor activator)
        {
            if (NetworkHelper.IsClient())
            {
                new ShrineInteractionMessage
                {
                    Shrine = this.netId,
                    Activator = activator.netId
                }.Send(NetworkDestination.Server);
            }
            else
            {
                PerformInteraction(activator.gameObject, this.gameObject);
            }
        }

        [Server]
        public static void PerformInteraction(GameObject activator, GameObject shrine)
        {
            var user = Util.LookUpBodyNetworkUser(activator.gameObject);
            user.master.inventory.RemoveItem(AssetsIndexes.CharonsObolItemIndex);

            var orb = new ItemTransferOrb
            {
                origin = activator.gameObject.transform.position,
                itemIndex = AssetsIndexes.CharonsObolItemIndex,
                stack = 1,
                onArrival = _ =>
                {
                    var obolUsed = false;
                    foreach (var otherMaster in PlayerCharacterMasterController.instances)
                    {
                        if (otherMaster.master.inventory.GetItemCount(AssetsIndexes.DeathCurseItemIndex) > 0)
                        {
                            otherMaster.master.inventory.RemoveItem(AssetsIndexes.DeathCurseItemIndex);
                            obolUsed = true;
                        }
                    }

                    if (!obolUsed)
                    {
                        ObolRefund(activator, shrine);
                        return;
                    }

                    Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                    {
                        baseToken =
                            $"{user.userName} used {Green("Charon's Obol")} to remove team's Death Curse!"
                    });
                },
                orbEffectTargetObjectOverride = shrine.GetComponent<NetworkIdentity>()
            };
            
            OrbManager.instance.AddOrb(orb);
        }

        static void ObolRefund(GameObject activator, GameObject shrine)
        {
            var orb = new ItemTransferOrb
            {
                origin = shrine.transform.position,
                itemIndex = AssetsIndexes.CharonsObolItemIndex,
                stack = 1,
                inventoryToGrantTo = Util.LookUpBodyNetworkUser(activator).master.inventory,
                orbEffectTargetObjectOverride = activator.GetComponent<NetworkIdentity>()
            };
            OrbManager.instance.AddOrb(orb);
        }

        public bool ShouldIgnoreSpherecastForInteractibility(Interactor activator)
        {
            return false;
        }

        public bool ShouldShowOnScanner()
        {
            return true;
        }

        public string GetDisplayName() => "Charon's Shrine";
        
        public void OnEnable()
        {
            InstanceTracker.Add(this);
        }

        public void OnDisable()
        {
            InstanceTracker.Remove(this);
        }
    }
    
    
    public class ShrineInteractionMessage : INetMessage
    {
        public NetworkInstanceId Activator { get; set; }
        public NetworkInstanceId Shrine { get; set; }
        
        
        public void Serialize(NetworkWriter writer)
        {
            writer.Write(Activator);
            writer.Write(Shrine);
        }

        public void Deserialize(NetworkReader reader)
        {
            Activator = reader.ReadNetworkId();
            Shrine = reader.ReadNetworkId();
        }

        public void OnReceived()
        {
            var activator = Util.FindNetworkObject(this.Activator);
            var shrine = Util.FindNetworkObject(this.Shrine);
            ShrineInteraction.PerformInteraction(activator, shrine);
        }
    }
}