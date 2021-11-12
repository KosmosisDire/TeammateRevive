using System;
using System.Linq;
using R2API.Networking;
using RoR2;
using TeammateRevive.Common;
using TeammateRevive.Configuration;
using TeammateRevive.Debug.Monitor;
using TeammateRevive.Players;
using TeammateRevive.Resources;
using TeammateRevive.Revive;
using UnityEngine;

namespace TeammateRevive.Debug
{
    public static class DebugHelper
    {
        public static PluginConfig Config { get; set; }

        public static DebugMonitorPanelController Monitor;

        public static int DamageTargetIndex = 1;
        
        public static void Init(PluginConfig config)
        {
            NetworkingAPI.RegisterMessageType<DebugNetworkMessage>();
            Config = config;

            Monitor = new DebugMonitorPanelController();
            Monitor.AddWatcher<PlayerReviveMonitor>();
            Monitor.AddWatcher<InsideSkullMonitor>();
            Monitor.AddWatcher<PlayersCountMonitor>();
        }

        public static void Update()
        {
            var players = PlayersTracker.instance;
            
            // set 1st player hp to 1
            if (Input.GetKeyDown(KeyCode.F3)) players.All[0].GetBody().healthComponent.Networkhealth = 1;
            
            // set 1nd player hp to 1
            if (Input.GetKeyDown(KeyCode.F4)) players.All[1].GetBody().healthComponent.Networkhealth = 1;
            
            // damage 2nd player
            if (Input.GetKeyDown(KeyCode.F5))
                RunOnServer(DamageSelectedPlayer, nameof(DamageSelectedPlayer));

            if (Input.GetKeyDown(KeyCode.F6))
                RunOnServer(GiveCurse, nameof(GiveCurse));
            
            if (Input.GetKeyDown(KeyCode.F7))
                RunOnServer(RemoveCurse, nameof(GiveCurse));
            
            if (Input.GetKeyDown(KeyCode.F8))
                RunOnServer(GiveObol, nameof(GiveObol));
            
            if (Input.GetKeyDown(KeyCode.F9))
                RunOnServer(RemoveObol, nameof(RemoveObol));

            if (Input.GetKeyDown(KeyCode.F10))
                RunOnServer(SpawnSkullForFirstPlayer, nameof(SpawnSkullForFirstPlayer));

            // toggle debug monitor 
            if (Input.GetKeyDown(KeyCode.F11))
                Monitor.script.gameObject.SetActive(!Monitor.script.gameObject.activeSelf);

            if (Config.GodMode && NetworkHelper.IsServer)
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

        static void RunOnServer(Action act, string name)
        {
            if (NetworkHelper.IsServer)
            {
                act();
            }
            else
            {
                DebugNetworkMessage.SendToServer(name);
            }
        }

        public static void GiveObol()
        {
            NetworkUser.readOnlyInstancesList.ToList()
                .ForEach(u => u.master.inventory.GiveItem(AssetsIndexes.CharonsObolItemIndex));
        }
        
        public static void RemoveObol()
        {
            NetworkUser.readOnlyInstancesList.ToList()
                .ForEach(u => u.master.inventory.RemoveItem(AssetsIndexes.CharonsObolItemIndex));
        }

        public static void GiveCurse()
        {
            NetworkUser.readOnlyInstancesList.ToList()
                .ForEach(u => u.master.inventory.GiveItem(AssetsIndexes.DeathCurseItemIndex));
        }
        
        public static void RemoveCurse()
        {
            NetworkUser.readOnlyInstancesList.ToList()
                .ForEach(u => u.master.inventory.RemoveItem(AssetsIndexes.DeathCurseItemIndex));
        }

        public static void DamageSelectedPlayer()
        {
            PlayersTracker.instance.All[DamageTargetIndex].GetBody().healthComponent.TakeDamage(new DamageInfo
            {
                attacker = PlayersTracker.instance.All[0].GetBody().gameObject,
                damage = 10000,
                damageType = DamageType.Generic
            });
        }

        public static void SpawnSkullForFirstPlayer()
        {
            var players = PlayersTracker.instance;
            
            if (!players.Dead.Contains(players.All[0]))
            {
                players.Dead.Add(players.All[0]);
            }

            RevivalTracker.instance.ServerSpawnSkull(players.All[0]);
        }
    }
}