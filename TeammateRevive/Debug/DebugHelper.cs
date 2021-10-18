using System.Linq;
using R2API.Networking;
using RoR2;
using TeammateRevive.Players;
using TeammateRevive.Resources;
using TeammateRevive.Revive;
using UnityEngine;

namespace TeammateRevive.Debug
{
    public static class DebugHelper
    {
        public static void Init()
        {
            NetworkingAPI.RegisterMessageType<DebugNetworkMessage>();
        }

        public static void Update()
        {
            var players = PlayersTracker.instance;
            
            if (Input.GetKeyDown(KeyCode.F3)) players.All[0].GetBody().healthComponent.Networkhealth = 1;
            if (Input.GetKeyDown(KeyCode.F4)) players.All[0].GetBody().healthComponent.Networkhealth = 1;
            if (Input.GetKeyDown(KeyCode.F5)) players.All[0].GetBody().healthComponent.TakeDamage(new DamageInfo
            {
                attacker = players.All[0].GetBody().gameObject,
                damage = 10000,
                damageType = DamageType.Generic
            });

            if (Input.GetKeyDown(KeyCode.F6))
                NetworkUser.readOnlyInstancesList.ToList()
                    .ForEach(u => u.master.inventory.GiveItem(AddedResources.ReduceHpItemIndex));
            if (Input.GetKeyDown(KeyCode.F7))
                NetworkUser.readOnlyInstancesList.ToList()
                    .ForEach(u => u.master.inventory.RemoveItem(AddedResources.ReduceHpItemIndex));
            
            if (Input.GetKeyDown(KeyCode.F8))
                NetworkUser.readOnlyInstancesList.ToList()
                    .ForEach(u => u.master.inventory.GiveItem(AddedResources.ReviveItemIndex));

            if (Input.GetKeyDown(KeyCode.F10))
            {
                if (MainTeammateRevival.IsClient())
                {
                    DebugNetworkMessage.SendToServer("SpawnSkull");
                }
                else
                {
                    SpawnSkullForFirstPlayer();
                }
            }
        }

        public static void SpawnSkullForFirstPlayer()
        {
            var players = PlayersTracker.instance;
            
            if (!players.Dead.Contains(players.Alive[0]))
            {
                players.Dead.Add(players.Alive[0]);
            }

            RevivalTracker.instance.ServerSpawnSkull(players.All[0]);
        }
    }
}