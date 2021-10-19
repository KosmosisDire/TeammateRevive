using System.Linq;
using R2API.Networking;
using RoR2;
using TeammateRevive.Common;
using TeammateRevive.Configuration;
using TeammateRevive.Players;
using TeammateRevive.Resources;
using TeammateRevive.Revive;
using UnityEngine;

namespace TeammateRevive.Debug
{
    public static class DebugHelper
    {
        public static PluginConfig Config { get; set; }
        
        public static void Init(PluginConfig config)
        {
            NetworkingAPI.RegisterMessageType<DebugNetworkMessage>();
            Config = config;
        }


        public static void Update()
        {
            var players = PlayersTracker.instance;
            
            // set 1st player hp to 1
            if (Input.GetKeyDown(KeyCode.F3)) players.All[0].GetBody().healthComponent.Networkhealth = 1;
            
            // set 1nd player hp to 1
            if (Input.GetKeyDown(KeyCode.F4)) players.All[1].GetBody().healthComponent.Networkhealth = 1;
            
            // damage 2nd player
            if (Input.GetKeyDown(KeyCode.F5)) players.All[1].GetBody().healthComponent.TakeDamage(new DamageInfo
            {
                attacker = players.All[0].GetBody().gameObject,
                damage = 10000,
                damageType = DamageType.Generic
            });

            // give curse to all
            if (Input.GetKeyDown(KeyCode.F6))
                NetworkUser.readOnlyInstancesList.ToList()
                    .ForEach(u => u.master.inventory.GiveItem(ItemsAndBuffs.ReduceHpItemIndex));
            
            // remove curse from all
            if (Input.GetKeyDown(KeyCode.F7))
                NetworkUser.readOnlyInstancesList.ToList()
                    .ForEach(u => u.master.inventory.RemoveItem(ItemsAndBuffs.ReduceHpItemIndex));
            
            // give obol to all
            if (Input.GetKeyDown(KeyCode.F8))
                NetworkUser.readOnlyInstancesList.ToList()
                    .ForEach(u => u.master.inventory.GiveItem(ItemsAndBuffs.ReviveItemIndex));
            
            // remove obol from all
            if (Input.GetKeyDown(KeyCode.F9))
                NetworkUser.readOnlyInstancesList.ToList()
                    .ForEach(u => u.master.inventory.RemoveItem(ItemsAndBuffs.ReviveItemIndex));

            // spawn debug skull for current player
            if (Input.GetKeyDown(KeyCode.F10))
            {
                if (NetworkHelper.IsClient())
                {
                    DebugNetworkMessage.SendToServer("SpawnSkull");
                }
                else
                {
                    SpawnSkullForFirstPlayer();
                }
            }

            if (Config.GodMode)
            {
                foreach (var player in players.All)
                {
                    var body = player.GetBody();
                    if (body != null)
                    {
                        body.healthComponent.Networkbarrier = 1000;
                    }
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